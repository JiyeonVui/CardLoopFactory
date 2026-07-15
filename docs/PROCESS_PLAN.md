# Card Loop Factory — Process Plan (Kế hoạch triển khai)

> Tài liệu 1 trang: cách scope dự án, các milestone, những gì đã cắt và lý do, và chỗ dồn "polish budget".

## 1. Tổng quan game (1 dòng)

Game puzzle casual 3D: người chơi **bấm vào tray** để đẩy card theo màu lên một **băng chuyền chữ U**; card chạy vòng quanh và tự **khớp vào các match-slot cùng màu**. Thắng khi dọn sạch mọi tray, thua khi băng chuyền kẹt đầy mà không còn nước đi tạo tiến triển.

## 2. Quy trình lên kế hoạch & phát triển (6 bước)

### Bước 1 — Chơi game mẫu & break down cơ chế

Chơi game tham chiếu, phân tích gameplay và tách thành **4 cơ chế chính**:

1. **Di chuyển thẻ trên belt** — card chạy dọc quỹ đạo băng chuyền (chữ U), bám path và quay đầu khi vào cua.
2. **Distribution card đến tray** — phát card từ tray lên belt theo nhóm màu (khi người chơi bấm tray).
3. **Match card vào lại tray đúng màu** — card trên belt khớp vào tray/slot cùng màu.
4. **Release tray** — tray đủ điều kiện thì được giải phóng (dọn khỏi sân), tiến tới điều kiện thắng.

### Bước 2 — Thiết kế kiến trúc code (MVC)

Nguyên tắc kiến trúc:

- **MVC rõ ràng**: **Controller** thay đổi giá trị của **Model**; **View** đọc giá trị Model để hiển thị. View không tự quyết logic.
- **Controller & Model viết bằng C# thuần, KHÔNG phụ thuộc Unity** → mục đích: có thể tách ra **auto-test** khi có nhiều level (test logic không cần chạy engine).
- **Hạn chế Singleton.** Dùng **Service Locator** làm cơ chế **Dependency Injection** (`[Service]` / `[Inject]`).
- **Level = file JSON** (data-driven), không hard-code trong code.

Liệt kê **Model / Controller / View** dự kiến:

