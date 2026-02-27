using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.IAmAtomic;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IAmAtomicComponent : Component
{
    [DataField]
    public float CastTime = 8f; // секунд каст

    [DataField]
    public float ExplosionRadius = 12f;

    [DataField]
    public float ExplosionIntensity = 200f;

    [DataField]
    public int ExplosionMaxTileBreak = 3;

    /// <summary>
    /// Передаётся клиенту для отображения круга радиуса
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsCasting = false;

    [DataField, AutoNetworkedField]
    public float CastProgress = 0f; // 0.0 - 1.0
}
