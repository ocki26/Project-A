# ⚡ HƯỚNG DẪN CẤU HÌNH & TÔ VẼ SPAWN ZONE CHUYÊN NGHIỆP ⚡

Chào bạn! Hệ thống Spawn Zone của bạn đã được nâng cấp lên phiên bản **Pro & Hybrid (Hệ thống lai)**. Dưới đây là tài liệu chi tiết hướng dẫn cách sử dụng công cụ vẽ vùng spawn, thiết lập các tùy chọn, và các mẹo tối ưu hóa hiệu năng để game chạy mượt mà ngay cả trên **máy cấu hình yếu**.

---

## 📖 1. Tổng Quan Hệ Thống Spawn Zone Lai

Hệ thống hỗ trợ 2 cấp độ thiết lập chính:
1. **Cấu hình Toàn Cục (Global Presets)**: Định nghĩa thông qua file ScriptableObject `RoomSpawnPreset`. Một preset có thể được dùng chung cho nhiều phòng cùng loại (ví dụ: tất cả phòng bình thường đều dùng chung Preset Rừng).
2. **Cấu hình Cục Bộ (Local Override)**: Gắn trực tiếp component `RoomSpawnZoneAuthoring` vào từng Prefab phòng cụ thể để vẽ tay chi tiết các khu vực mọc vật thể độc quyền cho riêng phòng đó (ví dụ: chỉ mọc cây quanh hồ nước của phòng Boss).

> [!IMPORTANT]
> **Cơ chế Fallback thông minh:**
> Khi sinh bản đồ, `SpawnZoneManager` sẽ tự động kiểm tra xem prefab phòng sinh ra có chứa dữ liệu vẽ tay cục bộ (`RoomSpawnZoneAuthoring` đang hoạt động) hay không:
> * **Nếu có:** Hệ thống sẽ dùng chính xác các vùng bạn đã vẽ trên prefab đó.
> * **Nếu không:** Hệ thống tự động fallback về dùng Preset ScriptableObject toàn cục tương ứng với loại phòng để sinh vật thể.

---

## 🛠️ 2. Hướng Dẫn Từng Bước: Tạo 1 Chỗ Spawn Cây Tự Nhiên

Hãy làm theo các bước sau để thiết lập một khu vực mọc cây cối quanh góc của một căn phòng cụ thể:

### Bước 1: Mở Prefab Phòng Trong Chế Độ Edit (Prefab Stage)
1. Trong cửa sổ **Project**, nháy kép (Double Click) vào file Prefab phòng của bạn (ví dụ: `Room_Forest_01`) để mở giao diện chỉnh sửa Prefab của Unity.
2. Đảm bảo GameObject gốc của Prefab này đã được gắn component **`RoomPrefabData`**. Bạn có thể bấm nút **`🔍 Auto Fit`** màu xanh bên cạnh trường `Kích Thước (Size)` để hệ thống tự động đo và cấu hình `Room Size` cùng `Pivot Is Center` chính xác theo thực tế các ô gạch đã vẽ trong Tilemap!

### Bước 2: Bật Bộ Vẽ Vùng Cục Bộ
1. Chọn GameObject gốc của Prefab phòng.
2. Trong bảng **Inspector**, tìm đến mục **🛰️ QUẢN LÝ VÙNG SPAWN ZONE (INTEGRATED)** ở cuối component `RoomPrefabData`.
3. Bấm nút **`⚡ Vẽ Vùng Cục Bộ (Local)`**. Unity sẽ tự động add component `RoomSpawnZoneAuthoring` vào GameObject này và nhúng trực tiếp giao diện vẽ vào `RoomPrefabData` để bạn không phải quản lý nhiều file.

### Bước 3: Thêm Vùng Mới & Thiết Lập Cây Cối
1. Bấm nút **`＋ Thêm Vùng Mới`** trong danh sách.
2. Click mở rộng vùng vừa tạo (ví dụ: `Vùng Mới 1`):
   * **Tên Vùng**: Đổi thành `Khu Vực Mọc Cây`.
   * **Hình Dạng Vùng**: Chọn `CustomBrush` (để tô màu tự do) hoặc `Circle`/`Rectangle` (nếu muốn dùng hình học).
   * **Màu Sắc Hiển Thị**: Chọn một màu xanh lá cây nhẹ để dễ quan sát.
   * **Mật Độ (density)**: Đặt khoảng `0.1` đến `0.2` (mật độ vật thể trên 1 ô lưới).
