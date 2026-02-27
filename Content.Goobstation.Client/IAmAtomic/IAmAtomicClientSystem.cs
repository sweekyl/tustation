using Content.Goobstation.Shared.IAmAtomic;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Goobstation.Client.IAmAtomic;

public sealed class IAmAtomicClientSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new IAmAtomicOverlay(EntityManager, _playerManager, _timing));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<IAmAtomicOverlay>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<IAmAtomicComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            if (!comp.IsCasting)
                continue;

            comp.CastProgress = Math.Min(comp.CastProgress + frameTime / comp.CastTime, 1f);
        }
    }
}
