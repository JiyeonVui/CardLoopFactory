using System;
using System.Collections.Generic;
using DG.Tweening;
using Engine.Manager;
using Extension;
using Script.Engine.Manager.Pooling;
using UnityEngine;
using UnityEngine.EventSystems;

public class TrayView : MonoBehaviour, IPointerClickHandler, IGameContextSubscriber, IPoolReturnable
{
    // Fired when the user taps/clicks this tray on screen. Passes itself so a
    // single listener can tell which tray was tapped.

    [SerializeField] private List<MeshRenderer> _renderers = new List<MeshRenderer>();
    [SerializeField] private List<Material> _materials = new List<Material>();
    [SerializeField] private Transform _cardHolder;
    [SerializeField] private Transform _box;
    [SerializeField] private GameObject cardPrefab;
    
    private List<Vector3> _cardSlotPositions = new List<Vector3>()
    {
        new Vector3 (-0.69f, 0f, 2.1f),
        new Vector3 (0.69f, 0f, 2.1f),
        new Vector3 (-0.69f, 0f, 0.69f),
        new Vector3 (0.69f, 0f, 0.69f),
        new Vector3 (-0.69f, 0f, -0.69f),
        new Vector3 (0.69f, 0f, -0.69f),
        new Vector3 (-0.69f, 0f, -2.1f),
        new Vector3 (0.69f, 0f, -2.1f),
    };
    private List<CardView> _cardViews = new List<CardView>();
    private TrayModel _trayModel;
    private MatchColorModel _matchColorModel;
    private bool _isOpen = false;
    private int _numOfCardsDoneAnim;

    // Cooldown chống mash: 2 lần distribute liên tiếp trên cùng tray phải cách nhau
    // ít nhất ngần này (giây, unscaled). Đủ ngắn để thao tác cố ý không thấy lag.
    private const float DistributeCooldownSeconds = 0.1f;
    private float _nextDistributeTime;

    // Anim vào slot 2 nhịp: bay lên điểm A (ngay trên slot, cao ReachHeight) trong khi
    // xoay về đúng hướng, rồi đáp thẳng xuống slot. Đi theo style hằng số duration của
    // GameController/CardView.
    private const float MoveToReachHeight = 3f;
    private const float MoveToReachDuration = 0.3f;
    // Nhịp đáp dài hơn chút để nhìn rõ cú nảy khi chạm slot (Ease.OutBounce).
    private const float MoveToLandDuration = 0.35f;

    // Anim release khi slot đủ card: bay lên độ cao Y rồi bay ngang ra khỏi màn hình
    // theo trục X trước khi despawn về pool.
    private const float ReleaseUpHeight = 2f;
    private const float ReleaseAwayX = -100f;
    private const float ReleaseUpDuration = 0.3f;
    private const float ReleaseAwayDuration = 0.5f;
    private IGameController _gameController;
    private IGameEntityFactory _factory;
    // Inject mỗi lần spawn/reuse qua ReadyToStartGame → ResolveInjection, nên luôn có mặt
    // (không phụ thuộc SetPoolingService — đường tắt InactiveType của pool có thể bỏ qua nó).
    [Inject] private IPoolingService _poolingService;
    [Inject] private IMatchColorController _matchColorController;
    [Inject] private ITrayController _trayController;
    [Inject] private IManagerAudio _managerAudio;

    public int TrayId => _trayModel.Id;
    public Transform CardHolder => _cardHolder;
    private void Update()
    {
        if (_isOpen)
        {
            return;
        }
        
        if (_trayModel == null)
        {
            return;
        }

        if (!_trayModel.IsLocked)
        {
            OnOpen();
        }
    }

    public void ReadyToStartGame(GameContextData gameContextData)
    {
        _gameController = gameContextData.gameController;
        _factory = gameContextData.gameEntityFactory;
        ServiceLocator.Instance.ResolveInjection(this);
        ResetState();
    }

    // Pool inject IPoolingService qua đây (lần spawn đầu). Giữ để tray tự trả về đúng
    // typed pool bằng ReturnObjectToPool<TrayView> (xem PlayReleaseAnim).
    public void SetPoolingService(IPoolingService poolingService)
    {
        _poolingService = poolingService;
    }

    // Reset field runtime mỗi lần spawn. Đặt ở ReadyToStartGame (chạy qua
    // InitializeGameObject ở cả spawn mới lẫn reuse) thay vì SetPoolingService — hook
    // đó bị skip khi reuse do generic return cache InactiveType. Init() set lại
    // _trayModel + card ngay sau.
    private void ResetState()
    {
        transform.DOKill();
        _numOfCardsDoneAnim = 0;
        _isOpen = false;
        _matchColorModel = null;
        _cardViews.Clear();
    }

    public void Init(TrayModel trayModel)
    {
        _trayModel = trayModel;
        SetTrayMaterial(trayModel);
        CreateCards(trayModel);
        ApplyLockState(trayModel.IsLocked);
    }

