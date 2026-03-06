using Content.Goobstation.Shared.Phone;
using Content.Shared.Audio.Jukebox;
using Robust.Client.Audio;
using Robust.Client.UserInterface;
using Robust.Shared.Audio.Components;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Client.Phone;

public sealed class PhoneBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private PhoneMenu? _menu;

    public PhoneBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<PhoneMenu>();

        _menu.OnPlayPressed += playing =>
            SendMessage(playing ? new PhonePlayMessage() : new PhonePauseMessage());

        _menu.OnStopPressed += () =>
            SendMessage(new PhoneStopMessage());

        _menu.OnTrackSelected += trackId =>
            SendMessage(new PhoneSelectTrackMessage(trackId));

        _menu.OnSetTime += time =>
            SendMessage(new PhoneSetTimeMessage(time));

        _menu.OnSetVolume += volume =>
            SendMessage(new PhoneSetVolumeMessage(volume));

        PopulateTracks();
        Reload();
    }

    public void Reload()
    {
        if (_menu == null || !EntMan.TryGetComponent(Owner, out PhoneComponent? phone))
            return;

        _menu.SetAudioStream(phone.AudioStream);
        _menu.SetVolume(phone.Volume);

        if (_proto.TryIndex(phone.SelectedTrackId, out var trackProto))
        {
            var length = EntMan.System<AudioSystem>().GetAudioLength(trackProto.Path.Path.ToString());
            _menu.SetSelectedTrack(trackProto.Name, (float)length.TotalSeconds);
        }
        else
        {
            _menu.SetSelectedTrack(string.Empty, 0f);
        }
    }

    public void PopulateTracks()
    {
        _menu?.Populate(_proto.EnumeratePrototypes<JukeboxPrototype>());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        Reload();
    }
}
