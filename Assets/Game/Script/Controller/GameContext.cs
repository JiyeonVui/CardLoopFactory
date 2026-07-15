
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Engine.Manager;
using Extension;
using Script.Engine.Manager.Pooling;
using UnityEngine;

public class GameContext : MonoBehaviour
{
    // Only one level exists for now, so its Resources path is hard-coded.
    private const string LevelResourcePath = "level_test";

    // Prefab spawned once per TrayModel in the loaded level. Lives under
    // Assets/Game/Prefab (outside Resources), so it must be wired in the inspector.
    [SerializeField] private GameObject _trayPrefab;
    [SerializeField] private Transform trayHolderOnPlane;
    [SerializeField] private Transform cameraTransform;
    // Quản lý 3 màn UI (Start / Transition / Result). Kéo vào inspector — đây là nơi
    // GameContext gán và Provide UIManager làm service IUIManager.
    [SerializeField] private UIManager _uiManager;

    // Camera quay tại chỗ (idle) quanh local Z khi chưa vào game: 360°/CameraSpinDuration
    // giây (= 10°/s), loop vô hạn. Khi bấm Start ngừng quay và (sau khi che) đặt về góc chơi.
    private const float CameraSpinDuration = 36f;
    private static readonly Vector3 CameraPlayEuler = new Vector3(90f, 0f, 0f);

    private GameContextData _contextData;
    private GameController _gameController;
    private Tween _cameraSpinTween;


    protected async void Start() {
        try
        {
            await ServiceInitializer.InitializeAsync();

            // Đảm bảo về main thread trước khi gọi API Unity (FindFirstObjectByType,
            // spawn, v.v.) — phòng trường hợp một service init rời main thread.
            await UniTask.SwitchToMainThread();

            // Đăng ký UIManager làm service để nơi khác resolve nếu cần.
            if (_uiManager != null)
            {
                ServiceLocator.Instance.Provide<IUIManager>(_uiManager);
            }

            _gameController = FindFirstObjectByType<GameController>();

            var poolingService = ServiceLocator.Instance.Resolve<IPoolingService>();
            var factory = new GameEntityFactory(poolingService);

            _contextData = new(_gameController, factory);
            factory.SetInitializer(sub => sub.ReadyToStartGame(_contextData));

            // Chưa load level: chờ người chơi bấm Start (level sẽ load dưới màn transition).
            _gameController.StopGame(true);

            WireUiEvents();
            StartCameraSpin();
            _uiManager?.ShowStart();
        }
        catch (Exception e) {

            Debug.LogError(e);
        }
    }

    // Camera quay quanh trục Z LOCAL (LocalAxisAdd giữ nguyên tilt X/Y ban đầu), tốc độ
    // 360°/CameraSpinDuration (= 10°/s), loop vô hạn với tốc độ đều (Linear + Incremental).
    private void StartCameraSpin()
    {
        if (cameraTransform == null)
        {
            return;
        }

        _cameraSpinTween?.Kill();
        _cameraSpinTween = cameraTransform
            .DOLocalRotate(new Vector3(0f, 0f, 360f), CameraSpinDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Incremental);
    }

    private void StopCameraSpin()
    {
        _cameraSpinTween?.Kill();
        _cameraSpinTween = null;
    }

    private void OnDestroy()
    {
        StopCameraSpin();
    }

    // Nối UIManager với vòng đời game. GameController giữ UI-agnostic: chỉ bắn OnWin/
    // OnLose, còn hiển thị màn kết quả do đây quyết định.
    private void WireUiEvents()
    {
        if (_uiManager == null)
        {
            Debug.LogError("[GameContext] UIManager chưa được gán trong inspector.");
            return;
        }

        _uiManager.OnStartClicked += () => HandleStartClicked().Forget();
        _uiManager.OnRetryClicked += () => HandleRetryClicked().Forget();
        _gameController.OnWin += () => _uiManager.ShowResult(true);
        _gameController.OnLose += () => _uiManager.ShowResult(false);
    }

