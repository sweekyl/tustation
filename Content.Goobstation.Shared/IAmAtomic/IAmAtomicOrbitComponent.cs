using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.IAmAtomic;

/// <summary>
/// Компонент орбитальной сферы — летает вокруг владельца по кругу.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IAmAtomicOrbitComponent : Component
{
    [DataField, AutoNetworkedField] public EntityUid Owner = EntityUid.Invalid;
    [DataField, AutoNetworkedField] public float Angle = 0f;        // текущий угол в радианах
    [DataField, AutoNetworkedField] public float Radius = 2f;       // радиус орбиты
    [DataField, AutoNetworkedField] public float Speed = 2f;        // радиан/сек
    [DataField, AutoNetworkedField] public float VerticalOffset = 0f;
}