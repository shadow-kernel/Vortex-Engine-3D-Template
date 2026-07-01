using Vortex;

// VORTEX — Lobby / start-screen camera director (100% GAME-side; lives in the project, not the engine).
// Attach this to the Main Camera of the "Lobby" scene. It frames a small furnished stage near the origin and
// glides cinematically between three tab viewpoints — HOME / STATS / OPTIONS — so the start screen is a living
// 3D world, not a flat 2D panel. The 2D menu (buttons) is the HorrorLobby.vui screen, routed to HorrorLobbyActions.
// Keyboard: 1/2/3 or Tab switch tabs, Enter = play. Controller: LB/RB or D-Pad switch tabs, A = play.
public class LobbyController : VortexBehaviour
{
    public float SlideSpeed = 3.4f;
    public float SwayAmount = 2.0f;
    public float SwaySpeed  = 0.45f;

    private Vector3 _focus = new Vector3(0f, 1.0f, 0f);

    private struct View { public Vector3 Pos; public float Yaw, Pitch; }
    private View[] _views;
    private int _tab;
    private bool _tabHeld, _padTabHeld;

    private Vector3 _pos;
    private float _yaw, _pitch;
    private float _t;

    private VuiHandle _ui;
    private bool _stageBuilt;

    public override void Start()
    {
        Cursor.Locked = false; // start screen: free mouse so the player can click the UI

        _views = new View[3];
        _views[0] = MakeView(new Vector3(0f,    1.7f, 4.6f)); // HOME    — straight on
        _views[1] = MakeView(new Vector3(-3.9f, 1.9f, 3.1f)); // STATS   — swung left
        _views[2] = MakeView(new Vector3(3.9f,  1.9f, 3.1f)); // OPTIONS — swung right

        _tab = 0;
        _pos = _views[0].Pos; _yaw = _views[0].Yaw; _pitch = _views[0].Pitch;
        Apply();

        _ui = Gui.Load("HorrorLobby.vui");
        if (_ui != null)
        {
            _ui.Show();
            // Neutralize the old horror overlays so the start screen reads as a clean 3D starter (UI kept, mood neutral).
            _ui.SetColor("blood", Color.Rgba(0, 0, 0, 0));
            _ui.SetColor("lightning", Color.Rgba(0, 0, 0, 0));
            _ui.SetColor("flicker", Color.Rgba(0, 0, 0, 0));
            _ui.SetColor("title", Color.Rgb(240, 240, 245));
            _ui.SetVisible("neon", false);
        }
        // Clean, bright daylight so the stage looks like a neutral render.
        Lighting.SetAmbient(0.55f);
        Lighting.SetDirectional(-0.35f, -0.78f, -0.42f, 1.0f, 0.98f, 0.92f, 2.4f);
    }

    // A clean, furnished little showroom behind the menu — built at runtime from CC0 Kenney furniture (no scene
    // editing). Neutral, well-lit, generic: the kind of stage a 3D starter template ships with.
    private void BuildStage()
    {
        _stageBuilt = true;
        World.Clear();

        const string F = "Assets/Models/kenney/furniture/";
        const float T = 1f;
        int half = 3;

        // floor grid
        for (int gx = -half; gx <= half; gx++)
            for (int gz = -half; gz <= half; gz++)
                World.Add(F + "floorFull.glb", gx * T, 0f, gz * T, 0f, 1f);

        // back + side walls (open toward the camera)
        for (int gx = -half; gx <= half; gx++)
        {
            World.Add(F + "wall.glb", gx * T, 0f, -half * T - 0.5f, 0f, 1f);
            World.Add(F + "wall.glb", -half * T - 0.5f, 0f, gx * T, 90f, 1f);
            World.Add(F + "wall.glb",  half * T + 0.5f, 0f, gx * T, 270f, 1f);
        }

        // a tidy furnished set
        World.Add(F + "kitchenBar.glb",        0f, 0f, -2.2f, 0f, 1f);
        World.Add(F + "bedDouble.glb",        -2.2f, 0f, -2.0f, 0f, 1f);
        World.Add(F + "sideTable.glb",        -3.0f, 0f, -2.6f, 0f, 1f);
        World.Add(F + "televisionVintage.glb", 2.4f, 0.0f, -2.6f, 200f, 1f);
        World.Add(F + "rugRectangle.glb",      0f, 0.02f, 0.5f, 0f, 1f);
        World.Add(F + "pottedPlant.glb",      -2.8f, 0f, 1.8f, 0f, 1f);
        World.Add(F + "chair.glb",             1.6f, 0f, 0.8f, 200f, 1f);

        // a neutral character standing on the stage
        World.Add("Assets/Models/character.glb", 0f, 0f, -0.4f, 180f, 1f);
    }

