using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.IAmAtomic;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IAmAtomicComponent : Component
{
    [DataField] public float CastTime = 39f;
    [DataField] public float VisualRadius = 14f;

    // totalIntensity=200000 slope=5 maxTileIntensity=100 => огромный взрыв
    [DataField] public float ExplosionTotalIntensity = 200000f;
    [DataField] public float ExplosionSlope = 5f;
    [DataField] public float ExplosionMaxTileIntensity = 100f;

    [DataField, AutoNetworkedField] public bool IsCasting = false;
    [DataField, AutoNetworkedField] public float CastProgress = 0f;

    /// <summary>
    /// Включается на 17й секунде — cast_3, режим СИЯНИЯ
    /// </summary>
    [DataField, AutoNetworkedField] public bool IsGlowing = false;
}