| Model (C# thuần) | Controller (C# thuần / logic) | View (MonoBehaviour) |
|------------------|-------------------------------|----------------------|
| `CardModel`, `ConveyorCard` | `GameController` (điều phối, win/lose) | `CardView` |
| `TrayModel` | `BeltController` (+ `BeltPath`) — cơ chế 1 | `TrayView` |
| `MatchColorModel`, `MatchColorSlot` | `TrayController` — cơ chế 2 | `BeltArrowAnimator` |
| (Level data từ JSON) | `MatchColorController` — cơ chế 3 | `UIManager` + các `UIScreen` |
| | `LevelManager` (đọc JSON), `GameContext` (bootstrap) | Start / Result / Transition / Splash |

### Bước 3 — Code kiến trúc cơ bản

Dựng khung theo đúng phân tách MVC ở trên. **Được phép hard-code ở bước này** (giá trị tạm, level mẫu) nhưng **kiến trúc phải rõ ràng** — đúng ranh giới Model/Controller/View, đúng cơ chế DI.

### Bước 4 — Vibe code với AI

- Thay các chỗ đang **hard-code** bằng data/level thật.
- Viết **mô tả yêu cầu**, nhờ Claude soạn **prompt cho agent** thực thi.
- Trọng tâm bước này: tinh chỉnh **game feel** — animation, sound, kiểm tra **win/lose**, dựng **prototype-flow**.

### Bước 5 — Chọn asset

- Vì dùng **asset free** nên chọn lọc một số asset phù hợp (xem [ART_ASSETS.md](./ART_ASSETS.md)).
- Nếu **không tìm được asset phù hợp** → **quay lại Bước 4** để điều chỉnh **concept theo asset** đang có.

### Bước 6 — Test & điều chỉnh

Test toàn game, playtest cân bằng độ khó, sửa lỗi và tinh chỉnh lại (lặp về các bước trước khi cần).

## 3. Cách scope dự án (Scoping)

Triết lý: **cắt theo chiều rộng, giữ theo chiều sâu** — làm một vòng gameplay (core loop) chạy mượt và "cảm giác đã tay" thay vì nhiều tính năng nửa vời.

- **Core loop tối thiểu, có thể chơi được**: tray → phát card → belt chạy vòng → match slot → thắng/thua. Đây là phần bất khả xâm phạm, mọi thứ khác là phụ.
- **Data-driven ngay từ đầu**: toàn bộ level (vị trí tray, thành phần màu, quỹ đạo belt, số slot, tốc độ) nằm trong `level_test.json`. Nhờ vậy scope "làm thêm level" = thêm data, **không đụng code**.
- **Kiến trúc gọn nhưng đúng chuẩn**: Service Locator + Dependency Injection (`[Inject]`/`[Service]`), Object Pooling cho card, tách rõ Model / View / Controller. Đủ để mở rộng nhưng không over-engineer.
- **Tận dụng asset pack có sẵn** (Kenney Factory Kit, 300Mind UI Kit, Casual Game Sounds) thay vì tự làm art từ đầu — dồn thời gian còn lại cho gameplay & polish. Chi tiết ở [ART_ASSETS.md](./ART_ASSETS.md).
- **Nền tảng đích: mobile portrait** (casual/hyper-casual). Mọi quyết định về input (1-chạm), độ nặng (poly-count thấp, pooling) đều bám theo ràng buộc này.

## 4. Milestones

| # | Milestone | Nội dung chính | Trạng thái |
|---|-----------|----------------|------------|
| M0 | **Bootstrap** | Project Unity, Service Locator, pooling, cấu trúc MVC, load level từ JSON | ✅ Xong |
| M1 | **Core loop chơi được** | Tray → distribute card → belt chữ U chạy → match slot → điều kiện Win/Lose | ✅ Xong |
| M2 | **Belt & path polish** | Belt bo cua, card bám path & quay đầu mượt, đếm current/max, giữ chỗ (reserve slot) | ✅ Xong |
| M3 | **Phân phối theo màu từ click** | Bấm tray phát đúng nhóm màu, tray khoá/mở theo `BlockedByTrayIds` | ✅ Xong |
| M4 | **Audio & Haptic** | ScriptableObject Audio, ManagerAudio (throttle), HapticController, nhạc nền | ✅ Xong |
| M5 | **UI/UX flow** | Splash → Start → Game → Result, màn Transition, mây trang trí, arrow animator | 🔄 Đang làm |
| M6 | **Content & Balancing** | Nhiều level thật, tuning độ khó (số màu, số slot, tốc độ belt) | ⬜ Kế tiếp |
| M7 | **Polish & Ship** | VFX match/collect, feedback đã tay, tối ưu mobile, build & test thiết bị | ⬜ Kế tiếp |

## 5. Những gì đã CẮT và lý do (Cut list)

| Đã cắt | Lý do |
|--------|-------|
| **Nhiều loại tile/booster** (bomb, wildcard, undo…) | Chưa cần để chứng minh core loop; thêm booster khi loop nền đã vui. YAGNI. |
| **Meta-game** (map level, sao, tiền tệ, shop, daily) | Nằm ngoài phạm vi bài test; tốn thời gian mà không kiểm chứng được gameplay chính. |
| **Save/Progression bền vững** | Chỉ lưu volume qua `PlayerPrefs`. Single-scene, retry reset tại chỗ — không cần hệ thống save phức tạp. |
| **Multi-scene / SceneManager** | Cố ý chạy 1 scene duy nhất; retry reset state trong scene → nhanh, ít lỗi vòng đời. |
| **Tự sản xuất art 3D/UI** | Dùng asset pack có license; dồn công sức cho gameplay & feel thay vì modeling/vẽ UI. |
| **Level editor tool** | JSON viết tay đủ dùng ở quy mô hiện tại; làm editor là premature. |
| **Tutorial / onboarding scripted** | Luật đơn giản (1-chạm), để sau khi content ổn định. |

## 6. Chỗ dồn "Polish budget"

Ngân sách polish có hạn nên tập trung vào những điểm người chơi **cảm nhận trực tiếp mỗi giây**:

1. **Chuyển động card & belt (ưu tiên #1)** — card bay từ tray vào "miệng ống" rồi rơi xuống belt bằng DOTween (nhảy cung + rơi có gia tốc), bám path chữ U và **quay đầu mượt khi vào cua**. Đây là thứ mắt nhìn nhiều nhất → đáng đầu tư nhất.
2. **Game feel: audio + haptic đồng bộ** — mỗi hành động (click, distribute, match, collect) có SFX + rung; có throttle chống chồng tiếng khi nhiều card bắn cùng frame. Rẻ nhưng nâng "juice" rất mạnh.
3. **Rõ ràng trạng thái** — nhãn `current/max` trên belt, tray khoá/mở trực quan, để người chơi luôn hiểu vì sao thắng/thua.
4. **Chuyển cảnh mượt** — màn Transition + Splash + mây nền để nhịp game liền mạch, không "khựng".

**Cố ý KHÔNG polish** (ở giai đoạn này): đồ hoạ cao cấp, VFX nặng, hiệu ứng hạt phức tạp — sẽ thêm ở M7 sau khi content & balancing chốt, để không polish nhầm thứ có thể còn thay đổi.

## 7. Rủi ro & giả định

- **Rủi ro cân bằng độ khó**: điều kiện Lose (belt kẹt trọn 1 vòng + hết nước tạo slot) phụ thuộc mạnh vào data level → cần nhiều vòng playtest ở M6.
- **Giả định thiết bị**: nhắm mobile tầm trung; poly-count thấp + pooling để giữ 60 FPS.
- **Phụ thuộc license asset**: mọi asset pack dùng đều phải hợp lệ thương mại trước khi ship (xem [ART_ASSETS.md](./ART_ASSETS.md)).