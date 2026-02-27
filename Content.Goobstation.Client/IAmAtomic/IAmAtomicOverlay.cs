using Content.Goobstation.Shared.IAmAtomic;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Goobstation.Client.IAmAtomic;

public sealed class IAmAtomicOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly IPlayerManager _playerManager;
    private readonly IGameTiming _timing;
    private readonly TransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public IAmAtomicOverlay(IEntityManager entManager, IPlayerManager playerManager, IGameTiming timing)
    {
        _entManager = entManager;
        _playerManager = playerManager;
        _timing = timing;
        _transform = _entManager.System<TransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var query = _entManager.EntityQueryEnumerator<IAmAtomicComponent, TransformComponent>();

        while (query.MoveNext(out _, out var comp, out var xform))
        {
            if (!comp.IsCasting)
                continue;

            if (xform.MapID != args.MapId)
                continue;

            var worldPos = _transform.GetWorldPosition(xform);
            var pulse = (float) (Math.Sin(_timing.RealTime.TotalSeconds * 4) * 0.2 + 0.6);
            var progress = comp.CastProgress;

            DrawCircleFilled(handle, worldPos, comp.ExplosionRadius, new Color(0.6f, 0.0f, 1.0f, pulse * 0.15f));
            DrawCircleBorder(handle, worldPos, comp.ExplosionRadius, new Color(0.8f, 0.2f, 1.0f, pulse));

            if (progress > 0f)
            {
                var innerRadius = comp.ExplosionRadius * progress;
                DrawCircleBorder(handle, worldPos, innerRadius, new Color(1.0f, 0.5f, 1.0f, 0.8f));
            }
        }

        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawCircleFilled(DrawingHandleWorld handle, Vector2 center, float radius, Color color)
    {
        const int segments = 64;
        var prev = center + new Vector2(radius, 0);
        for (var i = 1; i <= segments; i++)
        {
            var angle = i / (float) segments * MathF.Tau;
            var curr = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            handle.DrawLine(prev, curr, color);
            prev = curr;
        }
    }

    private void DrawCircleBorder(DrawingHandleWorld handle, Vector2 center, float radius, Color color)
    {
        const int segments = 64;
        for (var i = 0; i < segments; i++)
        {
            var angle1 = i / (float) segments * MathF.Tau;
            var angle2 = (i + 1) / (float) segments * MathF.Tau;
            var p1 = center + new Vector2(MathF.Cos(angle1), MathF.Sin(angle1)) * radius;
            var p2 = center + new Vector2(MathF.Cos(angle2), MathF.Sin(angle2)) * radius;
            handle.DrawLine(p1, p2, color);
        }
    }
}
