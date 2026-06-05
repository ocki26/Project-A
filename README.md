# 🎮 Unity 2D Top-Down Procedural Dungeon Game & Editor Tools

🌐 **[English](#english) | [Tiếng Việt](#tiếng-việt)**

---

<a name="english"></a>
## 🇺🇸 English Version

A high-performance 2D top-down action adventure game built in Unity, featuring procedural dungeon generation, a custom 2D Y-Sorting depth system for modular characters, and custom editor tools designed to accelerate level design workflows.

### 🚀 Gameplay & Editor Showcase

> [!TIP]
> **HOW TO ADD YOUR GIFS/IMAGES:** 
> 1. Record short GIFs of your game (using tools like ScreenToGif or OBS).
> 2. Name them `gameplay.gif`, `dungeon_gen.gif`, and `editor_tool.gif`.
> 3. Create a folder named `Showcase/` in your project root, put the GIFs inside.
> 4. Once pushed, the links below will automatically load and display your images on GitHub!

| 🏃 Dynamic Y-Sorting & Modular Character | 🏰 Procedural Dungeon Generation |
| --- | --- |
| ![Y-Sorting Showcase](Showcase/gameplay.gif) | ![Dungeon Gen Showcase](Showcase/dungeon_gen.gif) |

| 🎨 LPCTilePaletteGenerator (Custom Editor Tool) |
| --- |
| ![Custom Editor Tool Showcase](Showcase/editor_tool.gif) |

### 🛠️ Technical Implementation Details

#### 1. Procedural Dungeon Generator
* **Algorithm-based Generation:** Generates random dungeon layouts at runtime based on configured room templates (Start, Combat, Boss, corridors).
* **Spawn Point Control:** Manages player and enemy spawn points using a Turkish-blue visual Gizmos helper (`LPCPlayerSpawnPoint`) to ensure consistent positioning before system culling takes over.
* **Performance Optimization:** Integrates a **Culling Manager** that dynamically disables rooms out of the camera's view to maintain 60 FPS even on lower-end devices.

#### 2. 2D Dynamic Y-Sorting & Modular System
* **Modular Character Structure:** Supports character customization by layering up to 17 separate LPC animation sheets (Body, Legs, Feet, Torso, Armor, Hair, Weapon, etc.) synchronized under a single parent `Sorting Group`.
* **Custom Axis Depth Sorting:** Implements custom axis transparency sorting `(0, 1, 0)` combined with pivot-based sort points (aligned at the character's feet) to render depth naturally.
* **Dynamic Transparency (Roof Fader):** Utilizes `LPC_RoofFader` triggers to smoothly fade out roofs/canopies (lowering alpha to `0.25f`) when the player walks inside a tent or house, revealing obstacles and items underneath.

#### 3. Custom Unity Editor Extensions
* **LPCTilePaletteGenerator:** A custom editor window accessible via `Tools -> LPC -> Tile Palette Generator`. Automatically parses folders containing hundreds of individual `.asset` Tile files, sorts them naturally (e.g., `Objects_2` before `Objects_10`), layouts them into a neat grid, and saves them as a ready-to-use Tile Palette prefab.
* **Auto-Sorting Config Configurator:** A one-click utility inside the `LPC Player Controller` editor inspector to automatically setup sorting layers, sorting orders, and dẹt foot colliders (offsetting colliders to 15-20% height for smooth pathfinding).

### ⚙️ Tech Stack & Architecture
* **Game Engine:** Unity 2022.3 LTS (2D)
* **Render Pipeline:** Universal Render Pipeline (URP 2D)
* **Language:** C# (.NET)
* **Version Control:** Unity Version Control (Plastic SCM) & Git

### 🕹️ How to Run in Unity Editor
1. Clone the repository:
   ```bash
   git clone git@github.com:ocki26/Project-A.git
   ```
2. Open Unity Hub, click **Add**, and select the cloned folder.
3. Open the scene `Assets/Scenes/TownScene.unity` or `Assets/Test.unity` and click **Play**.

---

<a name="tiếng-việt"></a>
## 🇻🇳 Phiên Bản Tiếng Việt

Dự án game nhập vai phiêu lưu 2D Top-Down hiệu năng cao được phát triển trên Unity, tích hợp các thuật toán sinh ngục tối ngẫu nhiên, hệ thống hiển thị độ sâu hình ảnh (Y-sorting) cho nhân vật modular ghép mảnh, và các công cụ Custom Editor giúp tăng tốc quy trình thiết kế màn chơi.

### 🛠️ Các Tính Năng Kỹ Thuật Chi Tiết

#### 1. Bộ Sinh Bản Đồ Tự Động (Procedural Dungeon Generator)
* **Sinh map ngẫu nhiên:** Tự động sắp xếp các phòng mẫu (Phòng xuất phát, phòng chiến đấu, phòng Boss và hành lang) một cách logic khi bấm Play.
* **Kiểm soát Spawn Point:** Định vị vị trí xuất hiện của nhân vật thông qua Gizmos hiển thị trực quan (`LPCPlayerSpawnPoint`), tránh lỗi lệch tọa độ camera khi tải map.
* **Tối ưu hóa hiệu năng (Culling):** Tự động tắt (disable) các phòng nằm ngoài tầm nhìn của Camera để giữ mức khung hình ổn định 60 FPS.

#### 2. Xử Lý Chiều Sâu 2D & Hệ Thống Nhân Vật Lắp Ghép (Modular Y-Sorting)
* **Nhân vật Modular 17 Layer:** Đồng bộ và ghép mảnh hơn 17 lớp Sprite chuyển động khác nhau của nhân vật (Body, áo giáp, tóc, vũ khí, khiên...) dưới một `Sorting Group` duy nhất để không bị chồng chéo ảnh.
* **Y-Sorting ở chân:** Sử dụng cơ chế sắp xếp Custom Axis `(0, 1, 0)` kết hợp cấu hình Pivot sát đáy chân để sắp xếp chiều sâu động tự nhiên khi nhân vật đi trước/sau cây cối, cột đá, hoặc quái vật.
* **Mái che thông minh (Roof Fader):** Gắn trigger kèm script `LPC_RoofFader` để tự động làm mờ mái lều/mái nhà (giảm Alpha về `0.25f`) khi người chơi đi vào bên trong, để lộ các thùng gỗ, rương báu dưới mái che.

#### 3. Bộ Công Cụ Custom Editor Windows (Tăng tốc thiết kế)
* **LPCTilePaletteGenerator:** Công cụ mở rộng trong Unity Editor (`Tools -> LPC -> Tile Palette Generator`). Quét nhanh hàng trăm file ô gạch (`.asset`), sắp xếp tự động theo thứ tự tên tự nhiên và tạo thành một Prefab Tile Palette để vẽ map trong 1 giây.
* **Auto-Sorting Configurator:** Nút bấm tự động cấu hình nhanh Sorting Layer, thứ tự hiển thị của nhân vật và co nhỏ collider chân nhân vật để tránh bị kẹt tường.

### ⚙️ Công Nghệ Sử Dụng
* **Engine:** Unity 2022.3 LTS (2D)
* **Render Pipeline:** Universal Render Pipeline (URP 2D)
* **Ngôn ngữ:** C# (.NET)
* **Quản lý mã nguồn:** Unity Version Control (Plastic SCM) & Git

### 🕹️ Hướng Dẫn Chạy Dự Án
1. Clone dự án về máy:
   ```bash
   git clone git@github.com:ocki26/Project-A.git
   ```
2. Mở Unity Hub, nhấn **Add** và chọn thư mục vừa clone.
3. Mở scene `Assets/Scenes/TownScene.unity` hoặc `Assets/Test.unity` và nhấn **Play**.
