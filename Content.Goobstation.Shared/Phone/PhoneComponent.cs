using Content.Shared.Audio.Jukebox;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Phone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class PhoneComponent : Component
{
    /// <summary>Текущий выбранный трек</summary>
    [DataField, AutoNetworkedField]
    public ProtoId<JukeboxPrototype>? SelectedTrackId;

    /// <summary>Текущий аудио стрим</summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AudioStream;

    /// <summary>Громкость 0.0 - 1.0</summary>
    [DataField, AutoNetworkedField]
    public float Volume = 0.5f;

    /// <summary>Максимальная дальность слышимости в тайлах</summary>
    [DataField]
    public float MaxDistance = 12f;
}

[Serializable, NetSerializable]
public enum PhoneUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class PhonePlayMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class PhonePauseMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class PhoneStopMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class PhoneSelectTrackMessage(ProtoId<JukeboxPrototype> trackId) : BoundUserInterfaceMessage
{
    public ProtoId<JukeboxPrototype> TrackId { get; } = trackId;
}

[Serializable, NetSerializable]
public sealed class PhoneSetVolumeMessage(float volume) : BoundUserInterfaceMessage
{
    public float Volume { get; } = volume;
}

[Serializable, NetSerializable]
public sealed class PhoneSetTimeMessage(float time) : BoundUserInterfaceMessage
{
    public float Time { get; } = time;
}
