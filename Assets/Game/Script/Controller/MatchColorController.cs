using System.Collections.Generic;
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
    
    // Ngưỡng lệch trục X để coi 1 card là "trùng vị trí" với slot: chỉ so hiệu toạ
    // độ x, |card.x - slot.x| < ngưỡng thì match (bỏ qua y/z).
    private const float MatchTolerance = 0.1f;

    // Số card cần để hoàn thành 1 slot. LƯU Ý: lý tưởng nên lấy từ
    // TrayModel.Composition của tray tương ứng (mỗi tray trong level_test có
    // Count = 6). Interface CreateMatchColor không truyền count nên tạm dùng hằng
    // số này; nếu cần chính xác theo tray, thêm field required-count và truyền vào.
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
            NumberOfSlotsCards = DefaultSlotCapacity,
            SlotPosition = trayPosition.SlotPosition,
            SlotRotation = trayPosition.SlotRotation,
            SlotId = trayPosition.Id
        };

        _activeSlots.Add(model);
        return model;
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

            if (Mathf.Abs(conveyorCard.position.x - slot.SlotPosition.x) >= MatchTolerance)
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