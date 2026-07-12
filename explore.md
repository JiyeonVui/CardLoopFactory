# CardLoopFactory — Ghi chú khám phá source

> Tài liệu này tổng hợp những gì đã đọc được từ source (chỉ suy luận từ code, chưa xác nhận luật chơi với tác giả). Ngày: 2026-07-12.

## 1. Tổng quan kiến trúc

Unity project, theo mô hình **MVC + Service Locator/DI + Object Pooling**.

Toàn bộ code game nằm ở: `Assets/Game/Script/`

```
Assets/Game/Script/
├── Controller/
│   ├── GameContext.cs        # Bootstrap: init service, load level, spawn tray
│   ├── GameController.cs      # Điều phối game loop, distribution card -> belt
│   ├── BeltController.cs      # Băng chuyền vòng lặp
│   ├── TrayController.cs      # Logic vòng đời tray (đa phần còn stub)
│   └── LevelManager.cs        # Load level từ JSON Resources
├── Model/
│   ├── TrayModel.cs           # Data tray + CardGroup + TrayState enum
│   └── CardModel.cs           # CardModel, ConveyorCard, CardColor enum
├── View/
│   ├── TrayView.cs            # Hiển thị tray, nhận tap  <-- đang làm MoveToSlot
│   └── CardView.cs            # Hiển thị card, bám theo ConveyorCard trên belt
├── Extension/                 # DI, Pooling, Factory
│   ├── ServiceLocator.cs / ServiceAttribute.cs / InjectAttribute.cs
│   ├── GameEntityFactory/     # Spawn prefab từ pool + gọi ReadyToStartGame
│   └── PoolingService.cs / PoolInfo.cs / PoolingHelper.cs
└── UIUX/SplashScene.cs
```

Prefab liên quan: `Assets/Game/Prefab/TrayView.prefab`. Level data: `Resources/level_test.json`.

## 2. Các thành phần cốt lõi

### Model

- **TrayModel** (`Model/TrayModel.cs`)
  - `Id`, `Composition: List<CardGroup>` (mỗi group = màu + số lượng), `CardColor`.
  - `IsLocked`, `BlockedByTrayIds: List<int>` — cơ chế khoá tray theo tray khác.
  - `TrayState { Filled, Empty, IsUsed }`.
  - `Position`, `Rotation` (euler) — pose khi spawn.
  - `TotalCardCount` — không lưu trong JSON, seed bằng `RecalculateTotalCardCount()` (tổng Count các group); đếm ngược khi phát card.
- **CardModel / ConveyorCard / CardColor** (`Model/CardModel.cs`)
  - `CardColor` enum: `Blue, Green, Orange, Red, Yellow` (thứ tự QUAN TRỌNG — map với index material).
  - `ConveyorCard`: một card đang chạy trên belt (`card` + `position`).

### View

- **TrayView** (`View/TrayView.cs`) — MonoBehaviour, `IPointerClickHandler`, `IGameContextSubscriber`.
  - `_cardSlotPositions`: 7 vị trí local để đặt card trong tray (từ -0.6 đến 0.6 theo X).
  - `Init()`: set material theo màu, spawn card từ pool đặt vào slot, áp lock state.
  - Lock state: tray khoá hiện `_wingLeft/_wingRight`, ẩn `_cardHolder`; mở thì ngược lại.
  - `Update()`: nếu chưa mở và model `!IsLocked` -> `OnOpen()` (một chiều, set `_isOpen`).
  - `OnPointerClick()`: còn card -> `DistributionCard()`; hết card -> **`MoveToSlot()`**.
  - `DistributionCard()`: gọi `GameController.DistributionCard(_cardViews, _trayModel)` rồi clear list.
- **CardView** (`View/CardView.cs`)
  - `_colorMaterials[]` map theo CardColor enum.
  - Sau khi lên belt, `AddCoyorCard(conveyorCard)` -> mỗi `Update()` card bám `_conveyorCard.position`.
  - `MatchColor()` set `_isMatch = true` (dừng bám) — hiện chưa nơi nào gọi.

### Controller

- **GameContext** (bootstrap): `Start()` async -> `ServiceInitializer.InitializeAsync()` -> lấy `GameController`, tạo `GameEntityFactory` từ `IPoolingService`, dựng `GameContextData`, load level, `StopGame(false)`.
  - `SpawnTray()` dùng factory spawn `TrayView` tại pose của model rồi `Init(model)`.
  - Factory tự chạy `ReadyToStartGame(contextData)` trên mọi `IGameContextSubscriber` khi spawn (nên view sẵn context trước `Init`).