    public void CardFlyDone()
    {
        _numOfCardsDoneAnim++;
        if (_numOfCardsDoneAnim == _matchColorModel.NumberOfSlotsCards)
        {
            PlayReleaseAnim();
        }
    }

    // Slot đã đủ card: bay lên độ cao Y rồi bay ngang theo trục X ra khỏi màn hình.
    // Kết thúc tween mới gỡ slot khỏi controller/list và despawn tray về pool.
    private void PlayReleaseAnim()
    {
        // Tray đủ card, chạy anim release: phát match sound 1 lần.
        _managerAudio.PlayMatchSound();

        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOLocalMoveY(ReleaseUpHeight, ReleaseUpDuration).SetEase(Ease.OutQuad));
        sequence.Append(transform.DOLocalMoveX(ReleaseAwayX, ReleaseAwayDuration).SetEase(Ease.InQuad));
        sequence.OnComplete(() =>
        {
            _matchColorController.RemoveMatchColor(_trayModel.Id);
            _gameController.RemoveTrayMatchColor(this);
            _gameController.OnTrayReleased();
            ReturnCardsToPool();
            _poolingService.ReturnObjectToPool<TrayView>(this, gameObject);
        });
    }

    // Despawn tray + toàn bộ card đang nằm dưới nó (card chưa phát lẫn card đã match)
    // về pool. Dùng cho retry reset in-scene: dừng tween đang chạy để không teleport,
    // trả card rồi trả chính tray về đúng typed pool.
    public void DespawnToPool()
    {
        transform.DOKill();

        // Gỡ tham chiếu tray này ở GameController (nếu đang là match-slot) để không còn
        // reference tới object đã trả pool. Field tray chưa lên slot thì đây là no-op.
        if (_gameController != null)
        {
            _gameController.RemoveTrayMatchColor(this);
        }

        // Trả card đang nằm dưới CardHolder của tray (card chưa phát lẫn card đã match).
        ReturnCardsToPool();
        _poolingService.ReturnObjectToPool<TrayView>(this, gameObject);
    }

    // Trả card đã match về pool trước khi despawn tray. Các card này được reparent
    // về _cardHolder (xem CardView.MoveIntoMatchSlot) nên không còn trong _cardViews.
    // Gom trước rồi mới trả để không sửa collection con trong lúc đang duyệt.
    private void ReturnCardsToPool()
    {
        // Card đã match được reparent về _cardHolder. Gom trước rồi mới trả để không
        // sửa collection con trong lúc duyệt. CardView.ReturnToPool tự detach khỏi
        // _cardHolder và trả về đúng typed pool bằng generic ReturnObjectToPool.
        List<CardView> cards = new List<CardView>();
        foreach (Transform child in _cardHolder)
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
    
    // Step 1: tint the tray renderers with the tray's color.
    private void SetTrayMaterial(TrayModel trayModel)
    {
        Material material = GetMaterial(trayModel.CardColor);
        if (material == null)
        {
            return;
        }

        foreach (MeshRenderer meshRenderer in _renderers)
        {
            meshRenderer.sharedMaterial = material;
        }
    }

    // Step 2: spawn one card per unit in the composition, placed on its slot.
    private void CreateCards(TrayModel trayModel)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("[TrayView] cardPrefab is not assigned.");
            return;
        }

        if (_factory == null)
        {
            Debug.LogError("[TrayView] GameEntityFactory is not ready; call ReadyToStartGame first.");
            return;
        }

        if (trayModel.Composition == null)
        {
            return;
        }

        int slotIndex = 0;
        foreach (CardGroup group in trayModel.Composition)
        {
            for (int i = 0; i < group.Count; i++)
            {
                if (slotIndex >= _cardSlotPositions.Count)
                {
                    Debug.LogWarning("[TrayView] More cards than slots; extra cards ignored.");
                    return;
                }

                // Reuse cards from the pool instead of Instantiate. Card tái sử dụng từ
                // pool đang nằm dưới Pool Holder, nên reparent tường minh về _cardHolder để
                // slot offset (localPosition) đúng theo hệ toạ độ của tray.
                CardView cardView = _factory.Spawn<CardView>(cardPrefab, _cardHolder, true);
                cardView.transform.SetParent(_cardHolder, worldPositionStays: false);
                cardView.transform.localPosition = _cardSlotPositions[slotIndex];
                cardView.Init(group.Color, this);

                _cardViews.Add(cardView);
                slotIndex++;
            }
        }
    }

    // Step 3: a locked tray shows its wings (closed); an unlocked tray shows cards.
    private void ApplyLockState(bool isLocked)
    {
        _box.gameObject.SetActive(isLocked);
        _cardHolder.gameObject.SetActive(!isLocked);
    }

    private Material GetMaterial(CardColor color)
    {
        int index = (int)color;
        if (_materials == null || index < 0 || index >= _materials.Count)
        {
            Debug.LogError($"[TrayView] No material assigned for color '{color}'.");
            return null;
        }

        return _materials[index];
    }

    public Vector3 GetCardPosition(int indexOfCard)
    {
        return _cardSlotPositions[indexOfCard];
    }
    
    // Click vào tray giờ chỉ dùng để "phát tray" khi đã hết card. Việc phát card
    // đã chuyển sang cho từng CardView (CardView.OnPointerClick → DistributionCard).
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_trayModel.IsLocked)
        {
            return;
        }

        if (_cardViews.Count == 0)
        {
            MoveToSlot();
        }
    }

    private void OnOpen()
    {
        _isOpen = true;
        _box.gameObject.SetActive(false);
        _cardHolder.gameObject.SetActive(true);
    }

    public void DistributionCard(CardColor cardColor)
    {
        // Guard chống mash: bỏ qua các cú chạm dồn dập trong khoảng cooldown, tránh
        // đếm/reserve thừa khi người chơi spam touch. Dùng unscaledTime để không phụ
        // thuộc Time.timeScale (game có thể pause).
        if (Time.unscaledTime < _nextDistributeTime)
        {
            return;
        }
        _nextDistributeTime = Time.unscaledTime + DistributeCooldownSeconds;

        int sent = _gameController.DistributionCard(_cardViews, _trayModel, cardColor);

        // Chỉ gỡ đúng `sent` card đầu tiên đúng màu (trùng đúng các card GameController
        // đã chọn để FlyCardToBelt, vì cùng duyệt _cardViews theo thứ tự). Phần dư giữ
        // lại trên tray để phát tiếp khi belt còn chỗ.
        var removed = 0;
        _cardViews.RemoveAll(card =>
        {
            if (removed >= sent || card.CardColor != cardColor)
            {
                return false;
            }

            removed++;
            return true;
        });
    }

    // Tray đã phát hết card: biến chính nó thành 1 slot match-color trên sân bằng
    // cách tạo MatchColorModel rồi bay tới vị trí slot đó.
    private void MoveToSlot()
    {
        // Không còn slot trống thì bỏ qua (CreateMatchColor cũng sẽ trả null).
        if (_matchColorController.IsMatchColorFull())
        {
            Debug.LogWarning("[TrayView] Đã đạt giới hạn slot match-color; bỏ qua MoveToSlot.");
            return;
        }

        _matchColorModel = _matchColorController.CreateMatchColor(_trayModel);
        if (_matchColorModel == null)
        {
            return;
        }

        // Tray đã move lên slot: đánh dấu IsUsed để không còn tính là "tray rỗng trên sân"
        // khi check thua (TrayController.HasEmptyTrayToMove).
        _trayModel.State = TrayState.IsUsed;

        // Tray di chuyển lên slot: phát distribute sound (kèm haptic trong Manager).
        _managerAudio.PlayDistributeSound();

        // Đăng ký làm slot match-color để card khớp tra ra TrayView này theo TrayId
        // (CardView.MoveIntoMatchSlot → GameController.GetTrayView). Đăng ký ngay để
        // card match được trong lúc tray còn đang bay vào slot.
        _gameController.AddTrayMatchColor(this);

        // SlotPosition/SlotRotation là toạ độ LOCAL so với TrayHolderCard (giá trị
        // đúng như hiển thị trong Inspector, VD rotation 0/90/0). Reparent giữ world
        // pose để không teleport lúc bắt đầu tween; anim tween thẳng tới local đích.
        Vector3 targetLocalPos = _matchColorModel.SlotPosition;
        Vector3 targetLocalRotation = _matchColorModel.SlotRotation;

        transform.SetParent(_gameController.TrayHolderCard, worldPositionStays: true);

        // Điểm A: ngay trên slot đích, cao thêm ReachHeight theo trục Y.
        Vector3 reachLocalPos = targetLocalPos + new Vector3(0f, MoveToReachHeight, 0f);

        Sequence sequence = DOTween.Sequence();
        // Nhịp 1: bay lên A, xoay về đúng hướng — chạy song song.
        sequence.Append(transform.DOLocalMove(reachLocalPos, MoveToReachDuration).SetEase(Ease.OutQuad));
        sequence.Join(transform.DOLocalRotate(targetLocalRotation, MoveToReachDuration));
        // Nhịp 2: đáp thẳng xuống slot, nảy nhẹ khi chạm.
        sequence.Append(transform.DOLocalMove(targetLocalPos, MoveToLandDuration).SetEase(Ease.OutBounce));
        // Kết thúc anim: tray coi như đã tiêu thụ → mở khoá các tray bị nó chặn.
        // View của các tray đó tự phản ứng qua Update (poll IsLocked).
        sequence.OnComplete(() => _trayController.UnlockTraysBlockedBy(_trayModel.Id));
    }
}