    private View MakeView(Vector3 pos)
    {
        float dx = _focus.X - pos.X, dy = _focus.Y - pos.Y, dz = _focus.Z - pos.Z;
        float len = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (len < 1e-4f) len = 1f;
        dx /= len; dy /= len; dz /= len;
        View v;
        v.Pos = pos;
        v.Yaw = (float)(System.Math.Atan2(dx, dz) * 180.0 / System.Math.PI);
        v.Pitch = (float)(System.Math.Asin(-dy) * 180.0 / System.Math.PI);
        return v;
    }

    public override void Update(float dt)
    {
        if (dt <= 0f) return;
        _t += dt;
        if (!_stageBuilt) BuildStage();   // device ready now -> import + place the stage

        // --- tab selection (keyboard + controller) ---
        bool tab = Input.GetKey("Tab");
        if (Input.GetKey("D1")) _tab = 0;
        else if (Input.GetKey("D2")) _tab = 1;
        else if (Input.GetKey("D3")) _tab = 2;
        else if (tab && !_tabHeld) _tab = (_tab + 1) % _views.Length;
        _tabHeld = tab;

        if (Input.GetGamepadButtonDown("DPadRight") || Input.GetGamepadButtonDown("RB")) _tab = (_tab + 1) % _views.Length;
        else if (Input.GetGamepadButtonDown("DPadLeft") || Input.GetGamepadButtonDown("LB")) _tab = (_tab + 2) % _views.Length;

        View target = _views[_tab];
        float k = SlideSpeed * dt; if (k > 1f) k = 1f;
        _pos.X += (target.Pos.X - _pos.X) * k;
        _pos.Y += (target.Pos.Y - _pos.Y) * k;
        _pos.Z += (target.Pos.Z - _pos.Z) * k;
        _yaw   += ShortestDelta(_yaw, target.Yaw) * k;
        _pitch += (target.Pitch - _pitch) * k;
        Apply();

        // Play: keyboard Enter or controller A. (HorrorLobby.vui's PLAY button is routed to HorrorLobbyActions.)
        if (Input.GetKey("Return") || Input.GetKey("Enter") || Input.GetGamepadButtonDown("A"))
            Scene.Load("Match");

        OptionsMenu.Tick();   // apply the shared options screen live, if it's open
    }

    private void Apply()
    {
        float sway = (float)System.Math.Sin(_t * SwaySpeed) * SwayAmount;
        Vector3 p = _pos;
        if (Bad(p.X)) p.X = 0f;
        if (Bad(p.Y)) p.Y = 1.7f;
        if (Bad(p.Z)) p.Z = 4.6f;
        float yaw = _yaw + sway, pitch = _pitch;
        if (Bad(yaw)) yaw = 0f;
        if (Bad(pitch)) pitch = 0f;
        Position = p;
        Rotation = new Vector3(pitch, yaw, 0f);
    }

    private static float ShortestDelta(float from, float to)
    {
        if (Bad(from) || Bad(to)) return 0f;
        float d = (to - from) % 360f;
        if (d > 180f) d -= 360f;
        else if (d < -180f) d += 360f;
        return d;
    }

    private static bool Bad(float f) { return float.IsNaN(f) || float.IsInfinity(f); }
}
