using System;
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

    private GameContextData _contextData;

    protected async void Start() {
        try
        {
            await ServiceInitializer.InitializeAsync();

            var gameController = FindFirstObjectByType<GameController>();

            var poolingService = ServiceLocator.Instance.Resolve<IPoolingService>();
            var factory = new GameEntityFactory(poolingService);

            _contextData = new(gameController,factory);
            factory.SetInitializer(sub => sub.ReadyToStartGame(_contextData));

            // Load Level
            LoadLevel();

            // Giair transition
            Debug.LogError("Giai Transition");
            //

            gameController.StopGame(false);

        }
        catch (Exception e) {
            Debug.LogError(e);
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