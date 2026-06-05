# 🎮 Unity 2D Top-Down Procedural Dungeon Game & Editor Tools

A high-performance 2D top-down action adventure game built in Unity, featuring procedural dungeon generation, a custom 2D Y-Sorting depth system for modular characters, and custom editor tools designed to accelerate level design workflows.

---

## 🚀 Key Showcases

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

---

## 🛠️ Technical Implementation Details

### 1. Procedural Dungeon Generator
* **Algorithm-based Generation:** Generates random dungeon layouts at runtime based on configured room templates (Start, Combat, Boss, corridors).
* **Spawn Point Control:** Manages player and enemy spawn points using a Turkish-blue visual Gizmos helper (`LPCPlayerSpawnPoint`) to ensure consistent positioning before system culling takes over.
* **Performance Optimization:** Integrates a **Culling Manager** that dynamically disables rooms out of the camera's view to maintain 60 FPS even on lower-end devices.

### 2. 2D Dynamic Y-Sorting & Modular System
* **Modular Character Structure:** Supports character customization by layering up to 17 separate LPC animation sheets (Body, Legs, Feet, Torso, Armor, Hair, Weapon, etc.) synchronized under a single parent `Sorting Group`.
* **Custom Axis Depth Sorting:** Implements custom axis transparency sorting `(0, 1, 0)` combined with pivot-based sort points (aligned at the character's feet) to render depth naturally.
* **Dynamic Transparency (Roof Fader):** Utilizes `LPC_RoofFader` triggers to smoothly fade out roofs/canopies (lowering alpha to `0.25f`) when the player walks inside a tent or house, revealing obstacles and items underneath.

### 3. Custom Unity Editor Extensions
* **LPCTilePaletteGenerator:** A custom editor window accessible via `Tools -> LPC -> Tile Palette Generator`. Automatically parses folders containing hundreds of individual `.asset` Tile objects, sorts them naturally (e.g., `Objects_2` before `Objects_10`), layouts them into a neat grid, and saves them as a ready-to-use Tile Palette prefab.
* **Auto-Sorting Config Configurator:** A one-click utility inside the `LPC Player Controller` editor inspector to automatically setup sorting layers, sorting orders, and dẹt foot colliders (offsetting colliders to 15-20% height for smooth pathfinding).

---

## ⚙️ Tech Stack & Architecture
* **Game Engine:** Unity 2022.3 LTS (2D)
* **Render Pipeline:** Universal Render Pipeline (URP 2D)
* **Language:** C# (.NET)
* **Version Control:** Unity Version Control (Plastic SCM) & Git

---

## 🎮 How to Run and Play

### Play the WebGL Build
* You can play the WebGL demo directly in your browser: **[Link to itch.io page]**

### Open Project in Unity Editor
1. Clone the repository:
   ```bash
   git clone git@github.com:ocki26/Project-A.git
   ```
2. Open Unity Hub, click **Add**, and select the cloned folder.
3. Open the scene `Assets/Scenes/TownScene.unity` or `Assets/Test.unity` and click **Play**.
