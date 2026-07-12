using UnityEngine;

public class CardModel
{
    public int Id;
    public CardColor color;

    public CardModel(int id, CardColor color)
    {
        Id = id;
        this.color = color;
    }
}

public class ConveyorCard
{
    public CardModel card;
    public Vector3 position;

    // Trạng thái match do MatchColorController đánh dấu; CardView quan sát các
    // trường này để tự dừng bám belt và xếp card vào slot (không cần controller
    // giữ tham chiếu tới View).
    public bool IsMatched;
    public int TrayId; // vị trí gốc của slot đã khớp
    public int MatchSlotIndex;        // thứ tự card trong slot (= NumberOfCards lúc khớp)
}

public enum CardColor
{
    Blue,
    Green,
    Orange,
    Red,
    Yellow
}