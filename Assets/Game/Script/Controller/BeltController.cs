using System;
using System.Collections.Generic;
using Extension;
using UnityEngine;
[Service(nameof(IBeltController))]
public interface IBeltController
{
    void Init(List<Vector3> waypoints, float cornerRadius, float velocity, int maxCardsInBelt);

    // Xoá sạch card đang chờ/đang trên belt + reset bộ đếm. Dùng khi retry reset in-scene
    // (CardView tương ứng do GameContext trả về pool riêng).
    void ClearCards();

    ConveyorCard AddNewCard(CardColor color);
    void UpdateCardPosition(float deltaTime);

    // Pose điểm đầu/cuối quỹ đạo belt, để đặt marker startBelt/endBelt cho đúng.
    Vector3 StartPosition { get; }
    Vector3 EndPosition { get; }
    Vector3 StartDirection { get; }
    Vector3 EndDirection { get; }

    // Giới hạn số card trên belt (đặt theo level). CurrentCardCount là số card đang
    // thực sự trên belt (để hiện current/max); AvailableSlots đã trừ cả card đang bay.
    int MaxCardsInBelt { get; }
    int CurrentCardCount { get; }
    int AvailableSlots { get; }

    void SetMaxCardsInBelt(int max);

    // Giữ trước tối đa `requested` chỗ; trả về số chỗ thực sự giữ được (<= requested).
    int ReserveSlots(int requested);

    // Bắn mỗi khi card đáp lên belt hoặc rời belt: (current, max) cho UI cập nhật.
    event Action<int, int> OnCardCountChanged;

    // Belt đã đạt số card tối đa (current == max).
    bool IsFull { get; }

    // Belt đầy VÀ không có card nào rời belt (match) suốt >= 1 vòng belt -> kẹt.
    // Dùng cho điều kiện thua (kết hợp với MatchColor slot đã dùng hết).
    bool IsStalledForFullLoop { get; }
}

public class BeltController : IBeltController
{
    private Queue<ConveyorCard> cardInQueue = new Queue<ConveyorCard>();
    private List<ConveyorCard> cardOnBelt = new List<ConveyorCard>();

    // Quỹ đạo belt (chữ U bo tròn), bake từ waypoint của level. Card di chuyển theo
    // arc-length dọc path này thay vì cộng thẳng trục X.
    private BeltPath _path;

    private float _velocity = 12.0f;

    private int _currentModelId;

    // Resolve trễ: MatchColorController được Provide sau BeltController nên chưa
    // sẵn ở constructor; lấy ở lần update đầu tiên khi game đã chạy.
    private IMatchColorController _matchColorController;

    // A card is considered to still occupy the start slot until it has moved
    // slightly past it, so we don't spawn a new card on top of a just-spawned one.
    // Đơn vị là arc-length dọc path.
    private const float StartSlotTolerance = 1.8f;

    // Số đoạn nội suy cho mỗi góc bo — càng lớn càng mượt.
    private const int CornerSegments = 8;

    // Giới hạn mặc định nếu level không khai báo MaxCardsInBelt (hoặc khai báo <= 0).
    private const int DefaultMaxCardsInBelt = 24;
    private int _maxCardsInBelt = DefaultMaxCardsInBelt;

    // Card đã reserve lúc bấm distribute và đang bay tới belt nhưng CHƯA AddNewCard.
    // Chỉ dùng để tính chỗ trống (chống overshoot khi bấm nhanh), KHÔNG hiển thị lên UI.
    private int _pendingCount;

    // Thời gian (giây) kể từ lần cuối belt thay đổi thành viên (có card lên/rời belt).
    // Dùng để phát hiện belt kẹt: đầy mà suốt >= 1 vòng không card nào match.
    private float _stallTimer;

    public int MaxCardsInBelt => _maxCardsInBelt;
    public int CurrentCardCount => cardInQueue.Count + cardOnBelt.Count;
    public int AvailableSlots => Mathf.Max(0, _maxCardsInBelt - CurrentCardCount - _pendingCount);