3. Tại danh sách **Vật thể được mọc trong vùng này (Allowed Objects)**:
   * Chỉnh số lượng loại vật thể là `1`.
   * Gán file dữ liệu sinh vật thể của bạn (ví dụ: `SpawnRule_OakTree` hoặc `SpawnRule_PineTree`) vào ô chọn.

### Bước 4: Thực Hiện Vẽ Vùng Trên Scene View
1. Bấm nút **`Chọn Vẽ`** ở thanh tiêu đề của vùng `Khu Vực Mọc Cây` (tiêu đề sẽ sáng xanh lá cây báo `Đang chọn vẽ`).
2. Bấm nút **`Vào Chế Độ Vẽ Cọ (Scene View)`**.
3. Di chuột sang cửa sổ **Scene View**:
   * Bạn sẽ thấy một lưới tọa độ mờ hiển thị các chỉ số `x,y` và một ô vuông cọ vẽ theo con trỏ chuột.
   * **Tô vẽ**: Nhấn giữ **Chuột Trái** và kéo để tô màu các ô lưới muốn cây mọc.
   * **Xóa nét vẽ**: Nhấn giữ **Ctrl + Chuột Trái** và kéo qua các ô đã tô để xóa bớt.
   * **Cỡ cọ vẽ**: Bạn có thể chuyển nhanh giữa các cỡ cọ **1x1, 2x2, 3x3** trên bảng HUD ở góc trên bên trái Scene View để tô các vùng rộng lớn nhanh hơn.
   * **Thao tác nhanh**: Bạn cũng có thể bấm **Tô Đầy (Fill)**, **Đảo (Invert)** hoặc **Xóa Sạch (Clear)** ngay trên HUD.
4. Vẽ xong, bấm **`Thoát Chế Độ Vẽ Cọ`** trong Inspector.
5. Thoát khỏi Prefab Stage (Unity sẽ tự động lưu lại dữ liệu đã vẽ trực tiếp vào Prefab phòng của bạn).

---

## ⚡ 3. Các Tính Năng Cao Cấp Cho Preset Toàn Cục

Khi chỉnh sửa file Preset toàn cục ScriptableObject (`RoomSpawnPreset`), hệ thống cung cấp 2 tính năng cực kỳ mạnh mẽ:

### 📥 A. Tách Vùng preset thành Vùng Cục Bộ (Override)
Nếu bạn có một căn phòng đặc biệt muốn thừa hưởng 90% cấu hình của Preset mẫu nhưng muốn chỉnh lại nét cọ vẽ cho khớp với địa hình phòng:
1. Gán Preset mẫu đó vào ô `Global Spawn Preset` trong `RoomPrefabData`.
2. Bấm nút **`💥 Tách Thành Vùng Cục Bộ Để Vẽ Tay`**.
3. Hệ thống sẽ tự động sao chép toàn bộ các vùng, quy tắc spawn, mật độ từ preset vào phòng này và chuyển sang dạng cục bộ để bạn tha hồ sửa đổi mà không ảnh hưởng đến các phòng khác dùng chung preset.

### 📦 B. Vùng Con Nhúng (Embed Zone)
* Khi bấm nút **`＋ Thêm Vùng Nhúng (Embed)`** trong Preset, hệ thống sẽ tự động tạo dữ liệu vùng (`SpawnZoneData`) và lưu **trực tiếp bên trong** file asset Preset chính.
* Điều này giúp folder dự án của bạn luôn sạch sẽ, không bị tràn ngập hàng trăm file `.asset` nhỏ lẻ.

---

## 🚀 4. Bí Quyết Tối Ưu Hiệu Năng Cho Máy Yếu (Zero-GC Spawning)

Để game của bạn sinh map mượt mà, không bị giật lag (micro-stutter) do dọn rác bộ nhớ (Garbage Collection), hệ thống đã được tối ưu hóa triệt để:

