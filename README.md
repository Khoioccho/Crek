# Crek — Post-Apocalypse (Prologue)

A 3D Unity prototype. This first slice delivers the **intro screen** and the
**opening tutorial**, exactly as requested. Combat and character customization are
stubbed out as the next chapter.

> Engine: **Unity 6 (6000.4.9f1)**, **URP**, **new Input System**.

## How to run

1. Open the project folder in **Unity Hub** and launch it with Unity 6 (6000.4.x).
2. The first time it loads, an editor script auto-configures the project for **3D**
   (it was created from the URP *2D* template) and prints a line in the Console.
3. Open **`Assets/Scenes/SampleScene.unity`** and press **Play**.

That's it — the whole game builds itself from code at runtime, so there is nothing
to wire up in the Inspector.

## Controls

| Action | Key |
|---|---|
| Move | **W A S D** |
| Look | **Mouse** |
| Run | **Left Shift** |
| Jump | **Space** |
| Dodge roll (i-frames) | **C** |
| Light attack | **Left-click** or **F** |
| Heavy attack | **Right-click** |
| Lock-on | **Tab** / **Q** / **Middle-click** |
| Toggle first/third person | **V** |
| Zoom (third person) | **Mouse wheel** |
| Talk / advance dialogue | **E** or **Left-click** |
| Pause / free cursor | **Esc** |
| Start game (menu) | **Enter** / click **START** |

## What's in the prologue

- **Intro screen** with a windswept post-apocalypse backdrop that **drifts** and
  **cross-fades to a new scene every 10 seconds** (GTA load-screen style), a parallax
  grass foreground, title, and **START / QUIT** buttons.
- **Wake up** in a ruined house (fade-in + intro text).
- **Objective flow:** leave the house → follow the **waypoint north** to the village.
- **Meet the first NPC** — a villager who warns of a **goblin raid** and asks for help.
- A **goblin wave** marches in from the north as a teaser, then the **Prologue
  Complete** banner points to what's next.

## How it's structured (`Assets/Scripts`)

| File | Role |
|---|---|
| `PA_Bootstrap.cs` | Boots the game from code (no scene setup needed). |
| `PA_GameManager.cs` | Owns the camera/sun, game state, pause, menu↔play transitions. |
| `PA_MainMenu.cs` | The intro screen (cycling backdrops + buttons). |
| `PA_HUD.cs` | Fade, objectives, toasts, waypoint, dialogue, banners, crosshair. |
| `PA_World.cs` | Builds the house, path, village, NPC, decorations, player & goblins. |
| `PA_Player.cs` | First-person `CharacterController` movement & look. |
| `PA_NPC.cs` / `PA_Goblin.cs` | Villager interaction / goblin teaser AI. |
| `PA_Tutorial.cs` | The prologue step sequence. |
| `PA_Art.cs` | Procedural materials & textures (no external art assets). |
| `PA_Input.cs` | New Input System wrapper. |
| `Editor/PA_ProjectSetup.cs` | One-time switch from the 2D template to a 3D URP renderer. |

## Planned next (hooks already in place)

- Character customization after the tutorial (the wake-up beat is the entry point).
- Real goblin combat & the village-defense battle.

## Note

A previous non-functional 2D stub (`PostApocalypseGame.cs`, which used the old input
system that is disabled in this project) was removed and replaced by the system above.
