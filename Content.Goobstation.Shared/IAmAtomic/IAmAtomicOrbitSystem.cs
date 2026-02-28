using Content.Goobstation.Shared.IAmAtomic;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.IAmAtomic;

/// <summary>
/// Двигает орбитальные сферы вокруг владельца каждый кадр.
/// Работает и на клиенте и на сервере.
/// </summary>
public sealed class IAmAtomicOrbitSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateOrbits(frameTime);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        UpdateOrbits(frameTime);
    }

    private void UpdateOrbits(float frameTime)
    {
        var query = EntityQueryEnumerator<IAmAtomicOrbitComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var orbit, out _))
        {
            if (!EntityManager.EntityExists(orbit.Owner))
            {
                QueueDel(uid);
                continue;
            }

            orbit.Angle += orbit.Speed * frameTime;
            if (orbit.Angle > MathF.Tau) orbit.Angle -= MathF.Tau;
            if (orbit.Angle < 0) orbit.Angle += MathF.Tau;

            var ownerPos = _transform.GetWorldPosition(orbit.Owner);

            // Наклонённая орбита — используем VerticalOffset как наклон эллипса
            var x = MathF.Cos(orbit.Angle) * orbit.Radius;
            var y = MathF.Sin(orbit.Angle) * orbit.Radius * orbit.VerticalOffset;

            var newPos = ownerPos + new System.Numerics.Vector2(x, y);
            _transform.SetWorldPositionRotation(uid, newPos, Robust.Shared.Maths.Angle.Zero);
        }
    }
}