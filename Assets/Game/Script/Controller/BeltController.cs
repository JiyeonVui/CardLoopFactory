using System.Collections.Generic;
using Extension;
using UnityEngine;
[Service(nameof(IBeltController))]
public interface IBeltController
{
    void Init(List<Vector3> waypoints, float cornerRadius, float velocity);
    ConveyorCard AddNewCard(CardColor color);
    void UpdateCardPosition(float deltaTime);

    // Pose điểm đầu/cuối quỹ đạo belt, để đặt marker startBelt/endBelt cho đúng.
    Vector3 StartPosition { get; }
    Vector3 EndPosition { get; }
    Vector3 StartDirection { get; }
    Vector3 EndDirection { get; }
}

public class BeltController : IBeltController
{
    private Queue<ConveyorCard> cardInQueue = new Queue<ConveyorCard>();
    private List<ConveyorCard> cardOnBelt = new List<ConveyorCard>();

    // Quỹ đạo belt (chữ U bo tròn), bake từ waypoint của level. Card di chuyển theo
    // arc-length dọc path này thay vì cộng thẳng trục X.
    private BeltPath _path;

    private float _velocity = 2.0f;

    private int _currentModelId;

    // Resolve trễ: MatchColorController được Provide sau BeltController nên chưa
    // sẵn ở constructor; lấy ở lần update đầu tiên khi game đã chạy.
    private IMatchColorController _matchColorController;

    // A card is considered to still occupy the start slot until it has moved
    // slightly past it, so we don't spawn a new card on top of a just-spawned one.
    // Đơn vị là arc-length dọc path.
    private const float StartSlotTolerance = 0.3f;

    // Số đoạn nội suy cho mỗi góc bo — càng lớn càng mượt.
    private const int CornerSegments = 8;

    // Gọi lúc load level (GameContext.LoadLevel) để dựng quỹ đạo belt từ waypoint JSON.
    public void Init(List<Vector3> waypoints, float cornerRadius, float velocity)
    {
        _path = new BeltPath(waypoints, cornerRadius, CornerSegments);
        _velocity = velocity;

        if (!_path.IsValid)
        {
            Debug.LogError("[BeltController] Belt path không hợp lệ; cần >= 2 waypoint trong level.");
        }
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
        return conveyorCard;
    }

    public void UpdateCardPosition(float deltaTime)
    {
        if (_path == null || !_path.IsValid)
        {
            return;
        }

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
        cardOnBelt.RemoveAll(conveyorCard => conveyorCard.IsMatched);
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
    }

    private bool HasCardAtStart()
    {
        foreach (ConveyorCard card in cardOnBelt)
        {
            // So theo arc-length: card vừa lên belt (distance ~ 0) coi như đang chiếm
            // slot đầu, tránh spawn chồng lên nhau.
            if (card.distance <= StartSlotTolerance)
            {
                return true;
            }
        }

        return false;
    }
}
