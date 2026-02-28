using Content.Goobstation.Shared.IAmAtomic;
using Content.Shared.Damage;

namespace Content.Goobstation.Server.IAmAtomic;

public sealed class IAmAtomicInvulnSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IAmAtomicInvulnComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
    }

    private void OnBeforeDamage(Entity<IAmAtomicInvulnComponent> ent, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
    }
}