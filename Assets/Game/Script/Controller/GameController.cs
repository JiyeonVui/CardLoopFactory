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
    
    void DistributionCard(List<CardView> cards, TrayModel trayModel);

    void AddTrayMatchColor(TrayView trayView);

    void RemoveTrayMatchColor(TrayView trayView);

    TrayView GetTrayView(int trayId);
    
    void StopGame(bool isGameStop);
}
public class GameController : MonoBehaviour, IGameController
{
    [SerializeField] private Button _btnAddCard;
    [SerializeField] private GameObject _cardPrefab;
    
    [SerializeField] private GameObject startBelt;
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
    

    public void DistributionCard(List<CardView> cards, TrayModel trayModel)
    {
        if (startBelt == null)
        {
            Debug.LogError("[GameController] startBelt is not assigned.");
            return;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            FlyCardToBelt(cards[i], i * StaggerDelay, trayModel);
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

    public void StopGame(bool isGameStop)
    {
        _isGameStop = isGameStop;
    }

    private void FlyCardToBelt(CardView cardView, float delay, TrayModel trayModel)
    {
        Transform cardTransform = cardView.transform;

        // The card was parented under the tray while it lived there. Hand it over to
        // the belt holder so its pose is now owned by the belt, keeping its current
        // world pose so it doesn't teleport at the start of the animation.
        cardTransform.SetParent(beltHolderCard, worldPositionStays: true);

        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(delay);
        sequence.Append(cardTransform
            .DOJump(startBelt.transform.position, JumpPower, JumpCount, FlyDuration)
            .SetEase(Ease.OutQuad));
        // Face the card along the belt's travel direction as it lands. The belt is
        // currently a straight left->right line, whose heading (+X) maps to a local
        // rotation of 0 relative to beltHolderCard. See GetBeltHeadingRotation() for
        // how this generalises to curved belts.
        sequence.Join(cardTransform.DOLocalRotate(GetBeltHeadingRotation(), FlyDuration));
        sequence.OnComplete(() =>
        {
            var converyCard = _beltController.AddNewCard(cardView.CardColor);
            cardView.AddCoyorCard(converyCard);
            _trayController.OnCardDistributed(trayModel);
        });
    }

    // The local euler angles a card should have so its +X face points along the belt's
    // direction of travel. Today the belt runs straight left->right (heading = +X), so
    // aligned with beltHolderCard means zero rotation. When belts gain curved paths this
    // becomes a lookup of the path tangent at the card's position (see notes below).
    private Vector3 GetBeltHeadingRotation()
    {
        return Vector3.zero;
    }
}
