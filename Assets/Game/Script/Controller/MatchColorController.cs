using System.Collections.Generic;
using System.Linq;
using Extension;
using UnityEngine;

[Service(nameof(IMatchColorController))]
public interface IMatchColorController
{
    /// <summary>
    /// Khởi tạo danh sách vị trí slot (đọc từ level JSON) + giới hạn số lượng
    /// MatchColor được active cùng lúc. Gọi lúc load level, trước khi game chạy.
    /// </summary>
    void Init(List<Vector3> slotPositions, List<Vector3> slotRotations, int maxActiveMatchColor);

    /// <summary>
    /// Tạo 1 MatchColorModel mới tại vị trí trống gần nhất, gắn với TrayId đã
    /// distribute card. Gọi khi tray phát card ra belt và slot màu đó chưa full.
    /// Trả về null nếu không còn slot trống (nên check IsMatchColorFull() trước).
    /// </summary>
    MatchColorModel CreateMatchColor(TrayModel trayModel);

    /// <summary>
    /// Kiểm tra 1 card đang chạy trên belt có khớp (khoảng cách + màu) với
    /// MatchColorModel nào đang active không. Set giá trị match lên card khi khớp.
    /// Gọi mỗi tick từ BeltController.UpdateCardPosition, cho từng card trên belt.
    /// </summary>
    void CheckMatchColor(ConveyorCard conveyorCard);

    /// <summary>
    /// Số lượng MatchColorModel đang active đã đạt giới hạn (lấy từ level JSON) chưa.
    /// </summary>
    bool IsMatchColorFull();

    /// <summary>
    /// Vị trí slot trống gần nhất tính từ 1 điểm tham chiếu (VD vị trí tray vừa
    /// distribute), dùng để chạy anim di chuyển slot/card tới vị trí đó.
    /// </summary>

    MatchColorSlot GetTrayPosition();

    /// <summary>
    /// Xoá 1 MatchColorModel khỏi danh sách active theo MatchId (khi slot đã
    /// hoàn thành hoặc bị huỷ), giải phóng lại vị trí đó cho slot khác dùng.
    /// </summary>
    void RemoveMatchColor(int trayId);
}

// Class (reference type) chứ không phải struct: slot cần mutate IsEmpty tại chỗ
// trong _slotsList (CreateMatchColor / RemoveMatchColor). Nếu là struct, biến lấy
// ra chỉ là bản copy nên set IsEmpty không ghi lại vào list → slot không đổi trạng
// thái và GetTrayPosition trả về đúng slot đó ở lần gọi kế tiếp.
public class MatchColorSlot
{
    public int Id;
    public bool IsEmpty;
    public Vector3 SlotPosition;
    public Vector3 SlotRotation;
}

public class MatchColorController : IMatchColorController
{
    
    // Fallback số card cần để hoàn thành 1 slot khi tray không có Composition hợp lệ.
    // Bình thường lấy đúng theo tổng card gốc của tray (GetRequiredCardCount).
    private const int DefaultSlotCapacity = 6;

    private readonly List<MatchColorModel> _activeSlots = new List<MatchColorModel>();

    private List<MatchColorSlot> _slotsList = new List<MatchColorSlot>();
    
    private int _maxActive;
    private int _nextMatchId;

    public void Init(List<Vector3> slotPositions, List<Vector3> slotRotations, int maxActiveMatchColor)
    {

        _slotsList.Clear();
        for (var id = 0; id < slotPositions.Count; id++)
        {
            // Rotations đi song song theo index với positions; nếu thiếu thì mặc định 0.
            Vector3 rotation = slotRotations != null && id < slotRotations.Count
                ? slotRotations[id]
                : Vector3.zero;

            var slotItem = new MatchColorSlot()
            {
                Id = id,
                IsEmpty = true,
                SlotPosition = slotPositions[id],
                SlotRotation = rotation
            };
            _slotsList.Add(slotItem);
        }

        _maxActive = Mathf.Max(0, maxActiveMatchColor);
        _activeSlots.Clear();
        _nextMatchId = 0;
    }

