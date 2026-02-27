using Content.Goobstation.Shared.IAmAtomic;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Timing;
using Content.Shared.Chat;

namespace Content.Goobstation.Server.IAmAtomic;

public sealed class IAmAtomicSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private static readonly string[] CastLines =
    [
        "atomic-cast-line-1",
        "atomic-cast-line-2",
        "atomic-cast-line-3",
        "atomic-cast-line-4",
        "atomic-cast-line-5",
        "atomic-cast-line-6",
    ];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IAmAtomicComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<IAmAtomicComponent, IAmAtomicDoAfterEvent>(OnDoAfter);
    }

    private void OnUseInHand(Entity<IAmAtomicComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.IsCasting)
            return;

        ent.Comp.IsCasting = true;
        ent.Comp.CastProgress = 0f;
        Dirty(ent);

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.CastTime,
            new IAmAtomicDoAfterEvent(),
            ent,
            used: ent)
        {
            BreakOnMove = false,
            BreakOnDamage = false,
            NeedHand = true,
            BlockDuplicate = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);

        ScheduleCastLines(args.User, ent.Comp.CastTime);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/_DV/CosmicCult/ability_nova_cast.ogg"),
            args.User,
            AudioParams.Default.WithVolume(4f));
    }

    private void ScheduleCastLines(EntityUid user, float totalTime)
    {
        var lineCount = CastLines.Length;
        for (var i = 0; i < lineCount; i++)
        {
            var delay = totalTime * i / lineCount;
            var capturedUser = user;
            var capturedLine = CastLines[i];

            Timer.Spawn(TimeSpan.FromSeconds(delay), () =>
            {
                if (!Deleted(capturedUser))
                    _chat.TrySendInGameICMessage(capturedUser,
                        Loc.GetString(capturedLine),
                        InGameICChatType.Speak,
                        false);
            });
        }
    }

    private void OnDoAfter(Entity<IAmAtomicComponent> ent, ref IAmAtomicDoAfterEvent args)
    {
        ent.Comp.IsCasting = false;
        ent.Comp.CastProgress = 0f;
        Dirty(ent);

        if (args.Cancelled)
            return;

        var user = args.User;

        _chat.TrySendInGameICMessage(user,
            Loc.GetString("atomic-cast-final"),
            InGameICChatType.Speak,
            false);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/_DV/CosmicCult/ability_nova_impact.ogg"),
            ent,
            AudioParams.Default.WithVolume(10f));

        _explosion.QueueExplosion(
            user,
            "Default",
            ent.Comp.ExplosionIntensity,
            1f,
            ent.Comp.ExplosionRadius,
            maxTileBreak: ent.Comp.ExplosionMaxTileBreak);

        QueueDel(ent);
    }
}
