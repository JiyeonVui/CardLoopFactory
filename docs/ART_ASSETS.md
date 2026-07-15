# Card Loop Factory — Art / Asset Requirements (Danh sách Art)

> Danh sách art **tự định nghĩa**: từng sprite, model, sound, font — kèm **kích thước, số lượng, format, style reference, và nguồn lấy khi production**. Đây là một phần của bài test.

## 0. Style reference tổng thể

- **Hướng nghệ thuật**: 3D casual/hyper-casual, low-poly, khối hình bo tròn, màu tươi bão hoà cao, ánh sáng phẳng dịu — kiểu "toy factory / dây chuyền đồ chơi".
- **Tham chiếu**: Kenney *Factory Kit* (chuẩn low-poly), các game sorting casual mobile (Nuts & Bolts, Sort Puzzle), palette pastel-pop.
- **Bảng màu card (5 màu — theo `enum CardColor`)**: `Blue`, `Green`, `Orange`, `Red`, `Yellow`. Cần giữ đủ tương phản để phân biệt trên belt đang chuyển động; kèm chỉ báo phụ (biểu tượng/hoa văn) để hỗ trợ người mù màu.
- **Định hướng màn hình**: Portrait (dọc), thiết kế UI theo safe-area mobile.

---

## 1. Models 3D (props)

Format chuẩn: **FBX**, low-poly, 1 material/atlas dùng chung, pivot đặt hợp lý cho animation/pooling. Ngân sách poly gợi ý cho mobile.

| Asset | Số lượng | Poly budget (gợi ý) | Format | Style / ghi chú | Nguồn khi production |
|-------|----------|---------------------|--------|-----------------|----------------------|
| Card (thẻ trên belt) | 1 model × 5 biến thể màu | < 300 tris | FBX + prefab | Thẻ bo góc, mặt nổi màu; 5 material theo `CardColor` | Tự model (đơn giản) — đang có `Card.prefab` |
| Tray (khay chứa card) | 1 base (đặt lại vị trí/màu qua data) | < 800 tris | FBX + prefab | Khay có gờ, nhận màu chủ đạo theo `CardColor` | Tự model — `TrayView.prefab` |
| Băng chuyền chữ U | 1 (mô-đun ghép từ segment + góc) | phụ thuộc path | FBX / mesh sinh động | Segment thẳng + góc bo; khớp `BeltPathPoints` trong JSON | Kenney Factory Kit (`catwalk-*`, `conveyor`) |
| Ống phát card (pipe/miệng ống ở đầu belt) | 1 | < 500 tris | FBX | Điểm card "chui vào" belt (`PipeHeight`) | Kenney Factory Kit |
| Box / Crate (đích thu gom) | 1–2 | < 600 tris | FBX + prefab | Thùng nhận card đã match | `BoxView.prefab` / Kenney (`box-*`) |
| Gift Box (biến thể phần thưởng) | 1 | < 600 tris | FBX + prefab | Hộp quà cho slot hoàn thành | `GiftBoxView.prefab` |
| Truck (giao hàng, trang trí/kết màn) | 1 | < 1500 tris | FBX + prefab | Xe chở thùng đi khi thắng | `truckView.prefab` / Kenney Car Kit |
| Mũi tên hướng belt (arrow) | 1 | < 200 tris | FBX | Chỉ hướng chạy của belt, có animate (`BeltArrowAnimator`) | Kenney Factory Kit (`arrow-*`) |
| Props trang trí nhà máy | 5–10 | thấp | FBX | Cog, button-floor, catwalk, thùng… tạo bối cảnh | Kenney Factory Kit |
| Mây nền (Clouds) | 3–5 | thấp | FBX/quad | Trang trí nền, tạo chiều sâu | Asset "Clouds" (đang có) |

**Textures cho model**: 1 texture atlas màu low-poly (Kenney palette, 1024×1024 PNG) dùng chung; 5 material/màu cho card. Card có thể chỉ cần **material màu phẳng**, không cần texture riêng.

---

## 2. Sprites / UI 2D

Format: **PNG** (nền trong suốt), thiết kế theo **@1x / @2x** cho các mật độ màn hình; UI kéo giãn dùng **9-slice** ở đâu có thể. Kích thước ghi theo pixel ở mật độ tham chiếu (nhắm màn ~1080px chiều ngang).

| Asset | Số lượng | Kích thước (px) | Format | Style / ghi chú | Nguồn |
|-------|----------|-----------------|--------|-----------------|-------|
| Nút chính (Play/Retry/Next) | 3 trạng thái (normal/pressed/disabled) | ~400×140 | PNG 9-slice | Bo tròn, đổ bóng nhẹ, màu pop | 300Mind 2D Game UI Kit |
| Nút phụ (Sound/Music/Settings/Back) | ~4 icon × 2 trạng thái | 96×96 | PNG | Icon tròn toggle | 300Mind UI Kit |
| Panel / Popup nền | 2–3 | ~900×1200 (co giãn) | PNG 9-slice | Khung kết quả Win/Lose | 300Mind UI Kit |
| Nhãn tiêu đề Win / Lose | 2 | ~700×300 | PNG | Chữ nghệ thuật hoặc dựng bằng font | 300Mind UI Kit / tự dựng |
| Sao đánh giá (star) | 1 (empty/filled) | 128×128 | PNG | Thanh sao kết quả | 300Mind UI Kit |
| Thanh tiến độ / khung đếm belt | 1 khung + fill | ~500×80 | PNG 9-slice | Hiển thị `current/max` | 300Mind UI Kit |
| Icon màu card (hỗ trợ mù màu) | 5 | 64×64 | PNG | Biểu tượng phụ theo màu | Tự vẽ / icon pack |
| Sprite belt tham chiếu (2D) | 1 | có sẵn | PNG | `conveyor_belt_u_shape.png` (đang có, tham chiếu layout) | Trong project |
| Logo game / Splash | 1 | ~1024×1024 | PNG | Nhận diện game trên splash | Tự thiết kế |
| Background scene | 1–2 | 1080×1920 | PNG | Nền gradient/nhà máy phía sau 3D | 300Mind UI Kit / tự |

