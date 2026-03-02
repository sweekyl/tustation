using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Goobstation.Shared.PhraseWheel;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.PhraseWheel;

[UsedImplicitly]
public sealed class PhraseWheelUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private MenuButton? PhraseButton =>
        UIManager.GetActiveUIWidgetOrNull<Content.Client.UserInterface.Systems.MenuBar.Widgets.GameTopMenuBar>()?.PhraseWheelButton;

    private SimpleRadialMenu? _menu;

    public void OnStateEntered(GameplayState state)
    {
    }

    public void OnStateExited(GameplayState state)
    {
        CloseMenu();
    }

    public void LoadButton()
    {
        if (PhraseButton == null) return;
        PhraseButton.OnPressed += OnButtonPressed;
        UpdateButtonVisibility();
    }

    public void UnloadButton()
    {
        if (PhraseButton == null) return;
        PhraseButton.OnPressed -= OnButtonPressed;
    }

    public void UpdateButtonVisibility()
    {
        if (PhraseButton == null) return;
        var player = _playerManager.LocalSession?.AttachedEntity;
        PhraseButton.Visible = player.HasValue &&
                               _entityManager.HasComponent<PhraseWheelComponent>(player.Value);
    }

    private void OnButtonPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleMenu();
    }

    private void ToggleMenu()
    {
        if (_menu != null)
        {
            CloseMenu();
            return;
        }

        var player = _playerManager.LocalSession?.AttachedEntity;
        if (player == null || !_entityManager.HasComponent<PhraseWheelComponent>(player.Value))
            return;

        var phrases = _prototypeManager.EnumeratePrototypes<PhraseWheelEntryPrototype>();
        var buttons = new List<RadialMenuOption>();

        foreach (var phrase in phrases)
        {
            var captured = phrase;
            var label = string.IsNullOrEmpty(phrase.Label) ? phrase.Text : phrase.Label;
            if (label.Length > 20) label = label[..20] + "...";

            var option = new RadialMenuActionOption<PhraseWheelEntryPrototype>(HandleClick, captured)
            {
                Sprite = captured.Icon,
                ToolTip = phrase.Text,
            };
            buttons.Add(option);
        }

        if (buttons.Count == 0) return;

        _menu = new SimpleRadialMenu();
        _menu.SetButtons(buttons);
        _menu.OnClose += OnMenuClosed;
        _menu.OnOpen += OnMenuOpen;
        _menu.OpenCentered();

        if (PhraseButton != null)
            PhraseButton.SetClickPressed(true);
    }

    private void HandleClick(PhraseWheelEntryPrototype phrase)
    {
        _entityManager.RaisePredictiveEvent(new PlayPhraseWheelMessage { PhraseId = phrase.ID });
        CloseMenu();
    }

    private void OnMenuClosed()
    {
        if (PhraseButton != null) PhraseButton.Pressed = false;
        CloseMenu();
    }

    private void OnMenuOpen()
    {
        if (PhraseButton != null) PhraseButton.Pressed = true;
    }

    private void CloseMenu()
    {
        if (_menu == null) return;
        _menu.OnClose -= OnMenuClosed;
        _menu.OnOpen -= OnMenuOpen;
        _menu.Dispose();
        _menu = null;

        if (PhraseButton != null)
            PhraseButton.SetClickPressed(false);
    }
}
