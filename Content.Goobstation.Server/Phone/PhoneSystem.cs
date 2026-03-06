using Content.Goobstation.Shared.Phone;
using Content.Shared.Audio.Jukebox;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Player; // Для работы с сессиями
using Content.Shared.Ghost; // Если нужно проверять на гностов

namespace Content.Goobstation.Server.Phone;

public sealed class PhoneSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ActorSystem _actor = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhoneComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<PhoneComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<PhoneComponent, PhonePlayMessage>(OnPlay);
        SubscribeLocalEvent<PhoneComponent, PhonePauseMessage>(OnPause);
        SubscribeLocalEvent<PhoneComponent, PhoneStopMessage>(OnStop);
        SubscribeLocalEvent<PhoneComponent, PhoneSelectTrackMessage>(OnSelectTrack);
        SubscribeLocalEvent<PhoneComponent, PhoneSetVolumeMessage>(OnSetVolume);
        SubscribeLocalEvent<PhoneComponent, PhoneSetTimeMessage>(OnSetTime);
    }

    private void OnActivate(Entity<PhoneComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        _ui.OpenUi(ent.Owner, PhoneUiKey.Key, actor.PlayerSession);
    }

    private void OnShutdown(Entity<PhoneComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);
    }

    private void OnPlay(Entity<PhoneComponent> ent, ref PhonePlayMessage args)
    {
        if (Exists(ent.Comp.AudioStream))
        {
            _audio.SetState(ent.Comp.AudioStream, AudioState.Playing);
        }
        else
        {
            ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);

            if (string.IsNullOrEmpty(ent.Comp.SelectedTrackId) ||
                !_proto.TryIndex(ent.Comp.SelectedTrackId, out var trackProto))
                return;

            var volume = SharedAudioSystem.GainToVolume(ent.Comp.Volume);
            ent.Comp.AudioStream = _audio.PlayPvs(
                trackProto.Path,
                ent.Owner,
                AudioParams.Default
                    .WithMaxDistance(ent.Comp.MaxDistance)
                    .WithVolume(volume))?.Entity;
        }
        Dirty(ent);
    }

    private void OnPause(Entity<PhoneComponent> ent, ref PhonePauseMessage args)
    {
        _audio.SetState(ent.Comp.AudioStream, AudioState.Paused);
    }

    private void OnStop(Entity<PhoneComponent> ent, ref PhoneStopMessage args)
    {
        _audio.SetState(ent.Comp.AudioStream, AudioState.Stopped);
        Dirty(ent);
    }

    private void OnSelectTrack(Entity<PhoneComponent> ent, ref PhoneSelectTrackMessage args)
    {
        if (!_audio.IsPlaying(ent.Comp.AudioStream))
        {
            ent.Comp.SelectedTrackId = args.TrackId;
            ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);
        }
        Dirty(ent);
    }

    private void OnSetVolume(Entity<PhoneComponent> ent, ref PhoneSetVolumeMessage args)
    {
        ent.Comp.Volume = Math.Clamp(args.Volume, 0f, 1f);

        // Применяем громкость к текущему стриму если играет
        if (Exists(ent.Comp.AudioStream) &&
            TryComp<AudioComponent>(ent.Comp.AudioStream, out var audio))
        {
            var volume = SharedAudioSystem.GainToVolume(ent.Comp.Volume);
            _audio.SetVolume(ent.Comp.AudioStream, volume);
        }
        Dirty(ent);
    }

    private void OnSetTime(Entity<PhoneComponent> ent, ref PhoneSetTimeMessage args)
    {
        if (TryComp<ActorComponent>(args.Actor, out var actor))
        {
            var offset = actor.PlayerSession.Channel.Ping * 1.5f / 1000f;
            _audio.SetPlaybackPosition(ent.Comp.AudioStream, args.Time + offset);
        }
    }
}
