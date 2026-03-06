// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Movement.Systems;
using Content.Shared.Camera;
using Content.Shared.Hands;
using Content.Shared.Movement.Components;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Goobstation.Client.Aiming;

/// <summary>
/// Клиентская часть — читает EyeCursorOffsetComponent с оружия (добавляется сервером)
/// и двигает камеру за курсором через HeldRelayedEvent.
/// </summary>
public sealed class GunAimingSystem : EntitySystem
{
    [Dependency] private readonly EyeCursorOffsetSystem _eyeOffset = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunComponent, HeldRelayedEvent<GetEyeOffsetRelayedEvent>>(OnGetEyeOffset);
    }

    private void OnGetEyeOffset(Entity<GunComponent> entity, ref HeldRelayedEvent<GetEyeOffsetRelayedEvent> args)
    {
        if (!TryComp<Content.Client.Movement.Components.EyeCursorOffsetComponent>(entity.Owner, out var clientComp))
            return;

        var offset = _eyeOffset.OffsetAfterMouse(entity.Owner, clientComp);
        if (offset == null)
            return;

        args.Args.Offset += offset.Value;
    }
}
