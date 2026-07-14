using System.Collections.Generic;
using DG.Tweening;
using Script.Engine.Manager.Pooling;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardView : MonoBehaviour, IGameContextSubscriber, IPoolReturnable, IPointerClickHandler
{
    [SerializeField] private List<MeshRenderer> _meshRenderer;

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

    // Tray đích (match-slot) mà card bay vào khi khớp màu — set ở MoveIntoMatchSlot.
    private TrayView _trayView;

    // Tray gốc đang chứa card (set lúc TrayView.CreateCards). Click vào card sẽ nhờ
    // tray gốc này phát distribution cho đúng nhóm màu của card.
    private TrayView _ownerTray;

    // Cờ chống race: các card cạnh nhau nên 1 lần chạm có thể tới nhiều card; card
    // đã bắn tín hiệu distribution rồi thì không bắn lại.
    private bool _isDistributed = false;
    
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

        // Xoay card theo hướng đi trên belt để quay đầu mượt khi vào cua. Quy ước
        // mặt +X của card hướng theo chiều di chuyển (khớp GetBeltHeadingRotation=0
        // lúc belt đi thẳng +X), nên map +X → direction.
        if (_conveyorCard.direction != Vector3.zero)
        {
            transform.rotation = Quaternion.FromToRotation(Vector3.right, _conveyorCard.direction);
        }
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
        _ownerTray = null;
        _isDistributed = false;
    }

    // Trả card về typed pool (phải dùng generic ReturnObjectToPool<CardView>; bản
    // non-generic chỉ xử lý _objectPools nên không tìm thấy card → không deactivate).
    public void ReturnToPool()
    {
        transform.DOKill();
        transform.SetParent(null, worldPositionStays: false);
        _poolingService.ReturnObjectToPool<CardView>(this, gameObject);
    }

    public void Init(CardColor color, TrayView ownerTray)
    {
        _color = color;
        _ownerTray = ownerTray;
        SetCardColor(color);
    }

    // Click vào card: nhờ tray gốc phát distribution cho đúng nhóm màu của card.
    // Bỏ qua nếu card đã rời tray (đã bay lên belt -> ClearOwnerTray null _ownerTray).
    // KHÔNG tự set _isDistributed ở đây: card bay lên belt được đánh dấu trong
    // ClearOwnerTray(); card dư (belt hết chỗ) giữ nguyên để bấm phát tiếp. Chống mash
    // do TrayView.DistributionCard lo bằng cooldown.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isDistributed || _ownerTray == null)
        {
            return;
        }

        _ownerTray.DistributionCard(_color);
    }

    // Card đã rời tray gốc để bay lên belt: gỡ liên kết tray gốc (và đánh dấu đã
    // phát) để mọi click sau đó không phát lại nhóm màu. Gọi khi card bắt đầu bay.
    public void ClearOwnerTray()
    {
        _ownerTray = null;
        _isDistributed = true;
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

        foreach (var mesh in _meshRenderer)
        {
            mesh.sharedMaterial = material;
        }
    }
    
}
