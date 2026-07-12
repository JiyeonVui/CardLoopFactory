using System.Collections.Generic;
using Extension;
using UnityEngine;
[Service(nameof(IBeltController))]
public interface IBeltController
{
    ConveyorCard AddNewCard(CardColor color);
    void UpdateCardPosition(float deltaTime);

}

public class BeltController : IBeltController
{
    private Queue<ConveyorCard> cardInQueue = new Queue<ConveyorCard>();
    private List<ConveyorCard> cardOnBelt = new List<ConveyorCard>();
    
    private Vector3 _startBelt;
    private Vector3 _endBelt;

    private float _velocity;

    private int _currentModelId;

    // Resolve trễ: MatchColorController được Provide sau BeltController nên chưa
    // sẵn ở constructor; lấy ở lần update đầu tiên khi game đã chạy.
    private IMatchColorController _matchColorController;

    // A card is considered to still occupy the start slot until it has moved
    // slightly past it, so we don't spawn a new card on top of a just-spawned one.
    private const float StartSlotTolerance = 0.3f;

    public BeltController()
    {
        InitForTest();
    }
    
    private void InitForTest()
    {
        _startBelt = new Vector3(-4,0,0);
        _endBelt = new Vector3(4,0,0);
        _velocity = 2.0f;
    }
    

    public ConveyorCard AddNewCard(CardModel cardModel)
    {
        ConveyorCard newCard = new ConveyorCard
        {
            card = cardModel,
            position = _startBelt
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
            position = _startBelt
        };
        cardInQueue.Enqueue(conveyorCard);
        _currentModelId++;
        return conveyorCard;
    }

    public void UpdateCardPosition(float deltaTime)
    {
        // Logic 1: move every card already on the belt from left (start) to right (end).
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
            
            Vector3 next = card.position;
            next.x += step;

            // The belt is a horizontal loop: once past the end, wrap back to the start.
            if (next.x > _endBelt.x)
            {
                next = _startBelt;
            }

            card.position = next;
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
        nextCard.position = _startBelt;
        cardOnBelt.Add(nextCard);
    }

    private bool HasCardAtStart()
    {
        foreach (ConveyorCard card in cardOnBelt)
        {
            if (Vector3.Distance(card.position, _startBelt) <= StartSlotTolerance)
            {
                return true;
            }
        }

        return false;
    }
}
