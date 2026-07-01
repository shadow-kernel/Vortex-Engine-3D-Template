using Vortex;

// Button actions for Options.vui — ONE class per UI screen.
// Options.vui is the SHARED settings screen, opened from both the lobby (OPTIONS) and the in-match pause
// menu (OPTIONS). Its BACK button routes here regardless of who opened it. The actual apply-on-change logic
// (FOV / VSync / resolution / render-scale / volume) lives in OptionsMenu, which the active controller
// (LobbyController or PlayerController) ticks each frame while the screen is open.
public class OptionsActions : VortexBehaviour
{
    // BACK → close the settings screen and return to whatever was underneath (lobby or pause menu).
    public void OnOptionsBack() { OptionsMenu.Close(); }
}
