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

    // Vị trí world của card ở FRAME TRƯỚC (trước bước di chuyển của frame hiện tại).
    // MatchColorController dùng cặp (previousPosition, position) để phát hiện card
    // "quét qua" slot theo trục X trong frame này — kiểm tra crossing độc lập FPS,
    // thay cho việc so vị trí tức thời với cửa sổ tolerance (dễ nhảy qua khi giật frame).
    public Vector3 previousPosition;

    // Quãng đường (arc-length) đã đi dọc quỹ đạo belt. BeltController dùng để tính
    // position theo path chữ U thay vì cộng thẳng trục X; wrap khi vượt tổng độ dài.
    public float distance;

    // Hướng đi (tangent) của card trên belt tại vị trí hiện tại; CardView xoay card
    // theo hướng này để quay đầu mượt khi vào cua. Mặc định +X.
    public Vector3 direction = Vector3.right;

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