using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Damage;
using Content.Shared.Movement.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Sprinting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SprinterComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public bool IsSprinting = false;

    [DataField, AutoNetworkedField]
    public bool ScaleWithStamina = true;

    [DataField, AutoNetworkedField]
    public bool CanSprint = true;

    [DataField, AutoNetworkedField]
    public float StaminaDrainRate = 15f;

    [DataField, AutoNetworkedField]
    public float StaminaRegenMultiplier = 0.75f;

    [DataField, AutoNetworkedField]
    public float StaminaDrainMultiplier = 1.4f;

    [DataField, AutoNetworkedField]
    public float SprintSpeedMultiplier = 1.45f;

    [DataField, AutoNetworkedField]
    public TimeSpan TimeBetweenSprints = TimeSpan.FromSeconds(3);

    [ViewVariables, AutoNetworkedField]
    public TimeSpan LastSprint = TimeSpan.Zero;

    [DataField]
    public string StaminaDrainKey = "sprint";

    [DataField]
    public EntProtoId SprintAnimation = "SprintAnimation";

    [ViewVariables]
    public TimeSpan LastStep = TimeSpan.Zero;

    [DataField]
    public EntProtoId StepAnimation = "SmallSprintAnimation";

    [DataField]
    public SoundSpecifier SprintStartupSound = new SoundPathSpecifier("/Audio/_Goobstation/Effects/Sprinting/sprint_puff.ogg");

    [DataField, AutoNetworkedField]
    public TimeSpan TimeBetweenSteps = TimeSpan.FromSeconds(0.6);

    [DataField]
    public DamageSpecifier SprintDamageSpecifier = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Blunt", 10 },
        }
    };

    [DataField]
    public TimeSpan KnockdownDurationOnInterrupt = TimeSpan.FromSeconds(2f);

    [DataField]
    public float StaminaPenaltyOnShove = 25f;

    // === Одышка ===

    /// <summary>
    /// Порог стамины (0-1) при котором начинается лёгкая одышка.
    /// 0.6 = когда осталось 60% стамины (потрачено 40%)
    /// </summary>
    [DataField]
    public float BreathingLightThreshold = 0.6f;

    /// <summary>
    /// Порог стамины при котором начинается тяжёлая одышка.
    /// </summary>
    [DataField]
    public float BreathingHeavyThreshold = 0.3f;

    /// <summary>
    /// Звук лёгкой одышки — редкие вздохи
    /// </summary>
    [DataField]
    public SoundSpecifier? BreathingLightSound = new SoundPathSpecifier("/Audio/_Goobstation/Effects/Sprinting/breath_light.ogg");

    /// <summary>
    /// Звук тяжёлой одышки — частое дыхание
    /// </summary>
    [DataField]
    public SoundSpecifier? BreathingHeavySound = new SoundPathSpecifier("/Audio/_Goobstation/Effects/Sprinting/breath_heavy.ogg");

    /// <summary>
    /// Интервал между звуками лёгкой одышки
    /// </summary>
    [DataField]
    public TimeSpan BreathingLightInterval = TimeSpan.FromSeconds(3.5f);

    /// <summary>
    /// Интервал между звуками тяжёлой одышки
    /// </summary>
    [DataField]
    public TimeSpan BreathingHeavyInterval = TimeSpan.FromSeconds(1.8f);

    /// <summary>
    /// Когда последний раз играл звук одышки
    /// </summary>
    [ViewVariables]
    public TimeSpan LastBreathSound = TimeSpan.Zero;

    /// <summary>
    /// Текущий уровень одышки для отслеживания изменений
    /// </summary>
    [ViewVariables]
    public BreathingLevel CurrentBreathingLevel = BreathingLevel.None;
}

public enum BreathingLevel : byte
{
    None,
    Light,
    Heavy,
}

[Serializable, NetSerializable]
public sealed class SprintToggleEvent(bool isSprinting) : EntityEventArgs
{
    public bool IsSprinting = isSprinting;
}

[Serializable, NetSerializable]
public sealed class SprintStartEvent : EntityEventArgs;

[ByRefEvent]
public sealed class SprintAttemptEvent : CancellableEntityEventArgs;
