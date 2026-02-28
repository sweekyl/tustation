using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.IAmAtomic;

/// <summary>
/// Добавляется во время каста — блокирует движение.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IAmAtomicCastingComponent : Component;

/// <summary>
/// Добавляется во время взрыва — защищает от урона.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IAmAtomicInvulnComponent : Component;