using DG.Tweening;
using Script.Engine.Manager.Pooling;
using UnityEngine;

public class CardView : MonoBehaviour, IGameContextSubscriber, IPoolReturnable
{
    [SerializeField] private MeshRenderer _meshRenderer;

    // One material per CardColor. Order in the Inspector MUST match the CardColor
    // enum: Red, Yellow, Blue, Green, Purple, Pink, Orange.
    [SerializeField] private Material[] _colorMaterials;

    private ConveyorCard _conveyorCard;
    private bool _isMatch = false;
    private CardColor _color;
    private IGameController _gameController;
    private IPoolingService _poolingService;
    public CardColor CardColor
    {
        get => _color;
    }

    private TrayView _trayView;
    
    // Thời gian bay từ belt vào đúng ô trong slot match. Đi theo style hằng số
    // duration của GameController.FlyCardToBelt.
    private const float MoveToSlotDuration = 0.3f;

    private void Update()
    {
        if (_isMatch)
        {
            return;
        }

        if (_conveyorCard == null)
        {
            return;
        }

        // Controller đã đánh dấu card khớp slot: dừng bám belt và về đúng ô trong slot.
        if (_conveyorCard.IsMatched)
        {
            MoveIntoMatchSlot();
            return;
        }

        transform.position = _conveyorCard.position;
    }

    private void MoveIntoMatchSlot()
    {
        _isMatch = true;

        _trayView = _gameController.GetTrayView(_conveyorCard.TrayId);
        if (_trayView == null)
        {
            Debug.LogError($"[CardView] Không tìm thấy TrayView cho TrayId {_conveyorCard.TrayId}; " +
                           "tray đã đăng ký AddTrayMatchColor chưa?");
            return;
        }

        // Bàn giao card cho CardHolder của tray, giữ nguyên world pose để không bị
        // teleport khi bắt đầu tween (giống GameController.FlyCardToBelt).
        transform.SetParent(_trayView.CardHolder, worldPositionStays: true);

        // GetCardPosition trả về local position trong CardHolder (xem TrayView), nên
        // tween bằng DOLocalMove; rotation về 0 để card thẳng theo ô slot.
        Vector3 targetPos = _trayView.GetCardPosition(_conveyorCard.MatchSlotIndex);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOLocalMove(targetPos, MoveToSlotDuration).SetEase(Ease.OutQuad));
        sequence.Join(transform.DOLocalRotate(Vector3.zero, MoveToSlotDuration));
        // Anim xong: báo cho tray-slot biết đã có thêm 1 card vào chỗ, để nó đếm và
        // release slot khi đủ số card (xem TrayView.CardFlyDone).
        sequence.OnComplete(() => _trayView.CardFlyDone());
    }

    public void ReadyToStartGame(GameContextData gameContextData)
    {
        _gameController = gameContextData.gameController;
        ResetState();
    }

    // Pool inject IPoolingService qua đây (lần spawn đầu). Giữ lại để card tự trả về
    // đúng typed pool bằng ReturnObjectToPool<CardView> (xem ReturnToPool).
    public void SetPoolingService(IPoolingService poolingService)
    {
        _poolingService = poolingService;
    }

    // Reset trạng thái runtime mỗi lần spawn (ReadyToStartGame chạy qua
    // InitializeGameObject ở cả spawn mới lẫn reuse) để card tái sử dụng sạch.
    private void ResetState()
    {
        transform.DOKill();
        _isMatch = false;
        _conveyorCard = null;
        _trayView = null;
    }

    // Trả card về typed pool (phải dùng generic ReturnObjectToPool<CardView>; bản
    // non-generic chỉ xử lý _objectPools nên không tìm thấy card → không deactivate).
    public void ReturnToPool()
    {
        transform.DOKill();
        transform.SetParent(null, worldPositionStays: false);
        _poolingService.ReturnObjectToPool<CardView>(this, gameObject);
    }

    public void Init(CardColor color)
    {
        _color = color;
        SetCardColor(color);
    }
    
    public void AddCoyorCard(ConveyorCard conveyorCard)
    {
        _conveyorCard = conveyorCard;
    }
    

    private void SetCardColor(CardColor color)
    {
        int index = (int)color;
        if (_colorMaterials == null || index < 0 || index >= _colorMaterials.Length)
        {
            Debug.LogError($"[CardView] No material assigned for color '{color}'.");
            return;
        }

        Material material = _colorMaterials[index];
        if (material == null)
        {
            Debug.LogError($"[CardView] Material for color '{color}' is null.");
            return;
        }

        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        _meshRenderer.sharedMaterial = material;
    }
    
}
