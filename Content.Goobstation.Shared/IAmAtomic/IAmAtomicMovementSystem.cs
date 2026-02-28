using Content.Shared.Movement.Systems;

namespace Content.Goobstation.Shared.IAmAtomic;

public sealed class IAmAtomicMovementSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IAmAtomicCastingComponent, RefreshMovementSpeedModifiersEvent>(OnMovement);
    }

    private void OnMovement(Entity<IAmAtomicCastingComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(0f, 0f);
    }
}