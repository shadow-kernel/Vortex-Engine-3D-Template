using Vortex;

// Button actions for HorrorLobby.vui — ONE class per UI screen.
// The engine auto-wires this: a click on a HorrorLobby.vui button is routed to HorrorLobbyActions
// (no scene attachment needed). LobbyController still owns the lobby's camera + horror atmosphere and
// loads/drives the screen; THIS class is only the three menu buttons, so the wiring is obvious.
//
// UI action classes have no scene entity, so they use the static facades (Scene/Application/Gui), never
// Position/Rotation.
public class HorrorLobbyActions : VortexBehaviour
{
    // ENTER → start the match.
    public void OnEnter() { Scene.Load("Match"); }

    // OPTIONS → open the shared settings screen (Options.vui), driven by OptionsMenu.
    public void OnOptions() { OptionsMenu.Open(); }

    // LEAVE → quit the game. (Fixed: the standalone player now actually closes — it breaks the native
    // GameHost loop instead of a no-op Application.Shutdown that the blocked WPF Dispatcher never ran.)
    public void OnLeave() { Application.Quit(); }
}
