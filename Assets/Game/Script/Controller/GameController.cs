using System;
using System.Collections.Generic;
using DG.Tweening;
using Extension;
using UnityEngine;
using UnityEngine.UI;
[Service(nameof(IGameController))]
public interface IGameController
{
    Transform TrayHolderCard { get; }
    
    void DistributionCard(List<CardView> cards, TrayModel trayModel, CardColor cardColor);

    void AddTrayMatchColor(TrayView trayView);

    void RemoveTrayMatchColor(TrayView trayView);

    TrayView GetTrayView(int trayId);

    void PlaceBeltMarkers(IBeltController beltController);

    void StopGame(bool isGameStop);
}
public class GameController : MonoBehaviour, IGameController
{
    [SerializeField] private Button _btnAddCard;
    [SerializeField] private GameObject _cardPrefab;
    
    [SerializeField] private GameObject startBelt;
    [SerializeField] private GameObject endBelt;
    [SerializeField] private Transform beltHolderCard;
    [SerializeField] private Transform trayHolderCard;
    public Transform TrayHolderCard => trayHolderCard;
    
    
    [Inject] private IBeltController _beltController;
    [Inject] private ITrayController _trayController;

    private const int CardsPerBatch = 10;
    private static readonly Vector3 SpawnPosition = new Vector3(1000, 0, 0);

    // Distribution "fly to belt" animation tuning.
    private const float FlyDuration = 0.5f;
    private const float JumpPower = 2f;
    private const int JumpCount = 1;
    private const float ScaleDuration = 0.2f;
    private const float StaggerDelay = 0.1f;
    private bool _isGameStop;
    private List<TrayView> _trayMatchColor = new List<TrayView>();
    

    private void Awake()
    {
        _isGameStop = true;
    }
    

    private void Start()
    {
        ServiceLocator.Instance.ResolveInjection(this);
    }

    
    private void Update()
    {
        if(_isGameStop) return;
        
        _beltController.UpdateCardPosition(Time.deltaTime);
    }
    

    public void DistributionCard(List<CardView> cards, TrayModel trayModel, CardColor cardColor)
    {
        if (startBelt == null)
        {
            Debug.LogError("[GameController] startBelt is not assigned.");
            return;
        }

        var counter = 0;
        for (var i = 0; i < cards.Count; i++)
        {
            if (cards[i].CardColor == cardColor)
            {
                
                FlyCardToBelt(cards[i], counter * StaggerDelay, trayModel);
                counter++;
            }
               
        }
    }

    // Tray đã MoveToSlot tự đăng ký làm slot match-color, để card match tra ra
    // TrayView theo TrayId (xem CardView.MoveIntoMatchSlot). Tránh add trùng.
    public void AddTrayMatchColor(TrayView trayView)
    {
        if (trayView == null || _trayMatchColor.Contains(trayView))
        {
            return;
        }

        _trayMatchColor.Add(trayView);
    }

    // Slot match-color đã hoàn thành (đủ card) → gỡ khỏi danh sách để list không
    // phình và không tra nhầm TrayId đã release. Gọi từ TrayView.CardFlyDone.
    public void RemoveTrayMatchColor(TrayView trayView)
    {
        if (trayView == null)
        {
            return;
        }

        _trayMatchColor.Remove(trayView);
    }

    public TrayView GetTrayView(int trayId)
    {
        var trayView = _trayMatchColor.Find(x => x.TrayId == trayId);
        return trayView;
    }

    // Đặt marker startBelt/endBelt vào đúng điểm đầu/cuối quỹ đạo belt và xoay theo
    // hướng đi tại đó (+X card → hướng path). Gọi sau khi belt.Init (GameContext).
    // Nhận belt qua tham số (không dùng _beltController inject) vì hàm được gọi từ
    // GameContext.LoadLevel trước khi GameController.Start resolve injection.
    public void PlaceBeltMarkers(IBeltController beltController)
    {
        if (startBelt != null)
        {
            startBelt.transform.position = beltController.StartPosition;
            startBelt.transform.rotation = Quaternion.FromToRotation(Vector3.right, beltController.StartDirection);
        }

        if (endBelt != null)
        {
            endBelt.transform.position = beltController.EndPosition;
            endBelt.transform.rotation = Quaternion.FromToRotation(Vector3.right, beltController.EndDirection);
        }
    }

    public void StopGame(bool isGameStop)
    {
        _isGameStop = isGameStop;
    }

    private void FlyCardToBelt(CardView cardView, float delay, TrayModel trayModel)
    {
        Transform cardTransform = cardView.transform;

        // Card đã rời tray gốc để bay lên belt: gỡ liên kết tray gốc ngay để click sau
        // đó (nếu còn nhận được) không phát lại nhóm màu.
        cardView.ClearOwnerTray();

        // The card was parented under the tray while it lived there. Hand it over to
        // the belt holder so its pose is now owned by the belt, keeping its current
        // world pose so it doesn't teleport at the start of the animation.
        cardTransform.SetParent(beltHolderCard, worldPositionStays: true);

        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(delay);
        sequence.Append(cardTransform
            .DOJump(startBelt.transform.position, JumpPower, JumpCount, FlyDuration)
            .SetEase(Ease.OutQuad));
        // Xoay card đúng hướng world của điểm đầu path lúc đáp — trùng rotation mà
        // CardView.Update sẽ set (FromToRotation(+X, StartDirection)) nên không giật.
        // Dùng DORotate (world) vì heading là hướng world, không phải local nữa.
        sequence.Join(cardTransform.DORotate(GetBeltHeadingRotation(), FlyDuration));
        sequence.OnComplete(() =>
        {
            var converyCard = _beltController.AddNewCard(cardView.CardColor);
            cardView.AddCoyorCard(converyCard);
            _trayController.OnCardDistributed(trayModel);
        });
    }

    // Góc euler WORLD card cần có khi đáp lên đầu belt: mặt +X của card hướng theo
    // StartDirection (tangent tại điểm đầu quỹ đạo). Khớp đúng với rotation mà
    // CardView.Update áp cho card khi bám belt, nên chuyển tiếp liền mạch.
    private Vector3 GetBeltHeadingRotation()
    {
        return Quaternion.FromToRotation(Vector3.right, _beltController.StartDirection).eulerAngles;
    }
}