---

## 3. Fonts

Format: **TTF/OTF** → build sang **TextMeshPro SDF `.asset`**. Cần khai báo rõ font cho từng cấp bậc chữ.

| Vai trò | Font đề xuất | Size (pt, tham chiếu) | Format | Ghi chú | Nguồn |
|---------|--------------|------------------------|--------|---------|-------|
| Tiêu đề / Title lớn (Win, Lose, Logo) | **GROBOLD** hoặc **Ariston Comic** | 72–120 | TTF → SDF | Chữ dày, vui, đậm chất casual | 300Mind UI Kit (đang có) |
| Nút & heading | **Oswald SemiBold / Bold** | 32–48 | TTF → SDF | Rõ, gọn, dễ đọc trên nút | 300Mind UI Kit (đang có) |
| Body / số đếm (belt `current/max`, HUD) | **Oswald Medium / Regular** | 24–32 | TTF → SDF | Số & text phụ | 300Mind UI Kit (đang có) |
| Fallback hệ thống | **LiberationSans SDF** | — | SDF | Fallback ký tự thiếu | TextMesh Pro (mặc định) |

> Lưu ý production: kiểm tra **license nhúng** của mỗi font trước khi ship; nếu cần đa ngôn ngữ (tiếng Việt có dấu, CJK…) phải build atlas SDF có đủ glyph hoặc dùng Dynamic SDF.

---

## 4. Sound Effects (SFX)

Format: **WAV** (mono, 44.1 kHz) cho SFX ngắn — import Unity nén sang Vorbis/ADPCM tuỳ độ dài. Hiện code (`AudioSo` + `ManagerAudio`) đã khai báo các khe sau:

| SFX | Khi nào phát | Độ dài | Format | Style | Nguồn |
|-----|--------------|--------|--------|-------|-------|
| **Click** | Bấm tray / nút | < 0.3s | WAV | "pop/tap" mềm | Casual Game Sounds U6 |
| **Distribute** | Card bay từ tray lên belt | < 0.4s | WAV | "whoosh/swipe" nhẹ (có throttle chống chồng) | Casual Game Sounds U6 |
| **Match** | Card khớp vào slot màu | < 0.4s | WAV | "ding" tích cực | Casual Game Sounds U6 |
| **Collection / Complete** | Slot đầy / thu gom xong | < 0.6s | WAV | "chime" thưởng | Casual Game Sounds U6 |
| **Win** | Thắng màn | 1–2s | WAV | fanfare vui | Casual Game Sounds U6 |
| **Lose / Fail** | Thua (belt kẹt) | 1–2s | WAV | "buzz" nhẹ, không gắt | Casual Game Sounds U6 |
| **Popup / Earn** (đã có API) | Mở popup / nhận thưởng | < 0.5s | WAV | "pop" | Casual Game Sounds U6 |

Pack **Casual Game Sounds U6** đã có sẵn ~50 file (`DM-CGS-01..50.wav`) → chọn lọc gán vào `AudioSo.asset`.

---

## 5. Music (Nhạc nền)

| Track | Dùng ở đâu | Độ dài | Format | Style | Nguồn |
|-------|-----------|--------|--------|-------|-------|
| BGM chính (loop) | Toàn bộ gameplay | 1–2 phút, loop liền | MP3/OGG | Upbeat, nhẹ nhàng, không gây mệt khi nghe lâu | Đang có: `pixel-drift-pecan-pie-...mp3` (BGMus) |
| (Tuỳ chọn) BGM menu | Start/Result | 30–60s loop | MP3/OGG | Bình thản hơn gameplay | Bổ sung sau nếu cần |

> Import: bật **Load in Background** + **Streaming** cho nhạc dài để giảm RAM; SFX ngắn để **Decompress on Load**.

---

## 6. VFX (giai đoạn polish — M7)

| VFX | Khi nào | Nguồn |
|-----|---------|-------|
| Khói/hơi máy ở ống phát | Card chui vào belt | msVFX Free Smoke Effects Pack (đang có) |
| Particle "match" (lấp lánh) | Card khớp slot | Tự làm bằng Unity Particle System |
| Confetti khi Win | Kết màn thắng | Particle / asset pack |

---

## 7. Tổng kết nguồn asset (sourcing) & license

| Pack | Dùng cho | Trạng thái license cần xác nhận |
|------|----------|----------------------------------|
| Kenney Factory Kit / Car Kit | Models 3D nhà máy, belt, box, truck, arrow | Kenney = **CC0** ✅ (an toàn thương mại) |
| 300Mind 2D Game UI Kit | Sprite UI + fonts (Oswald, GROBOLD, Ariston) | ⚠️ Kiểm tra license kit + license nhúng của từng font |
| Casual Game Sounds U6 | Toàn bộ SFX | ⚠️ Xác nhận điều khoản dùng thương mại |
| BGM (pixel-drift…) | Nhạc nền | ⚠️ Xác nhận nguồn & quyền dùng trước khi ship |
| msVFX Free Smoke | VFX khói | ⚠️ Kiểm tra điều khoản "Free" |
| TextMesh Pro / LiberationSans | Font fallback | ✅ (Unity/OFL) |

**Việc cần làm trước khi ship**: rà license từng pack ⚠️, thay thế bất kỳ asset nào không có quyền thương mại bằng asset CC0/đã mua license.