    // Bấm Start: ẩn màn Start → cloud chụm che → load level dưới màn che → cloud tản ra
    // → bắt đầu chơi.
    private async UniTaskVoid HandleStartClicked()
    {
        _uiManager.HideStart();
        // Ngừng quay camera ngay khi bấm; đợi cloud che kín rồi mới snap về góc chơi để
        // người chơi không thấy camera nhảy góc.
        StopCameraSpin();
        await _uiManager.PlayCoverAsync();
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(CameraPlayEuler);
        }
        LoadLevel();
        await _uiManager.PlayRevealAsync();
        _gameController.StopGame(false);
    }

    // Bấm Retry: cloud chụm che màn hình, reset game ngay trong scene (không load lại
    // scene), ẩn màn kết quả, rồi cloud tản ra và chơi lại.
    private async UniTaskVoid HandleRetryClicked()
    {
        // Ẩn màn Result ngay khi bấm (trước await) để không bấm Retry lại lúc cloud đang che.
        _uiManager.HideResult();
        await _uiManager.PlayCoverAsync();
        ResetGame();
        await _uiManager.PlayRevealAsync();
        _gameController.StopGame(false);
    }

    // Reset in-scene: despawn hết tray + card về pool, xoá card trên belt, reset counter
    // GameController, rồi dựng lại level từ đầu (LoadLevel tự re-Init các controller và
    // đọc lại level JSON tươi nên không dính state ván cũ).
    private void ResetGame()
    {
        _gameController.ResetForReplay();
        DespawnAllTraysAndCards();

        IBeltController beltController = ServiceLocator.Instance.Resolve<IBeltController>();
        beltController.ClearCards();

        LoadLevel();
    }

    // Trả toàn bộ tray + card đang active về pool bằng cách duyệt đúng các holder chứa
    // chúng (không dùng FindObjectsByType vì sẽ quét cả object đã pooled). Object đã pool
    // nằm dưới Pool Holder nên không lọt vào các holder này.
    //
    // - Tray đứng sân: trayHolderOnPlane (GameContext).
    // - Tray đã thành match-slot: GameController.TrayHolderCard.
    //   (DespawnToPool của tray tự kéo theo card dưới CardHolder của nó.)
    // - Card đang bay/bám belt: GameController.BeltHolderCard.
    private void DespawnAllTraysAndCards()
    {
        DespawnTraysUnder(trayHolderOnPlane);
        DespawnTraysUnder(_gameController.TrayHolderCard);
        DespawnCardsUnder(_gameController.BeltHolderCard);
    }

    // Gom tray con trước rồi mới despawn để không sửa collection con trong lúc duyệt
    // (DespawnToPool reparent tray ra Pool Holder → thay đổi danh sách con của holder).
    private static void DespawnTraysUnder(Transform holder)
    {
        if (holder == null)
        {
            return;
        }

        var trays = new List<TrayView>();
        foreach (Transform child in holder)
        {
            if (child.TryGetComponent(out TrayView tray))
            {
                trays.Add(tray);
            }
        }

        foreach (TrayView tray in trays)
        {
            tray.DespawnToPool();
        }
    }

    private static void DespawnCardsUnder(Transform holder)
    {
        if (holder == null)
        {
            return;
        }

        var cards = new List<CardView>();
        foreach (Transform child in holder)
        {
            if (child.TryGetComponent(out CardView card))
            {
                cards.Add(card);
            }
        }

        foreach (CardView card in cards)
        {
            card.ReturnToPool();
        }
    }

    // Reads the level JSON and builds a TrayView for each TrayModel through the
    // entity factory (factory pattern), then initializes it with its model.
    private void LoadLevel()
    {
        if (_trayPrefab == null)
        {
            Debug.LogError("[GameContext] Tray prefab is not assigned.");
            return;
        }

        LevelManager level = LevelManager.LoadFromResources(LevelResourcePath);
        if (level?.TrayModels == null)
        {
            return;
        }

        // Cấp dữ liệu slot cho MatchColorController trước khi tray phát card.
        IMatchColorController matchColor = ServiceLocator.Instance.Resolve<IMatchColorController>();
        matchColor.Init(level.MatchSlotPositions, level.MatchSlotRotations, level.MaxActiveMatchColor);

        // Cấp danh sách TrayModel cho TrayController để xử lý mở khoá theo dependency.
        ITrayController trayController = ServiceLocator.Instance.Resolve<ITrayController>();
        trayController.Init(level);

        // Số tray của level để đếm điều kiện thắng (mỗi tray release xong đếm ngược).
        _contextData.gameController.SetNumberOfTray(level.TrayModels.Count);

        // Dựng quỹ đạo belt (chữ U) từ waypoint của level.
        IBeltController beltController = ServiceLocator.Instance.Resolve<IBeltController>();
        beltController.Init(level.BeltPathPoints, level.BeltCornerRadius, level.BeltVelocity, level.MaxCardsInBelt);

        // Đặt marker startBelt/endBelt vào đúng điểm đầu/cuối path sau khi path đã dựng.
        _contextData.gameController.PlaceBeltMarkers(beltController);

        foreach (TrayModel trayModel in level.TrayModels)
        {
            SpawnTray(trayModel);
        }
    }

    // Spawns a single tray at its model's transform via the factory. The factory
    // runs InitializeGameObject on spawn, which calls ReadyToStartGame on the
    // TrayView (and its cards), so the view is context-ready before Init.
    private void SpawnTray(TrayModel trayModel)
    {
        Quaternion rotation = Quaternion.Euler(trayModel.Rotation);
        TrayView trayView = _contextData.gameEntityFactory
            .Spawn<TrayView>(_trayPrefab, trayModel.Position, rotation);
        // Tray active nằm dưới trayHolderOnPlane (giữ world pose = trayModel.Position).
        // Khi MoveToSlot sẽ reparent sang TrayHolderCard; khi trả pool về Pool Holder.
        trayView.transform.SetParent(trayHolderOnPlane, worldPositionStays: true);
        trayView.Init(trayModel);
    }
}
public interface IGameContextSubscriber {
    void ReadyToStartGame(GameContextData gameContextData);
    void OnAllSubscribedReady() { }
}

public abstract class GameContextSubscriberBase : MonoBehaviour, IGameContextSubscriber {
    public abstract void ReadyToStartGame(GameContextData data);
    public virtual void OnAllSubscribedReady() { }
}

public record GameContextData(
    IGameController gameController,
    IGameEntityFactory gameEntityFactory
);