    public MatchColorModel CreateMatchColor(TrayModel trayModel)
    {
        if (IsMatchColorFull())
        {
            Debug.LogWarning("[MatchColorController] Đã đạt giới hạn slot active; bỏ qua CreateMatchColor.");
            return null;
        }

        var trayPosition = GetTrayPosition();
        if (trayPosition == null)
        {
            return null;
        }

        trayPosition.IsEmpty = false;

        MatchColorModel model = new MatchColorModel
        {
            Id = _nextMatchId++,
            TrayId = trayModel.Id,
            CardColor = trayModel.CardColor,
            NumberOfCards = 0,
            NumberOfSlotsCards = GetRequiredCardCount(trayModel),
            SlotPosition = trayPosition.SlotPosition,
            SlotRotation = trayPosition.SlotRotation,
            SlotId = trayPosition.Id
        };

        _activeSlots.Add(model);
        return model;
    }

    // Số card cần để hoàn thành slot = tổng card gốc của tray (Composition không bị
    // sửa khi phát card; chỉ _cardViews bị gỡ). KHÔNG dùng TrayModel.TotalCardCount vì
    // giá trị đó bị đếm ngược khi distribute nên lúc này đã về ~0. Fallback nếu thiếu
    // Composition để tránh slot không bao giờ release.
    private int GetRequiredCardCount(TrayModel trayModel)
    {
        if (trayModel?.Composition == null || trayModel.Composition.Count == 0)
        {
            return DefaultSlotCapacity;
        }

        return trayModel.Composition.Sum(group => group.Count);
    }

    public void CheckMatchColor(ConveyorCard conveyorCard)
    {
        if (conveyorCard == null || conveyorCard.card == null || conveyorCard.IsMatched)
        {
            return;
        }

        foreach (MatchColorModel slot in _activeSlots)
        {
            if (slot.NumberOfCards >= slot.NumberOfSlotsCards)
            {
                continue;
            }
            
            if (slot.CardColor != conveyorCard.card.color)
            {
                continue;
            }

            // Crossing thay vì "đang trong cửa sổ": card khớp đúng frame mà toạ độ X
            // của nó QUÉT QUA slot.x theo chiều tiến (+X) — tức slot.x nằm trong nửa
            // khoảng (previousX, currentX]. Cách này độc lập FPS: dù frame giật, bước
            // nhảy lớn cỡ nào thì khoảng quét vẫn chứa slot.x nên không bao giờ trượt
            // (khác kiểu so |card.x - slot.x| < tolerance cũ, dễ nhảy qua cửa sổ hẹp).
            // Trên belt chữ U, X không giảm dọc đường đi nên chỉ khớp một chiều; lúc
            // wrap (card teleport cuối→đầu) X nhảy lùi nên tự động không thoả điều kiện.
            float previousX = conveyorCard.previousPosition.x;
            float currentX = conveyorCard.position.x;
            if (!(previousX < slot.SlotPosition.x && slot.SlotPosition.x <= currentX))
            {
                continue;
            }

            // Khớp: chỉ đánh dấu ý định lên card (View tự xếp vào slot dựa trên các
            // flag này). Không đụng tới cardOnBelt — BeltController là chủ sở hữu duy
            // nhất của list đó và sẽ tự quét gỡ card đã IsMatched trong update của nó.
            conveyorCard.TrayId = slot.TrayId;
            conveyorCard.MatchSlotIndex = slot.NumberOfCards;
            conveyorCard.IsMatched = true;
            slot.NumberOfCards++;

            return;
        }
    }

    public bool IsMatchColorFull()
    {
        return _activeSlots.Count >= _maxActive;
    }

    public MatchColorSlot GetTrayPosition()
    {
        foreach (MatchColorSlot slot in _slotsList)
        {
            if (slot.IsEmpty)
            {
                return slot;
            }
        }

        Debug.LogWarning("[MatchColorController] Không còn slot trống.");
        return null;
    }
    
    public void RemoveMatchColor(int trayId)
    {
        var tray = _activeSlots.Find(x => x.TrayId == trayId);

        if (tray == null)
        {
            return;
        }

        var slot = _slotsList.Find(x => x.Id == tray.SlotId);
        if (slot != null)
        {
            slot.IsEmpty = true;
        }

        _activeSlots.Remove(tray);
    }
    
}