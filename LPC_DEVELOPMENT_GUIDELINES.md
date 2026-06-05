# Cẩm Nang Phát Triển Dự Án LPC Unity & Hướng Dẫn Sử Dụng CodeGraph

Tài liệu này lưu trữ toàn bộ các giải pháp tinh hoa, quy chuẩn thiết kế và hướng dẫn sử dụng công cụ phân tích **CodeGraph** dành riêng cho dự án game LPC Unity. 

> [!NOTE]
> Bất kỳ trợ lý AI nào (hoặc nhà phát triển) khi tiếp cận dự án này **bắt buộc phải đọc kỹ và tuân thủ 100% các nguyên tắc** dưới đây để đảm bảo code sạch, đồng bộ và không phá vỡ các tính năng sẵn có.

---

## 🧭 PHẦN 1: HƯỚNG DẪN SỬ DỤNG CODEGRAPH ĐỈNH CAO

Dự án này rất lớn, bao gồm **hơn 10,000 file C#** (gồm cả mã nguồn game và các thư viện Unity). Việc tìm kiếm bằng văn bản thông thường (Grep/Ctrl+F) rất chậm và dễ sai sót. **Bắt buộc phải ưu tiên sử dụng CodeGraph MCP** cho mọi tác vụ nghiên cứu cấu trúc:

### 1. Tìm kiếm định nghĩa nhanh (`codegraph_search`)
* **Khi nào dùng**: Khi muốn tìm chính xác nơi khai báo của một class, hàm, biến hay enum.
* **Cách dùng**: Truyền tham số `{ query: "Tên_Symbol" }` (ví dụ: `query: "LPCPlayerController2"`).

### 2. Tìm kiếm liên kết gọi chéo (`codegraph_callers` / `codegraph_callees`)
* **Khi nào dùng**: Khi muốn refactor code hoặc tìm xem một sự kiện được kích hoạt từ đâu.
* **Cách dùng**:
  - `codegraph_callers`: Ai đang gọi hàm này?
  - `codegraph_callees`: Hàm này đang gọi đến những hàm nào khác?

### 3. Lấy ngữ cảnh tập trung (`codegraph_context`)
* **Khi nào dùng**: Trước khi sửa đổi một tính năng lớn (ví dụ: Dungeon Generator, Player Combat).
* **Cách dùng**: Truyền tham số `{ task: "Mô tả nhiệm vụ cần làm..." }`. Hệ thống sẽ tự động tổng hợp tất cả các file và hàm liên quan chặt chẽ nhất để cung cấp cho bạn cái nhìn sâu sắc.

---

## 🎨 PHẦN 2: QUY CHUẨN Y-SORTING 2D TOP-DOWN (CHIỀU SÂU HÌNH ẢNH)

Để nhân vật di chuyển trước/sau các vật thể khác (như thùng gỗ, rương, cây cối, quái vật) hiển thị chiều sâu 3D tự nhiên, dự án áp dụng quy chuẩn sau:

### 1. Gom chung về Sorting Layer `Entities`
* **Quy tắc**: Player, Enemy, và các vật thể tĩnh đứng trên mặt đất (Thùng gỗ, Rương báu, Bàn ghế, Cột đá) **phải nằm chung Sorting Layer `Entities`** (Layer số 5).
* **Tuyệt đối không** dùng layer `Player` riêng biệt ở dưới cùng danh sách vì nó sẽ luôn vẽ đè lên mọi thứ.

### 2. Cấu hình Pivot & Sprite Sort Point ở Chân
* **Pivot**: Điểm Pivot của toàn bộ Sprite của Player, Enemy và các vật thể tĩnh phải đặt ở **đáy chân (Bottom)**.
* **Sprite Sort Point** (trên Sprite Renderer): Phải đổi từ *Center* thành **`Pivot`** để Unity so sánh tọa độ Y ở chân thay vì ở tâm hình học.

### 3. Giải pháp cho Nhân vật Modular (Nhiều mảnh ghép)
Vì Player ghép từ 17 mảnh con, ta sử dụng component **`Sorting Group`** ở GameObject cha:
* Component `Sorting Group` ở cha sẽ chọn layer **`Entities`**.
* Các Sprite con sẽ tự động được sắp xếp nội bộ bằng thuộc tính **`Order in Layer`** của chúng:
  - `Shadow`: `-1` (vẽ sát đất)
  - `Body`: `5`, `Legs`: `9`, `Feet`: `10`, `Torso`: `11`, `Armor`: `12`, `Hair/Helmet`: `16`, `Weapon/Shield` cầm tay: `17` (ngoài cùng).
