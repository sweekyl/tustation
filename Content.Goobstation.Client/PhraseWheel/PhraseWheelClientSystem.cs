using Content.Client.UserInterface.Systems.PhraseWheel;
using Content.Goobstation.Shared.PhraseWheel;
using Robust.Client.Player;

namespace Content.Goobstation.Client.PhraseWheel;

public sealed class PhraseWheelClientSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PhraseWheelComponent, ComponentStartup>(OnCompAdded);
        SubscribeLocalEvent<PhraseWheelComponent, ComponentShutdown>(OnCompRemoved);
    }

    private void OnCompAdded(Entity<PhraseWheelComponent> ent, ref ComponentStartup args)
    {
        UpdateVisibility();
    }

    private void OnCompRemoved(Entity<PhraseWheelComponent> ent, ref ComponentShutdown args)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        var controller = IoCManager.Resolve<Robust.Client.UserInterface.IUserInterfaceManager>()
            .GetUIController<PhraseWheelUIController>();
        controller.UpdateButtonVisibility();
    }
}
