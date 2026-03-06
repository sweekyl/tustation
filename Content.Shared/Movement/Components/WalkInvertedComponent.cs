using Robust.Shared.GameObjects;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Маркер - сущность использует систему спринта с инверсией ходьбы.
/// Без Shift = медленная ходьба, Shift = бег со стаминой.
/// </summary>
[RegisterComponent]
public sealed partial class WalkInvertedComponent : Component;
