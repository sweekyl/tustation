using Content.Client.Lobby;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Goobstation.Client.Patron;

public sealed class PatronSupportUIController : UIController,
    IOnStateEntered<LobbyState>,
    IOnStateExited<LobbyState>
{
    [Dependency] private readonly IUriOpener _uriOpener = default!;

    private PatronSupportWindow? _supportWindow;
    private bool _hasShownThisSession;

    public void OnStateEntered(LobbyState state)
    {
        if (_hasShownThisSession)
            return;

        _hasShownThisSession = true;
        ShowSupportWindow();
    }

    public void OnStateExited(LobbyState state)
    {
        if (_supportWindow == null)
            return;

        _supportWindow.OnClose -= OnWindowClosed;
        _supportWindow.Close();
        _supportWindow = null;
    }

    private void ShowSupportWindow()
    {
        if (_supportWindow != null)
            return;

        _supportWindow = UIManager.CreateWindow<PatronSupportWindow>();
        _supportWindow.OnClose += OnWindowClosed;

        _supportWindow.PatreonButton.OnPressed += _ =>
        {
            _uriOpener.OpenUri("https://discord.gg/NeTvZmP5ce");
            _supportWindow?.Close();
        };

        _supportWindow.OpenCentered();
    }

    private void OnWindowClosed()
    {
        _supportWindow = null;
    }
}
