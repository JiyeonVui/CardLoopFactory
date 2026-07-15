using System;
using System.Collections.Generic;
using DG.Tweening;
using Extension;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
[Service(nameof(IGameController))]
public interface IGameController
{
    Transform TrayHolderCard { get; }
    
    // Trả về số card thực sự được phát lên belt (có thể < số card đúng màu nếu belt
    // sắp đầy). TrayView dùng số này để chỉ gỡ đúng các card đã bay.
    int DistributionCard(List<CardView> cards, TrayModel trayModel, CardColor cardColor);

    void AddTrayMatchColor(TrayView trayView);

    void RemoveTrayMatchColor(TrayView trayView);

    TrayView GetTrayView(int trayId);

    void PlaceBeltMarkers(IBeltController beltController);

    void StopGame(bool isGameStop);

    // Số tray của level; set lúc load level để đếm điều kiện thắng.
    void SetNumberOfTray(int count);

    // Gọi khi 1 tray chạy xong release anim: đếm ngược, = 0 thì thắng.
    void OnTrayReleased();

    // Bắn khi game kết thúc. GameContext lắng nghe để hiện màn kết quả (UIManager).
    event Action OnWin;
    event Action OnLose;

    // Reset trạng thái runtime cho lần chơi lại (retry). Xoá danh sách tray match-color
    // và bộ đếm tray; LoadLevel sau đó sẽ set lại số tray.
    void ResetForReplay();
}
public class GameController : MonoBehaviour, IGameController
{
    [SerializeField] private Button _btnAddCard;
    [SerializeField] private GameObject _cardPrefab;

    // Text hiển thị số card hiện tại/tối đa trên belt (current/max). Kéo vào inspector.
    [SerializeField] private TMP_Text _beltCountLabel;
    
    [SerializeField] private GameObject startBelt;
    [SerializeField] private GameObject endBelt;
    [SerializeField] private Transform beltHolderCard;
    [SerializeField] private Transform trayHolderCard;
    public Transform TrayHolderCard => trayHolderCard;
    public Transform BeltHolderCard => beltHolderCard;
    
    
    [Inject] private IBeltController _beltController;
    [Inject] private ITrayController _trayController;
    [Inject] private IMatchColorController _matchColorController;

    private const int CardsPerBatch = 10;
    private static readonly Vector3 SpawnPosition = new Vector3(1000, 0, 0);

    // Distribution "fly to belt" animation tuning.
    private const float FlyDuration = 0.5f;
    private const float JumpPower = 2f;
    private const int JumpCount = 1;
    private const float ScaleDuration = 0.2f;
    private const float StaggerDelay = 0.1f;

    // Card đi vào ống ở đầu belt: nhảy cung lên miệng ống (cao PipeHeight theo Y so với
    // startBelt) rồi rơi thẳng xuống đúng vị trí startBelt (chui qua ống).
    private const float PipeHeight = 7f;
    private const float PipeDropDuration = 0.25f;
    private bool _isGameStop;
    private List<TrayView> _trayMatchColor = new List<TrayView>();

    public event Action OnWin;
    public event Action OnLose;

    // Số tray còn lại của level; đếm ngược mỗi khi 1 tray release xong, = 0 thì thắng.
    private int _numberOfTray;
    

    private void Awake()
    {
        _isGameStop = true;
    }
    

    private void Start()
    {
        ServiceLocator.Instance.ResolveInjection(this);

        // Lắng nghe belt để cập nhật text current/max. Vẽ luôn lần đầu theo trạng thái
        // hiện tại (Init đã chạy trước ở GameContext.LoadLevel).
        _beltController.OnCardCountChanged += UpdateBeltCountUI;
        UpdateBeltCountUI(_beltController.CurrentCardCount, _beltController.MaxCardsInBelt);
    }

    private void OnDestroy()
    {
        if (_beltController != null)
        {
            _beltController.OnCardCountChanged -= UpdateBeltCountUI;
        }
    }

    // Cập nhật text số card trên belt. Bắn từ BeltController.OnCardCountChanged mỗi khi
    // card đáp lên belt hoặc rời belt.
    private void UpdateBeltCountUI(int current, int max)
    {
        if (_beltCountLabel != null)
        {
            _beltCountLabel.text = $"{current}/{max}";
        }
    }

    
    private void Update()
    {
        if(_isGameStop) return;

        _beltController.UpdateCardPosition(Time.deltaTime);
        CheckLose();
    }

    // Thua khi cả hai đều đúng: (1) match-color slot đã dùng hết (không tạo thêm slot
    // được), (2) belt đầy và suốt >= 1 vòng không card nào match -> kẹt, không tiến
    // triển được nữa. StopGame để không log lặp lại.
    private void CheckLose()
    {
        if (_matchColorController.IsMatchColorFull() && _beltController.IsStalledForFullLoop)
        {
            Debug.LogError("Lose");
            StopGame(true);
            OnLose?.Invoke();
        }
    }
    

    public int DistributionCard(List<CardView> cards, TrayModel trayModel, CardColor cardColor)
    {
        if (startBelt == null)
        {
            Debug.LogError("[GameController] startBelt is not assigned.");
            return 0;
        }

        // Đếm số card đúng màu muốn phát.
        var wanted = 0;
        for (var i = 0; i < cards.Count; i++)
        {
            if (cards[i].CardColor == cardColor)
            {
                wanted++;
            }
        }

        if (wanted == 0)
        {
            return 0;
        }

        // Giữ chỗ theo chỗ còn trống. Belt 24/24 -> granted = 0 -> dừng luôn.
        int granted = _beltController.ReserveSlots(wanted);
        if (granted <= 0)
        {
            return 0;
        }

        var counter = 0;
        for (var i = 0; i < cards.Count && counter < granted; i++)
        {
            if (cards[i].CardColor != cardColor)
            {
                continue;
            }

            FlyCardToBelt(cards[i], counter * StaggerDelay, trayModel);
            counter++;
        }

        return granted;
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

    public void ResetForReplay()
    {
        StopGame(true);
        _trayMatchColor.Clear();
        _numberOfTray = 0;
    }

    public void SetNumberOfTray(int count)
    {
        _numberOfTray = Mathf.Max(0, count);
    }

    // 1 tray đã release xong: đếm ngược. Hết tray -> thắng.
    public void OnTrayReleased()
    {
        _numberOfTray--;
        if (_numberOfTray <= 0)
        {
            Debug.LogError("Win");
            StopGame(true);
            OnWin?.Invoke();
        }
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

        // Đích cuối = đầu belt; miệng ống ở ngay trên đó, cao PipeHeight theo trục Y world.
        Vector3 targetPos = startBelt.transform.position;
        Vector3 pipeMouthPos = targetPos + Vector3.up * PipeHeight;

        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(delay);
        // Nhịp 1: nhảy cung lên miệng ống, đồng thời xoay đúng hướng world của điểm đầu
        // path — trùng rotation mà CardView.Update sẽ set (FromToRotation(+X,
        // StartDirection)) nên không giật. Dùng DORotate (world) vì heading là hướng world.
        sequence.Append(cardTransform
            .DOJump(pipeMouthPos, JumpPower, JumpCount, FlyDuration)
            .SetEase(Ease.OutQuad));
        sequence.Join(cardTransform.DORotate(GetBeltHeadingRotation(), FlyDuration));
        // Nhịp 2: rơi thẳng xuống targetpos (chui qua ống), tăng tốc khi rơi.
        sequence.Append(cardTransform
            .DOMove(targetPos, PipeDropDuration)
            .SetEase(Ease.InQuad));
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