- **GameController** (MonoBehaviour, `IGameController`):
  - `[Inject] IBeltController`, `[Inject] TrayController`.
  - `Update()`: nếu game chạy -> `_beltController.UpdateCardPosition(dt)`.
  - `DistributionCard(cards, tray)`: mỗi card gọi `FlyCardToBelt()` với stagger 0.1s.
  - `FlyCardToBelt()`: reparent card sang `beltHolderCard` (giữ world pose), DOTween `DOJump` + `DOLocalRotate` tới `startBelt`. OnComplete: `BeltController.AddNewCard(color)` -> gắn ConveyorCard vào card, `TrayController.OnCardDistributed(tray)`.
  - `GetBeltHeadingRotation()`: hiện trả `Vector3.zero` (belt thẳng trái->phải); ghi chú sẽ mở rộng cho belt cong.
- **BeltController** (`IBeltController`): băng chuyền **vòng lặp**.
  - `cardInQueue` (chờ) + `cardOnBelt` (đang chạy). Start `(-2,0,0)` -> End `(2,0,0)`, velocity 2.0.
  - `UpdateCardPosition()`: dịch card theo +X; vượt end thì wrap về start. Nếu slot đầu trống (`StartSlotTolerance 0.3`) thì dequeue card mới ra belt.
  - `RemoveCard()` còn rỗng.
- **TrayController** (`ITrayController`): phần lớn stub.
  - `RecomputeLockState()`, `DistributionCard()` — rỗng.
  - `RemoveTrayModel()` -> xóa khỏi `_levelManager.TrayModels` (lưu ý: `_levelManager` chưa được gán ở đâu -> nguy cơ NRE).
  - `OnCardDistributed()`: giảm `TotalCardCount`, về 0 thì `State = Empty`.
- **LevelManager**: `LoadFromResources(path)` đọc JSON -> `JsonUtility.FromJson<LevelManager>` -> seed `TotalCardCount` cho từng tray.

### Extension (hạ tầng)

- **ServiceLocator** + `[Service]`/`[Inject]`: đăng ký/resolve interface, `ResolveInjection(this)` inject field `[Inject]`.
- **GameEntityFactory**: spawn prefab từ pool, chạy initializer (`ReadyToStartGame`) cho subscriber.
- **PoolingService / PoolInfo / PoolingHelper**: object pooling cho card & tray.

## 3. Luồng chơi hiện tại (đã suy ra)

1. `GameContext.Start()` -> init service, load `level_test`, spawn 1 `TrayView` / `TrayModel`.
2. Tray khoá hiện wings; khi `!IsLocked` -> `Update()` mở tray hiện card.
3. Tap tray còn card -> card **bay lên belt** (DOTween), vào queue của `BeltController`, `TotalCardCount--`.
4. Belt chạy vòng lặp, feed card từ queue ra khi slot đầu trống, card wrap khi tới cuối.
5. Tap tray **hết card** -> `MoveToSlot()` (**đang là stub**, phần đang phát triển).

Tên game gợi ý: **"Loop"** = belt vòng lặp; **"Factory"** = dây chuyền card.

## 4. Điểm đang dở / cần làm rõ

- **`MoveToSlot()`** (`TrayView.cs:171`) là stub `Debug.LogError`. **Chưa xác định "slot" là gì**:
  - Chưa có khái niệm SlotController/SlotView/holder cho tray rỗng trong source.
  - `_cardSlotPositions` là slot cho *card trong tray*, không phải đích của tray.
  - `TrayState.IsUsed` có thể liên quan tray đã "move to slot".
- `OnPointerClick()` vẫn còn `Debug.LogError` như log tạm.
- Nhiều hàm controller còn rỗng: `TrayController.RecomputeLockState/DistributionCard`, `BeltController.RemoveCard`, `CardView.MatchColor` chưa được gọi.
- `TrayController._levelManager` chưa được khởi tạo -> `RemoveTrayModel` có thể NRE.
- Cơ chế **thắng/thua**, **match màu**, đích cuối của card trên belt: chưa có trong code.

## 5. Câu hỏi mở cho tác giả

1. `MoveToSlot()` mong muốn làm gì khi tray hết card? (tray rỗng bay vào slot chứa? slot trên belt? mở khóa tray khác?)
2. "Slot" đã tồn tại trong scene/prefab chưa, hay cần dựng mới?
3. Card trên belt cuối cùng match/tiêu thụ ở đâu? Điều kiện thắng level là gì?