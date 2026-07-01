using Vortex;

// First-person player movement — 100% GAME-side. WASD = move, mouse = look, Shift = sprint, Space = jump,
// Ctrl/C = crouch, ESC = open menu (frees the mouse + freezes you; the game keeps running, it is NOT paused).
public class PlayerController : VortexBehaviour
{
    public float WalkSpeed   = 6f;
    public float SprintSpeed = 10f;
    public float CrouchSpeed = 3f;
    public float MouseSens   = 0.10f;
    public float JumpSpeed   = 8.5f;  // snappier jump than the old 7.5
    public float Gravity     = 26f;   // snappier fall than the old 20 (tighter arc, grounded feel)
    public float CrouchDrop  = 0.7f;
    public float Fov         = 70f;   // vertical field of view (degrees); adjustable in the ESC settings
    // Crisp Quake/Source-style ground feel (no more mushy lerp):
    public float GroundAccel = 12f;   // how hard you accelerate to top speed on the ground
    public float AirAccel    = 2.5f;  // limited mid-air steering (natural air control)
    public float Friction    = 9f;    // ground stop strength (higher = snappier, no drift)
    public float PadLookSpeed = 220f; // controller right-stick look speed (deg/sec at full deflection)

    private float _standEyeY;
    private float _vx, _vz, _vy;
    private bool  _grounded;
    private bool  _jumpHeld;
    private bool  _escHeld;
    private bool  _paused;             // ESC pause menu (retained VUI — same style as the lobby)
    private VuiHandle _pauseUi;
    private float _pitch, _yaw;

    public override void Start()
    {
        Cursor.Locked = true;
        _standEyeY = Position.Y;
        var r = Rotation; _pitch = r.X; _yaw = r.Y;
        _vx = _vz = _vy = 0f;
        _grounded = true;
        Camera.SetFieldOfView(Fov);
    }

    public override void Update(float dt)
    {
        if (dt <= 0f) return;

        // ESC toggles the retained pause menu (same VUI style as the lobby). It frees the mouse + freezes the
        // player; the game keeps running (not a hard pause). While the Options screen is up it freezes input,
        // so ESC won't toggle then — use the on-screen BACK button (mouse).
        bool esc = Input.GetKey("Escape") || Input.GetGamepadButtonDown("Start");
        if (esc && !_escHeld) TogglePause();
        _escHeld = esc;

        if (Cursor.Locked) MovePlayer(dt);
        else { _vx = 0f; _vz = 0f; } // menu open: frozen, no drift on resume

        OptionsMenu.Tick();           // apply the settings screen's widgets live, if it's open
        DrawHud();
    }

    // ===== pause menu (PauseMenu.vui) =====
    // The pause UI lives WITH the player on purpose: ESC/open/Resume are tied to the player's cursor-lock +
    // movement-freeze state, and an entity holds a single behaviour — so this is the player's own screen, not a
    // separate UI actions class. The engine routes PauseMenu.vui's button clicks (OnResume/OnPauseOptions/
    // OnToLobby/OnQuitGame) to these methods. (Options.vui's BACK is its own class, OptionsActions.)
    private void TogglePause() { if (_paused) Resume(); else Pause(); }
    private void Pause()
    {
        if (_pauseUi == null) _pauseUi = Gui.Load("PauseMenu.vui");
        if (_pauseUi != null) _pauseUi.Show();
        _paused = true;
        Cursor.Locked = false;        // free + show the cursor; the MovePlayer gate above freezes the player
    }
    private void Resume()
    {
        OptionsMenu.Close();          // close the settings screen too if it was open
        if (_pauseUi != null) _pauseUi.Hide();
        _paused = false;
        Cursor.Locked = true;         // recapture the cursor -> mouse-look resumes
    }
    public void OnResume() { Resume(); }
    public void OnPauseOptions() { OptionsMenu.Open(); }   // opens the shared Options.vui over the pause menu
    public void OnToLobby() { Scene.Load("Lobby"); }
    public void OnQuitGame() { Application.Quit(); }       // now actually closes the standalone player (native-loop exit)