    public bool IsFull => CurrentCardCount >= _maxCardsInBelt;

    public bool IsStalledForFullLoop
    {
        get
        {
            if (!IsFull || _path == null || _velocity <= 0f || _path.TotalLength <= 0f)
            {
                return false;
            }

            float loopTime = _path.TotalLength / _velocity;
            return _stallTimer >= loopTime;
        }
    }

    public event Action<int, int> OnCardCountChanged;

    // Gọi lúc load level (GameContext.LoadLevel) để dựng quỹ đạo belt từ waypoint JSON.
    public void Init(List<Vector3> waypoints, float cornerRadius, float velocity, int maxCardsInBelt)
    {
        _path = new BeltPath(waypoints, cornerRadius, CornerSegments);
        _velocity = velocity;
        _maxCardsInBelt = maxCardsInBelt > 0 ? maxCardsInBelt : DefaultMaxCardsInBelt;
        _pendingCount = 0;

        if (!_path.IsValid)
        {
            Debug.LogError("[BeltController] Belt path không hợp lệ; cần >= 2 waypoint trong level.");
        }

        RaiseCardCountChanged();
    }

    // Reset toàn bộ card trên belt về rỗng (giữ nguyên path/velocity đã Init). Bộ đếm
    // model id về 0 để lần chơi mới bắt đầu lại từ đầu.
    public void ClearCards()
    {
        cardInQueue.Clear();
        cardOnBelt.Clear();
        _pendingCount = 0;
        _stallTimer = 0f;
        _currentModelId = 0;
        RaiseCardCountChanged();
    }

    public void SetMaxCardsInBelt(int max)
    {
        _maxCardsInBelt = Mathf.Max(0, max);
        RaiseCardCountChanged();
    }

    // Belt 19/24, requested = 7 -> trả về 5. Belt 24/24 -> trả về 0.
    public int ReserveSlots(int requested)
    {
        if (requested <= 0)
        {
            return 0;
        }

        int granted = Mathf.Min(requested, AvailableSlots);
        _pendingCount += granted;
        return granted;
    }

    private void RaiseCardCountChanged()
    {
        OnCardCountChanged?.Invoke(CurrentCardCount, _maxCardsInBelt);
    }

    public Vector3 StartPosition => _path != null ? _path.GetPositionAtDistance(0f) : Vector3.zero;
    public Vector3 StartDirection => _path != null ? _path.GetDirectionAtDistance(0f) : Vector3.right;
    public Vector3 EndPosition => _path != null ? _path.GetPositionAtDistance(_path.TotalLength) : Vector3.zero;
    public Vector3 EndDirection => _path != null ? _path.GetDirectionAtDistance(_path.TotalLength) : Vector3.right;


    public ConveyorCard AddNewCard(CardModel cardModel)
    {
        ConveyorCard newCard = new ConveyorCard
        {
            card = cardModel,
            position = StartPosition,
            direction = StartDirection,
            distance = 0f
        };

        cardInQueue.Enqueue(newCard);
        return newCard;
    }
    
    public ConveyorCard AddNewCard(CardColor cardColor)
    {
        CardModel cardModel = new CardModel(_currentModelId, cardColor);
        ConveyorCard conveyorCard = new ConveyorCard()
        {
            card = cardModel,
            position = StartPosition,
            direction = StartDirection,
            distance = 0f
        };
        cardInQueue.Enqueue(conveyorCard);
        _currentModelId++;

        // Card đã "chạy vào belt": chuyển 1 chỗ từ pending sang thực -> current tăng 1.
        _pendingCount = Mathf.Max(0, _pendingCount - 1);
        RaiseCardCountChanged();

        return conveyorCard;
    }