### 1. Kiến Trúc Không Cấp Phát Bộ Nhớ Rác (Zero-GC Generation)
* **Spatial Hash Grid tối ưu**: Grid phân vùng không gian để kiểm tra khoảng cách vật thể sử dụng một cơ chế **List Pool** đặc biệt. Các list chứa tọa độ được tái chế liên tục thay vì tạo mới, giúp giảm hoàn toàn GC allocations về 0 khi sinh hàng ngàn vật thể.
* **Double-Pass Resolver**: Thuật toán tính toán vùng đè lên nhau (dựa trên Priority) được thực hiện qua hai lượt duyệt trực tiếp trên danh sách gốc. Không sinh ra bất kỳ List trung gian nào mỗi khi kiểm tra điểm spawn.
* **Tái sử dụng mảng tĩnh**: Danh sách vị trí các cửa và vật thể được duy trì qua các biến toàn cục dùng lại (`tempDoorPositions`), tránh tạo mới mảng động liên tục.
* **Object Pooling**: Vật thể sinh ra được lấy từ `VegetationPooler` thay vì gọi `Instantiate` và `Destroy` liên tục (gây lag nặng).

### 2. Các Thiết Lập Khuyến Nghị Dành Cho Máy Yếu
Khi cấu hình các Spawn Rule và Phòng, hãy chú ý các thông số sau để máy yếu hoạt động tốt nhất:

> [!TIP]
> * **Giới hạn số lượng tối đa (`maxTotalSpawns`)**: Khống chế tổng số lượng vật thể trong mỗi phòng ở mức vừa phải (ví dụ: `50 - 150` tùy kích thước phòng). Tránh đặt số lượng quá lớn làm nghẽn GPU khi render.
> * **Đặt khoảng cách tối thiểu hợp lý (`minDistanceBetween`)**: Không nên đặt khoảng cách quá nhỏ (dưới `0.5`) kết hợp với số lượng spawn mong muốn quá lớn. Điều này buộc thuật toán phải thử đi thử lại nhiều lần (max attempts) để tìm vị trí trống, gây quá tải CPU. Đặt khoảng cách khoảng `1.2 - 2.0` cho cây lớn và `0.8 - 1.2` cho bụi cỏ.
> * **Hạn chế đè lớp quá phức tạp**: Cố gắng phân chia các vùng spawn rõ ràng bằng cọ vẽ, tránh để quá nhiều vùng đè lên nhau với số lượng ưu tiên sát nhau, giúp bộ giải quyết overlap xử lý nhanh nhất có thể.
> * **Bật/Tắt tính năng lọc vật lý (`obstacleLayer`)**: Nếu không thực sự cần thiết, hãy để `Obstacle Layer` trong `SpawnZoneManager` là `Nothing`. Quét va chạm vật lý (`Physics2D.OverlapCircle`) trong lúc sinh bản đồ tốn tài nguyên hơn kiểm tra lưới tọa độ rất nhiều.

