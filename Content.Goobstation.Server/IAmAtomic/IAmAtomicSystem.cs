using Content.Goobstation.Shared.IAmAtomic;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Systems;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.IAmAtomic;

public sealed class IAmAtomicSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    // Тайминги звуков:
    // cast_1: 0с (4.5с)
    // cast_2: 4.5с (12.5с)
    // cast_3: 17с (16с) — режим сияния
    // cast_4: 33с (6с)
    // Взрыв: 39с — строго после последней фразы "Ядерный Взрыв"
    private const float CastDuration = 39f;

    // Фразы привязаны к звукам, последняя "Ядерный Взрыв" за 2с до взрыва
    private static readonly (float time, string phrase)[] Phrases =
    [
        (0.3f,  "Бежать?"),
        (1.2f,  "А кто бежит?"),
        (2.2f,  "и куда?"),
        (3.6f,  "ДА И ЗАЧЕМ?!!!"),
        (5.2f,  "Игры закончились..."),
        (9.5f,  "Так УЗРИТЕ ЖЕ!! Своими собственными глазами"),
        (14.5f, "Мою сокрушительную..."),
        (16.8f, "пронзающую небеса всё поражающую атаку"),
        (22.5f, "Силу... которую не остановить..."),
        (28.5f, "Это... конец всего..."),
        (33.5f, "Я и есть......"),
        // "Ядерный Взрыв" говорит ровно за 2.5с до взрыва чтобы успело прозвучать
        (36.5f, "Ядерный Взрыв"),
    ];

    private readonly Dictionary<EntityUid, List<EntityUid>> _orbits = new();

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

        var user = args.User;
        ent.Comp.IsCasting = true;
        ent.Comp.CastProgress = 0f;
        ent.Comp.CastTime = CastDuration;
        Dirty(ent);

        EnsureComp<IAmAtomicCastingComponent>(user);
        _movement.RefreshMovementSpeedModifiers(user);

        var doAfterArgs = new DoAfterArgs(EntityManager, user, CastDuration,
            new IAmAtomicDoAfterEvent(), ent, used: ent)
        {
            BreakOnMove = false,
            BreakOnDamage = false,
            NeedHand = true,
            BlockDuplicate = true,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);

        SpawnOrbits(user, ent);
        ScheduleAll(user, ent);
    }

    private void SpawnOrbits(EntityUid user, Entity<IAmAtomicComponent> item)
    {
        var coords = Transform(user).Coordinates;
        var orbits = new List<EntityUid>();

        // radius, speed, tilt, startAngle
        var orbitDefs = new (float radius, float speed, float tilt, float startAngle)[]
        {
            (2.0f,  2.2f,  1.0f,  0.0f),
            (2.0f,  2.2f,  1.0f,  MathF.PI),
            (2.5f, -1.6f,  0.35f, MathF.PI * 0.5f),
            (2.5f, -1.6f,  0.35f, MathF.PI * 1.5f),
            (1.6f,  3.8f,  0.6f,  MathF.PI * 0.25f),
            (1.6f,  3.8f,  0.6f,  MathF.PI * 1.25f),
        };

        for (var i = 0; i < orbitDefs.Length; i++)
        {
            var (radius, speed, tilt, startAngle) = orbitDefs[i];
            var orb = Spawn("IAmAtomicOrbEntity", coords);
            var orbitComp = EnsureComp<IAmAtomicOrbitComponent>(orb);
            orbitComp.Owner = user;
            orbitComp.Radius = radius;
            orbitComp.Speed = speed;
            orbitComp.VerticalOffset = tilt;
            orbitComp.Angle = startAngle;
            Dirty(orb, orbitComp);
            orbits.Add(orb);
        }

        _orbits[user] = orbits;
    }

    private void RemoveOrbits(EntityUid user)
    {
        if (!_orbits.TryGetValue(user, out var orbits)) return;
        foreach (var orb in orbits)
            if (!Deleted(orb)) QueueDel(orb);
        _orbits.Remove(user);
    }

    private void ScheduleAll(EntityUid user, Entity<IAmAtomicComponent> item)
    {
        // === ЗВУКИ ===
        try
        {
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Goobstation/IAmAtomic/cast_1.ogg"),
                user, AudioParams.Default.WithVolume(8f).WithMaxDistance(40f));
        }
        catch { }

        Timer.Spawn(4500, () =>
        {
            if (Deleted(user)) return;
            try { _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Goobstation/IAmAtomic/cast_2.ogg"),
                user, AudioParams.Default.WithVolume(8f).WithMaxDistance(40f)); } catch { }
        });

        Timer.Spawn(17000, () =>
        {
            if (Deleted(user)) return;
            try { _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Goobstation/IAmAtomic/cast_3.ogg"),
                user, AudioParams.Default.WithVolume(10f).WithMaxDistance(60f)); } catch { }

            if (!TryComp<IAmAtomicComponent>(item, out var comp)) return;
            comp.IsGlowing = true;
            Dirty(item);
        });

        Timer.Spawn(33000, () =>
        {
            if (Deleted(user)) return;
            try { _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Goobstation/IAmAtomic/cast_4.ogg"),
                user, AudioParams.Default.WithVolume(12f).WithMaxDistance(80f)); } catch { }
        });

        // === ФРАЗЫ ===
        foreach (var (time, phrase) in Phrases)
        {
            var capturedPhrase = phrase;
            var capturedUser = user;
            var capturedItem = item;

            Timer.Spawn((int)(time * 1000), () =>
            {
                if (Deleted(capturedUser)) return;
                if (!TryComp<IAmAtomicComponent>(capturedItem, out var comp) || !comp.IsCasting) return;
                _chat.TrySendInGameICMessage(capturedUser, capturedPhrase, InGameICChatType.Speak, false);
            });
        }

        // === ПРОГРЕСС ===
        for (var i = 1; i <= (int)CastDuration; i++)
        {
            var progress = i / CastDuration;
            var capturedItem = item;

            Timer.Spawn(i * 1000, () =>
            {
                if (Deleted(capturedItem)) return;
                if (!TryComp<IAmAtomicComponent>(capturedItem, out var comp) || !comp.IsCasting) return;
                comp.CastProgress = progress;
                Dirty(capturedItem);
            });
        }
    }

    private void OnDoAfter(Entity<IAmAtomicComponent> ent, ref IAmAtomicDoAfterEvent args)
    {
        ent.Comp.IsCasting = false;
        ent.Comp.IsGlowing = false;
        ent.Comp.CastProgress = 0f;
        Dirty(ent);

        var user = args.User;
        RemComp<IAmAtomicCastingComponent>(user);
        _movement.RefreshMovementSpeedModifiers(user);
        RemoveOrbits(user);

        if (args.Cancelled)
            return;

        var coords = Transform(user).Coordinates;
        Spawn("IAmAtomicFinalVFX", coords);

        try
        {
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/_DV/CosmicCult/ascendant_shatter.ogg"),
                user, AudioParams.Default.WithVolume(16f).WithMaxDistance(200f));
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/_DV/CosmicCult/ability_nova_impact.ogg"),
                user, AudioParams.Default.WithVolume(18f).WithMaxDistance(200f));
        }
        catch { }

        EnsureComp<IAmAtomicInvulnComponent>(user);

        // Волна 1
        _explosion.QueueExplosion(user, "Default",
            ent.Comp.ExplosionTotalIntensity * 0.3f,
            ent.Comp.ExplosionSlope,
            ent.Comp.ExplosionMaxTileIntensity,
            maxTileBreak: 5);

        // Волна 2
        Timer.Spawn(600, () =>
        {
            if (Deleted(user)) return;
            _explosion.QueueExplosion(user, "Default",
                ent.Comp.ExplosionTotalIntensity * 0.6f,
                ent.Comp.ExplosionSlope,
                ent.Comp.ExplosionMaxTileIntensity,
                maxTileBreak: 10);
        });

        // Волна 3 — финальная
        Timer.Spawn(1300, () =>
        {
            if (Deleted(user)) return;
            try
            {
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/_DV/CosmicCult/ability_nova_impact.ogg"),
                    user, AudioParams.Default.WithVolume(20f).WithMaxDistance(300f));
            }
            catch { }

            _explosion.QueueExplosion(user, "Default",
                ent.Comp.ExplosionTotalIntensity,
                ent.Comp.ExplosionSlope,
                ent.Comp.ExplosionMaxTileIntensity,
                maxTileBreak: int.MaxValue,
                canCreateVacuum: true);

            Timer.Spawn(3000, () =>
            {
                if (!Deleted(user))
                    RemComp<IAmAtomicInvulnComponent>(user);
            });
        });

        QueueDel(ent);
    }
}