# UI scripts — one class per screen

Each `.vui` screen has **its own C# actions class**, named `<Screen>Actions`. A button's **On Click**
method (set in the UI editor) is routed by the engine to that screen's class automatically — there is no
single shared "UIActions" dumping file anymore, and method names can't collide between screens.

| Screen (`Assets/UI/…`) | Actions class                 | What it does                                   |
|------------------------|-------------------------------|------------------------------------------------|
| `HorrorLobby.vui`      | `HorrorLobbyActions.cs`       | ENTER / OPTIONS / LEAVE buttons                |
| `Options.vui`          | `OptionsActions.cs`           | BACK button (shared settings screen)           |
| `PauseMenu.vui`        | `PlayerController.cs`*        | RESUME / OPTIONS / LEAVE TO LOBBY / QUIT GAME  |

\* The pause menu lives **with the player** on purpose: ESC/open/Resume are tied to the player's cursor-lock
and movement-freeze, and an entity holds a single behaviour. So it's the player's own screen rather than a
standalone `PauseMenuActions` class. (If you'd rather have a dedicated `PauseMenuController` on its own scene
entity, that's a clean follow-up.)

## How the wiring works

- In the UI editor, select a Button → type a method name under **On Click → C# method** → **Create / Bind**.
  The editor creates/opens `Assets/Scripts/UI/<Screen>Actions.cs` and adds the method stub.
- At runtime, clicking the button fires that action **tagged with its screen**. The engine
  (`ScriptRuntime.InvokeUiActions`) finds the screen's `<Screen>Actions` class — attached to a scene entity,
  or **auto-created on first click** (no scene wiring needed) — and calls the method. If no such class exists
  it falls back to any running behaviour with that method, so hand-written controllers keep working.

## Rules for actions classes

- They have **no scene entity**, so use the static facades — `Scene.Load`, `Application.Quit`, `Gui.*`,
  `Settings.*`, `OptionsMenu.*`. Don't read `Position`/`Rotation` (they'll be zero).
- Keep per-frame screen-driving logic (animations, applying slider values, etc.) in the controller that owns
  the scene (e.g. `LobbyController` drives the lobby atmosphere and ticks `OptionsMenu`).

## Companion helpers (not screens)

- `OptionsMenu.cs` — the shared settings **apply logic** (FOV / VSync / resolution / render-scale / volume).
  It's a static helper the active controller ticks each frame while `Options.vui` is open; the BACK button
  itself is `OptionsActions`.