---
 
 ## 🌲 5. Giải Thích Thuộc Tính & Cách Giữ Nấm/Vật Thể Mọc Thẳng
 
 ### 🎯 5.1 Giải thích các thuộc tính Spawn Zone chính:
 * **Tỉ Lệ Xuất Hiện Vùng (% - `zoneSpawnChance`) [MỚI]**: Cấu hình xác suất vùng spawn đó có được tạo ra trong phòng hay không. Đặt `100%` để vùng luôn xuất hiện; đặt thấp hơn (ví dụ: `40%`) để thỉnh thoảng vùng này mới xuất hiện, giúp tăng tính biến hóa và ngẫu nhiên cho màn chơi (ví dụ: thỉnh thoảng mới có vùng mọc nấm độc hoặc hòm kho báu).
 * **Độ Ưu Tiên (Priority)**: Khi các vùng spawn đè lên nhau, vùng nào có độ ưu tiên cao hơn sẽ chiếm quyền kiểm soát điểm đó.
 * **Trọng Số Giao Thoa (Spawn Weight)**: Khi các vùng có cùng Độ Ưu Tiên chồng lên nhau, trọng số này sẽ quyết định tỉ lệ ngẫu nhiên vùng nào chiến thắng.
 * **Mật Độ (Density)**: Số lượng vật thể tối đa mong muốn phân bổ trên mỗi ô vuông đơn vị.
 
 ### 🍄 5.2 Tại sao Nấm / Cây mọc nghiêng và cách cấu hình Mọc Thẳng Đứng:
 Trong hệ thống Spawn Rule, có thuộc tính **`Random Rotation`** (Xoay ngẫu nhiên quanh trục Z).
 * **Khi bật `Random Rotation` (Mặc định)**: Vật thể khi spawn ra sẽ bị xoay ngẫu nhiên từ `0` đến `360` độ. Đối với các vật thể nhìn từ trên xuống hoàn toàn (như bụi cỏ dẹt, đá tròn), điều này giúp chúng trông tự nhiên hơn.
 * **Tại sao nấm/cây bị lạ**: Đối với nấm, cây hoặc các sprite 2D có hướng đứng cố định (hướng thẳng đứng lên trên màn hình), việc xoay trục Z sẽ làm chúng bị nằm ngang, chúc đầu xuống hoặc nghiêng ngả.
 * **Cách khắc phục**:
   1. Trong cửa sổ **Project**, click chọn file **`SpawnRule`** của loại nấm đó (ví dụ: `SpawnRule_Mushroom`).
   2. Trong bảng **Inspector**, tìm đến nhóm **Căn Chỉnh Transform**.
   3. **Bỏ tích chọn** ô **`Random Rotation`** (Xoay Ngẫu Nhiên).
   4. Sau khi bỏ chọn, nấm và cây sẽ luôn mọc thẳng đứng `(Quaternion.identity)` như cách bạn đã thiết lập ban đầu!

  ### 🎲 5.3 Sinh ngẫu nhiên nhiều loại vật thể trong cùng một Spawn Rule [MỚI]:
  Để giảm số lượng file `SpawnRule` cần tạo khi bạn muốn mọc xen kẽ nhiều loại vật thể khác nhau (ví dụ: mọc ngẫu nhiên nấm xanh, nấm đỏ, nấm nhỏ trong cùng một khu vực):
  1. Mở file **`SpawnRule`** của bạn.
  2. Tại bảng **Inspector**, bạn sẽ thấy danh sách **`Prefab Variants`** (Các biến thể Prefab):
     * Bấm **`+`** để thêm các biến thể.
     * **Prefab**: Kéo thả Prefab của vật thể muốn spawn (nấm xanh, nấm đỏ, v.v.).
     * **Weight (Trọng số)**: Nhập độ ưu tiên xuất hiện cho biến thể đó. Ví dụ:
       * Nấm Thường (Weight = 70)
       * Nấm Xanh Lấp Lánh (Weight = 25)
       * Nấm Độc Hiếm (Weight = 5)
       * Hệ thống sẽ tự động tính toán tỉ lệ phần trăm dựa trên tổng trọng số này.
  3. **Cơ chế Fallback thông minh**: Nếu danh sách biến thể này trống, hệ thống sẽ tự động quay về sử dụng **Prefab chính** đã gán ở trường `Prefab` phía trên. Điều này giúp toàn bộ các file Spawn Rule cũ của bạn hoạt động bình thường mà không lo lỗi dữ liệu!

  ---
 
 ## ⌨️ 6. Bảng Phím Tắt Khi Vẽ Trong Scene View
 
 | Phím Tắt / Thao Tác | Hành Động | Ý Nghĩa |
 | :--- | :--- | :--- |
 | **Chuột Trái & Kéo** | Vẽ (Paint) | Tô màu vùng spawn lên các ô lưới. |
 | **Ctrl + Chuột Trái & Kéo** | Xóa (Erase) | Xóa bọ vẽ tại các ô lưới đã chọn. |
 | **Bảng HUD góc màn hình** | Brush Size / Actions | Đổi cỡ cọ (1x1, 2x2, 3x3) hoặc Tô đầy/Xóa sạch vùng cực nhanh. |
 | **Gizmos Handles (Tâm / Rìa)** | Điều chỉnh hình học | Kéo thả trực tiếp để thay đổi vị trí tâm, bán kính hình tròn, các đỉnh đa giác. |
 
 Chúc bạn thiết kế được những hầm ngục sinh động và tối ưu hiệu năng tốt nhất! Nếu gặp khó khăn gì trong quá trình vẽ, hãy xem lại tài liệu này nhé.