    public void UpdateCardPosition(float deltaTime)
    {
        if (_path == null || !_path.IsValid)
        {
            return;
        }

        // Đo thời gian belt "đứng yên về thành viên" để phát hiện kẹt (reset khi có
        // card lên/rời belt ở TrySpawnCardAtStart / RemoveMatchedCards).
        _stallTimer += deltaTime;

        // Logic 1: move every card already on the belt along the U-shaped path.
        if (cardOnBelt.Count > 0)
        {
            MoveCardsAlongBelt(deltaTime);
            CheckMatches();
            RemoveMatchedCards();
        }

        // Logic 2: if the start slot is free, feed the next queued card onto the belt.
        TrySpawnCardAtStart();
    }

    // BeltController là chủ sở hữu duy nhất của cardOnBelt. MatchColorController chỉ
    // đánh dấu IsMatched; ở đây belt gỡ card đã match khỏi list trong một lần quét
    // sau CheckMatches. Chỉ gỡ tham chiếu khỏi cardOnBelt — KHÔNG huỷ ConveyorCard,
    // nên CardView vẫn giữ tham chiếu và tự animate card vào slot an toàn.
    private void RemoveMatchedCards()
    {
        int removed = cardOnBelt.RemoveAll(conveyorCard => conveyorCard.IsMatched);
        if (removed > 0)
        {
            _stallTimer = 0f;   // có card match rời belt -> belt còn tiến triển, không kẹt.
            RaiseCardCountChanged();
        }
    }

    // Cho từng card trên belt hỏi MatchColorController xem có khớp slot nào không.
    // CheckMatchColor giờ chỉ set flag IsMatched (không mutate cardOnBelt), việc gỡ
    // card đã match do RemoveMatchedCards() lo ngay sau vòng này.
    private void CheckMatches()
    {
        _matchColorController ??= ServiceLocator.Instance.Resolve<IMatchColorController>();

        for (int i = cardOnBelt.Count - 1; i >= 0; i--)
        {
            _matchColorController.CheckMatchColor(cardOnBelt[i]);
        }
    }

    private void MoveCardsAlongBelt(float deltaTime)
    {
        float step = _velocity * deltaTime;

        foreach (ConveyorCard card in cardOnBelt)
        {
            if (card.IsMatched)
            {
                continue;
            }

            // Tiến theo arc-length dọc path; belt là vòng lặp nên wrap về đầu khi vượt
            // tổng độ dài (card "nhảy" từ cuối ∪ về đầu, giống hành vi teleport cũ).
            card.distance += step;
            if (_path.TotalLength > 0f)
            {
                card.distance %= _path.TotalLength;
            }

            card.position = _path.GetPositionAtDistance(card.distance);
            card.direction = _path.GetDirectionAtDistance(card.distance);
        }
    }

    private void TrySpawnCardAtStart()
    {
        if (cardInQueue.Count == 0)
        {
            return;
        }

        if (HasCardAtStart())
        {
            return;
        }

        ConveyorCard nextCard = cardInQueue.Dequeue();
        nextCard.distance = 0f;
        nextCard.position = StartPosition;
        nextCard.direction = StartDirection;
        cardOnBelt.Add(nextCard);

        // Card mới lên belt -> cho nó 1 vòng để có cơ hội match trước khi coi là kẹt.
        _stallTimer = 0f;
    }

    private bool HasCardAtStart()
    {
        float total = _path.TotalLength;

        foreach (ConveyorCard card in cardOnBelt)
        {
            // So theo arc-length quanh "đường nối" của belt vòng lặp: card vừa lên belt
            // (distance ~ 0) HOẶC card sắp wrap từ cuối về đầu (distance ~ total) đều coi
            // như đang chiếm slot đầu -> chặn spawn để không chồng lên nhau tại điểm đầu.
            if (card.distance <= StartSlotTolerance ||
                card.distance >= total - StartSlotTolerance)
            {
                return true;
            }
        }

        return false;
    }
}
