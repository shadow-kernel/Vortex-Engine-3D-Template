using Vortex;

// Shared in-game settings screen (Options.vui) — ONE instance globally, opened from the lobby OPTIONS button
// AND the Match pause menu, so both reuse the exact same screen + apply logic (no duplication). A behaviour
// calls Open()/Close() from its button actions and Tick() every frame; the widgets apply to the engine live.
// Not a VortexBehaviour (it's a static helper the running controllers drive).
public static class OptionsMenu
{
    private static VuiHandle _ui;
    private static bool _open;

    // last-applied values (start at the .vui's authored defaults) -> apply only on a real change, no per-frame spam
    private static float _lastFov = 90f, _lastScale = 1f, _lastVol = 0.8f;
    private static bool _lastVsync = true, _lastFull;
    private static int _lastResIdx = 2;
    private static int _lastDlss = 0;   // 0=Off, 1=Quality, 2=Balanced, 3=Performance, 4=Ultra Perf
    private static int _lastFg = 0;     // 0=Off, 1=x2, 2=x3, 3=x4 (DLSS Frame Generation)
    private static readonly int[] _resW = { 1280, 1600, 1920, 2560 };
    private static readonly int[] _resH = { 720, 900, 1080, 1440 };

    public static bool IsOpen { get { return _open; } }

    public static void Open()
    {
        if (_ui == null) _ui = Gui.Load("Options.vui");
        if (_ui == null) return;
        _ui.Show();
        _open = true;
        // DLSS + Frame-Gen rows only make sense on a DLSS-capable GPU; hide them everywhere else.
        bool dlss = Settings.DlssSupported;
        _ui.SetVisible("dlssLabel", dlss);
        _ui.SetVisible("dlssStepper", dlss);
        _ui.SetVisible("fgLabel", dlss);
        _ui.SetVisible("fgStepper", dlss);
        _ui.SetVisible("presentedLabel", dlss);
        RefreshLabels();
    }

    public static void Close()
    {
        if (_ui != null) _ui.Hide();
        _open = false;
    }

    // Apply each setting the instant its widget changes. FOV / VSync / Fullscreen / Resolution take effect
    // immediately; render-scale + master volume are stored in the engine (their features apply them).
    public static void Tick()
    {
        if (!_open || _ui == null || !_ui.IsValid) return;

        float fov = _ui.GetSlider("fovSlider");
        if (System.Math.Abs(fov - _lastFov) > 0.05f) { _lastFov = fov; Settings.SetFieldOfView(fov); _ui.SetText("fovLabel", "Field of View    " + (int)System.Math.Round((double)fov)); }

        float scale = _ui.GetSlider("renderScaleSlider");
        if (System.Math.Abs(scale - _lastScale) > 0.005f) { _lastScale = scale; Settings.SetRenderScale(scale); _ui.SetText("scaleLabel", "Render Scale    " + (int)System.Math.Round((double)(scale * 100f)) + "%"); }

        float vol = _ui.GetSlider("volumeSlider");
        if (System.Math.Abs(vol - _lastVol) > 0.005f) { _lastVol = vol; Settings.SetMasterVolume(vol); _ui.SetText("volLabel", "Master Volume    " + (int)System.Math.Round((double)(vol * 100f)) + "%"); }

        bool vs = _ui.GetToggle("vsyncToggle");
        if (vs != _lastVsync) { _lastVsync = vs; Settings.SetVSync(vs); }

        bool fs = _ui.GetToggle("fullscreenToggle");
        if (fs != _lastFull) { _lastFull = fs; Settings.SetFullscreen(fs); }

        int ri = _ui.GetStep("resolutionStepper");
        if (ri != _lastResIdx && ri >= 0 && ri < _resW.Length) { _lastResIdx = ri; Settings.SetResolution(_resW[ri], _resH[ri]); }

        int dl = _ui.GetStep("dlssStepper");   // 0=Off, 1..4 = Quality/Balanced/Performance/Ultra Perf
        if (dl != _lastDlss && dl >= 0 && dl <= 4) { _lastDlss = dl; Settings.SetDlssMode(dl); }

        int fg = _ui.GetStep("fgStepper");     // 0=Off, 1=x2, 2=x3, 3=x4
        if (fg != _lastFg && fg >= 0 && fg <= 3) { _lastFg = fg; Settings.SetFrameGenMode(fg); }

        // Real vs Shown FPS. The engine counts REAL rendered frames; Frame Gen inserts AI frames at Present, so the
        // x2/x3/x4 shows up in "Shown" (engine-accumulated rate), NOT in the real counter. Both come from the engine.
        int real = Settings.CurrentFps;
        if (_lastFg > 0)
        {
            string mult = _lastFg == 1 ? "x2" : (_lastFg == 2 ? "x3" : "x4");
            string line = "Real " + real + "  /  Shown " + Settings.FrameGenPresentedFps + " (" + mult + ")";
            // FG raises LOW fps toward the refresh rate; above it (you're already maxing the monitor) it only adds latency.
            if (real >= 120) line += "   — note: above your refresh, FG only adds latency here";
            _ui.SetText("presentedLabel", line);
        }
        else
        {
            _ui.SetText("presentedLabel", "Real " + real + " FPS   (Frame Gen off)");
        }
    }

    private static void RefreshLabels()
    {
        if (_ui == null) return;
        _ui.SetText("fovLabel", "Field of View    " + (int)System.Math.Round((double)_lastFov));
        _ui.SetText("scaleLabel", "Render Scale    " + (int)System.Math.Round((double)(_lastScale * 100f)) + "%");
        _ui.SetText("volLabel", "Master Volume    " + (int)System.Math.Round((double)(_lastVol * 100f)) + "%");
    }
}
