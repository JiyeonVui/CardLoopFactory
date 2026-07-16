# CardLoopFactory

Game puzzle casual 3D theo phong cách "toy factory / dây chuyền đồ chơi".

## 1. Mô tả game

**Card Loop Factory** là game giải đố casual 3D (màn hình dọc). Người chơi **bấm vào tray** để đẩy các card theo màu lên một **băng chuyền hình chữ U**; card chạy vòng quanh belt và tự **khớp vào các match-slot cùng màu**. Dọn sạch mọi tray thì **thắng**; nếu băng chuyền kẹt đầy mà không còn nước đi tạo tiến triển thì **thua**.

Điểm kỹ thuật chính:

- **Kiến trúc MVC** rõ ràng — Controller & Model viết bằng **C# thuần, không phụ thuộc Unity** để dễ auto-test.
- **Data-driven**: mỗi level là một file **JSON**, không hard-code trong code.
- Dùng **Service Locator** cho Dependency Injection thay cho Singleton.
- Hướng nghệ thuật low-poly, màu tươi, có chỉ báo phụ hỗ trợ người mù màu.

## 2. Art / Asset

Danh sách chi tiết toàn bộ art cần dùng (models 3D, sprite UI, font, SFX, nhạc nền, VFX) — kèm **kích thước, số lượng, format, style reference, nguồn lấy và link tải** của từng asset.

📄 Xem: [`docs/ART_ASSETS.md`](docs/ART_ASSETS.md)

## 3. Process Plan

Kế hoạch triển khai dự án: cách scope, các **milestone**, quy trình 6 bước (break down cơ chế → thiết kế kiến trúc MVC → code → làm level JSON → polish), những gì đã cắt và lý do, cùng chỗ dồn "polish budget".

📄 Xem: [`docs/PROCESS_PLAN.md`](docs/PROCESS_PLAN.md)

## 4. Cách chạy dự án

**Yêu cầu:** [Unity **6000.0.70f1**](https://unity.com/releases/editor/archive) (Unity 6) — nên cài đúng version qua Unity Hub để tránh lỗi upgrade project.

**Các bước:**

1. Clone repo:
   ```bash
   git clone git@github-jiyeon:JiyeonVui/CardLoopFactory.git
   ```
2. Mở **Unity Hub** → **Add** → chọn thư mục `CardLoopFactory` vừa clone.
3. Mở project bằng đúng Unity **6000.0.70f1** (Hub sẽ tự gợi ý cài nếu thiếu).
4. Trong Editor, mở scene chính: **`Assets/Scenes/GameScene.unity`**.
5. Nhấn **Play** để chơi.

> Level được nạp từ file **JSON** (data-driven). Chơi lại (retry) sẽ **reset ngay trong scene**, không load lại scene.

