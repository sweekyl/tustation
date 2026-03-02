using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.PhraseWheel;

/// <summary>
/// Добавляется игроку когда ему выдали доступ к колесу фраз.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PhraseWheelComponent : Component;