    private void MovePlayer(float dt)
    {
        // ---- Look ----
        _yaw   += Input.MouseDeltaX * MouseSens;
        _pitch += Input.MouseDeltaY * MouseSens;
        // controller right stick = look (up on the stick looks up)
        _yaw   += Input.RightStickX * PadLookSpeed * dt;
        _pitch -= Input.RightStickY * PadLookSpeed * dt;
        if (Input.GetKey("Left"))  _yaw   -= 90f * dt;
        if (Input.GetKey("Right")) _yaw   += 90f * dt;
        if (Input.GetKey("Up"))    _pitch -= 90f * dt;
        if (Input.GetKey("Down"))  _pitch += 90f * dt;

        // Reject NaN/Inf BEFORE the modulo + clamp (a bad Euler must never reach the native quaternion math).
        if (float.IsNaN(_yaw)   || float.IsInfinity(_yaw))   _yaw   = 0f;
        if (float.IsNaN(_pitch) || float.IsInfinity(_pitch)) _pitch = 0f;
        if (_pitch > 89f) _pitch = 89f; else if (_pitch < -89f) _pitch = -89f;
        _yaw %= 360f;
        Rotation = new Vector3(_pitch, _yaw, 0f);

        // ---- Wish direction (from yaw) ----
        bool crouch = Input.GetKey("LeftCtrl") || Input.GetKey("C") || Input.GetGamepadButton("B");
        bool sprint = Input.GetKey("LeftShift") || Input.GetGamepadButton("LeftStick");
        float maxSpeed = crouch ? CrouchSpeed : (sprint ? SprintSpeed : WalkSpeed);

        double yawRad = _yaw * System.Math.PI / 180.0;
        float fX = (float)System.Math.Sin(yawRad), fZ = (float)System.Math.Cos(yawRad);
        float rX = (float)System.Math.Cos(yawRad), rZ = (float)-System.Math.Sin(yawRad);
        float dx = 0f, dz = 0f;
        if (Input.GetKey("W")) { dx += fX; dz += fZ; }
        if (Input.GetKey("S")) { dx -= fX; dz -= fZ; }
        if (Input.GetKey("D")) { dx += rX; dz += rZ; }
        if (Input.GetKey("A")) { dx -= rX; dz -= rZ; }
        // controller left stick = move (Y up on the stick = forward)
        dx += fX * Input.LeftStickY + rX * Input.LeftStickX;
        dz += fZ * Input.LeftStickY + rZ * Input.LeftStickX;
        float wl = (float)System.Math.Sqrt(dx * dx + dz * dz);
        float wishX = 0f, wishZ = 0f;
        if (wl > 0.001f) { wishX = dx / wl; wishZ = dz / wl; }

        // ---- Quake/Source movement: friction on the ground, then accelerate toward the wish dir ----
        if (_grounded)
        {
            ApplyFriction(dt);                                // snappy stop, no floaty drift
            Accelerate(wishX, wishZ, maxSpeed, GroundAccel, dt);
        }
        else
        {
            Accelerate(wishX, wishZ, maxSpeed, AirAccel, dt); // limited air steering
        }
        if (float.IsNaN(_vx) || float.IsInfinity(_vx)) _vx = 0f;
        if (float.IsNaN(_vz) || float.IsInfinity(_vz)) _vz = 0f;
        if (float.IsNaN(_vy) || float.IsInfinity(_vy)) _vy = 0f;

        // ---- Apply move + jump/gravity ----
        float eyeY = crouch ? _standEyeY - CrouchDrop : _standEyeY;
        Vector3 p = Position;
        p.X += _vx * dt;
        p.Z += _vz * dt;

        bool jump = Input.GetKey("Space") || Input.GetGamepadButton("A");
        if (_grounded)
        {
            if (jump && !_jumpHeld) { _vy = JumpSpeed; _grounded = false; }
            else { p.Y = eyeY; _vy = 0f; }
        }
        if (!_grounded)
        {
            _vy -= Gravity * dt;
            p.Y += _vy * dt;
            if (p.Y <= eyeY) { p.Y = eyeY; _vy = 0f; _grounded = true; }
        }
        _jumpHeld = jump;

        if (float.IsNaN(p.X) || float.IsInfinity(p.X)) p.X = 0f;
        if (float.IsNaN(p.Y) || float.IsInfinity(p.Y)) p.Y = eyeY;
        if (float.IsNaN(p.Z) || float.IsInfinity(p.Z)) p.Z = 0f;
        Position = p;
    }

    // Accelerate toward the wish direction up to wishSpeed (classic Quake/Source model). Only adds the speed
    // still missing along the wish dir, so it's crisp on the ground and gives natural air control in the air.
    private void Accelerate(float wishX, float wishZ, float wishSpeed, float accel, float dt)
    {
        float current = _vx * wishX + _vz * wishZ; // speed already going the way we want
        float add = wishSpeed - current;
        if (add <= 0f) return;
        float accelSpeed = accel * wishSpeed * dt;
        if (accelSpeed > add) accelSpeed = add;
        _vx += wishX * accelSpeed;
        _vz += wishZ * accelSpeed;
    }

    // Ground friction: bleed off horizontal speed each frame so stopping is snappy instead of floaty.
    private void ApplyFriction(float dt)
    {
        float speed = (float)System.Math.Sqrt(_vx * _vx + _vz * _vz);
        if (speed < 0.0001f) { _vx = 0f; _vz = 0f; return; }
        float newSpeed = speed - speed * Friction * dt;
        if (newSpeed < 0f) newSpeed = 0f;
        float scale = newSpeed / speed;
        _vx *= scale; _vz *= scale;
    }

    private void DrawHud()
    {
        float W = UI.Width, H = UI.Height;
        if (W < 10f) return;

        // FPS counter (top-right, always visible). With Frame Gen on, the BIG green number is the SHOWN rate (real +
        // AI-generated frames) — that's where x2/x3/x4 appears — with a small honest line below: the multiplier + the
        // real rendered rate. (The engine renders the real frames; FG inserts the rest at Present.)
        int fgm = Settings.FrameGenMode;
        if (fgm > 0)
        {
            string mult = fgm == 1 ? "x2" : (fgm == 2 ? "x3" : "x4");
            UI.Text(Settings.FrameGenPresentedFps + " FPS", W - 138f, 12f, 126f, 26f, 16f, Color.Rgb(120, 230, 150), 2, 700);
            UI.Text(mult + " · " + Settings.CurrentFps + " real", W - 200f, 38f, 188f, 18f, 12f, Color.Rgb(95, 175, 115), 2, 600);
        }
        else
        {
            UI.Text(Settings.CurrentFps + " FPS", W - 138f, 12f, 126f, 26f, 16f, Color.Rgb(120, 230, 150), 2, 700);
        }

        // Gameplay HUD (crosshair + health) — only while playing. The PAUSE + SETTINGS menus are now retained
        // VUI screens (PauseMenu.vui / Options.vui, same style as the lobby), driven by the pause logic above.
        if (Cursor.Locked)
        {
            UI.Rect(W * 0.5f - 1f, H * 0.5f - 9f, 2f, 18f, Color.Rgba(255, 255, 255, 180), 0f);
            UI.Rect(W * 0.5f - 9f, H * 0.5f - 1f, 18f, 2f, Color.Rgba(255, 255, 255, 180), 0f);
        }
    }
}
