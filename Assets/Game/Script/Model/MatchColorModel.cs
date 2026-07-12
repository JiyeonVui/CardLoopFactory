using UnityEngine;

// Một slot match-color đang active trên sân. Được tạo khi 1 tray phát card ra
// belt, và bị xoá khi đã gom đủ số card cần thiết (NumberOfCards >= NumberOfSlots).
public class MatchColorModel
{
    // Id instance của slot đang active (dùng cho RemoveMatchColor).
    public int Id { get; set; }

    // Tray đã tạo ra slot này. Giữ lại để sau có thể tra số card cần hoàn thành
    // từ TrayModel.Composition (hiện tạm dùng hằng số, xem MatchColorController).
    public int TrayId { get; set; }
    
    public int SlotId  { get; set; }
    // Số card đã khớp vào slot cho tới lúc này. Cũng là chỉ số (index) chỗ đặt
    // card tiếp theo trong slot, để View xếp card thành hàng giống TrayView.
    public int NumberOfCards { get; set; }

    // Số card cần để hoàn thành slot (capacity). Khi NumberOfCards đạt giá trị
    // này thì slot hoàn thành và được giải phóng.
    public int NumberOfSlotsCards { get; set; }

    // Màu card mà slot này chấp nhận.
    public CardColor CardColor { get; set; }

    // Vị trí vật lý (world) của slot trên sân, cấp từ danh sách slot của level.
    public Vector3 SlotPosition { get; set; }

    // Góc xoay (euler độ) của slot, cấp từ level; áp cho card khi xếp vào slot.
    public Vector3 SlotRotation { get; set; }
}