* **⚡ Nút bấm tự động**: Nhấp chọn Player, kéo xuống component `LPC Player Controller 2`, click vào nút màu xanh lá cây **`⚡ TỰ ĐỘNG CẤU HÌNH SORTING PLAYER`** để tự động thiết lập toàn bộ cấu trúc này trong 1 giây!

---

## 🏃 PHẦN 3: QUY CHUẨN COLLIDER PHẦN CHÂN (FOOT COLLIDER)

* **Nguyên tắc**: Không bao giờ để Collider (BoxCollider2D/CapsuleCollider2D) bao phủ toàn bộ cơ thể của Player hoặc các vật thể tĩnh.
* **Cấu hình chuẩn**: Co nhỏ Collider lại sao cho nó **chỉ dẹt ở dưới chân (chiếm 15-20% chiều cao dưới cùng)**.
* **Hiệu quả**: Thân trên của Player có thể đi chui ra sau cái thùng gỗ hoặc chân tường một cách tự nhiên mà không bị cản lại từ xa.

---

## ⚡ PHẦN 4: CƠ CHẾ THỂ LỰC CHIẾN ĐẤU TINH CHỈNH (STAMINA SYSTEM)

Được cấu hình trong [LPCPlayerController2.cs](file:///e:/project%20game/tool%20t%E1%BB%B1%20%C4%91%E1%BB%99ng%20animation/Assets/Scripts/LPCPlayerController2.cs):

1. **Tấn Công Hồi Ngay**: Thể lực chỉ ngưng hồi trong lúc hoạt ảnh chém đang diễn ra (`isAttacking == true`). Chém xong (`isAttacking == false`), thể lực hồi phục ngay lập tức với tốc độ 100% mặc định (`10f` stamina/giây), bất kể đang ở trong combat.
2. **Debuff 50% Khi Chém Hoặc Bị Đánh**:
   - Khi player **chém quái** (tấn công) HOẶC **bị quái đánh trúng** (nhận sát thương), một debuff được kích hoạt: **Giảm 50% tốc độ hồi thể lực** (còn `5f` stamina/giây) kéo dài trong vòng **`5` giây** (`staminaDebuffDuration = 5.0f`).
   - Mốc thời gian được lưu trong biến `lastCombatTime = Time.time`.
   - Hết 5 giây không thực hiện chém tiếp và không bị đánh trúng thêm, tốc độ hồi tự động khôi phục lại 100% bình thường (`10f`/giây).
3. **Cạn Kiệt (Exhausted)**: Khi stamina cạn về 0, tốc chạy và tốc hồi stamina giảm 50%. Có thể cộng dồn nhân đôi với debuff chiến đấu (giảm còn 25% tức `2.5f` stamina/giây nếu dính cả hai).

---

## 🏰 PHẦN 5: BỘ SINH DUNGEON & PHÒNG START SPWN POINT

Được cấu hình trong [DungeonGenerator.cs](file:///e:/project%20game/tool%20t%E1%BB%B1%20%C4%91%E1%BB%99ng%20animation/Assets/DungeonSystem/Runtime/Generation/DungeonGenerator.cs):

1. **Tách biệt phòng Start (`RoomType.Start`)**: Ô xuất phát `(0,0)` luôn được gán loại phòng `Start` độc lập, sử dụng danh sách prefab `startRooms` cấu hình riêng biệt trong `DungeonConfig` để đảm bảo tính thẩm mỹ của điểm xuất phát.
2. **Hệ thống Điểm Spawn Người chơi (`LPCPlayerSpawnPoint`)**:
   - Trong prefab phòng Start, đặt một GameObject con chứa component `LPCPlayerSpawnPoint` và kéo Gizmos xanh ngọc trực quan đến vị trí muốn mọc Player.
   - Khi dungeon sinh ra, `DungeonGenerator.cs` tự động dịch chuyển Player (`TeleportPlayerToStartRoom()`) đến đúng điểm xanh ngọc này **trước khi** khởi tạo hệ thống culling để tránh việc màn hình bị đen xì do culling lệch tọa độ.

---

## 🛡️ PHẦN 6: CẨM NANG CẤU HÌNH LAYER CHI TIẾT (PLAYER, MAP, TƯỜNG, QUÁI & THỰC VẬT)

Dưới đây là bảng quy chuẩn cấu hình đồng bộ hiển thị (**Sorting Layer**) và va chạm vật lý (**Physics Layer**) để đảm bảo game vận hành trơn tru, không lỗi đồ họa và di chuyển chân thực nhất.

### 1. Nhân vật chính (Player)
* **Sorting Layer (Lớp hiển thị)**:
  - Chọn **`Entities`** (Layer số 5) cho **Sorting Group** ở GameObject cha của Player.
  - Các **SpriteRenderer con**: Sử dụng nút **`⚡ TỰ ĐỘNG CẤU HÌNH SORTING PLAYER`** để gán thứ tự Order chuẩn từ `-1` đến `25`.
  - ⚠️ *Quan trọng*: Hãy điền chữ **`Entities`** vào ô **`Sorting Layer Name`** trên script `LPCPlayerController2` trước khi bấm nút tự động cấu hình để tránh bị nhảy về `Default`.
* **Physics Layer (Va chạm vật lý)**:
  - Đặt là **`Player`** (ở góc trên bên phải Inspector).
* **Collider cấu hình**:
  - `CapsuleCollider2D` thu nhỏ dẹt nằm sát phần chân (chỉ cao khoảng 15-20% chiều cao nhân vật) để dễ luồn lách.

### 2. Bản đồ & Nền đất (Map & Floor)
* **Sorting Layer**:
  - **Tilemap Floor** (Đất nền, sàn nhà): Chọn **`Map_Floor`** (Layer số 1), `Order in Layer = 0`.
  - **Tilemap Ground Detail** (Thảm trang trí, hoa văn sàn): Chọn **`Map_FloorDetail`** (Layer số 2), `Order in Layer = 0` (hoặc `1` để đè lên nền đất).
* **Physics Layer**:
  - Đặt là **`Default`** (không cản chân nhân vật).
* **Collider cấu hình**: Không gắn bất kỳ Collider nào lên Tilemap này.

### 3. Tường trong bản đồ (Walls in Map)
* **Sorting Layer**:
  - **Thân tường chắn đường**: Chọn **`Map_Walls`** (Layer số 7), `Order in Layer = 0`.
  - **Đỉnh tường / Mái nhà che đầu**: Chọn **`Map_Foreground`** (Layer số 8), `Order in Layer = 0`. Giúp nhân vật đi phía sau tường che khuất được đầu.
* **Physics Layer**:
  - Đặt là **`Wall`** (hoặc `Obstacle`).
* **Collider cấu hình**:
  - Phải có component **`Tilemap Collider 2D`** kèm **`Composite Collider 2D`** (để gộp collider tường liền mạch thành một khối lớn, tránh lag và lỗi nhân vật bị kẹt vào khe tường).

### 4. Quái vật & Kẻ địch (Enemies & Monsters)
* **Sorting Layer**:
  - Chọn **`Entities`** (Layer số 5) để có thể sắp xếp chiều sâu động (Y-Sorting) hoàn hảo cùng Player.
  - **Sprite Sort Point** trên Sprite Renderer: Bắt buộc chọn **`Pivot`** (thay vì *Center*).
  - **Pivot của Sprite**: Đặt sát đáy chân quái vật (Bottom).
* **Physics Layer**:
  - Đặt là **`Enemy`**.
* **Collider cấu hình**:
  - Collider dẹt nhỏ ở chân để tránh bị kẹt cứng vật lý vào tường.

### 5. Thực vật & Chướng ngại vật (Plants, Trees & Obstacles)
* **Bụi cỏ nhỏ dẫm chân lên được**:
  - **Sorting Layer**: Chọn **`Map_FloorDetail`** (Layer số 2).
  - **Physics Layer**: `Default` (Không có Collider).
* **Gốc cây, đá tảng cản đường (Obstacles)**:
  - **Sorting Layer**: Chọn **`Entities`** (Layer số 5) để Y-Sorting hoạt động (khi đi lên sau gốc cây thì cây che người, khi đi xuống trước gốc cây thì người che cây).
  - **Sprite Sort Point**: Chọn **`Pivot`**.
  - **Physics Layer**: Đặt là **`Wall`** (hoặc `Obstacle`).
  - **Collider**: Thiết lập Box/Circle Collider siêu nhỏ dẹt nằm sát gốc cây/đế đá tảng.
* **Tán lá cây to che đầu (Tree Canopy)**:
  - **Sorting Layer**: Chọn **`Map_Foreground`** (Layer số 8). Giúp tán lá luôn phủ lên đầu nhân vật khi đi qua gốc cây.
  - **Physics Layer**: `Default` (Không có Collider).
