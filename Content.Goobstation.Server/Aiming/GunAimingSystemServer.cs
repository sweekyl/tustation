// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Movement.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Goobstation.Server.Aiming;

/// <summary>
/// Серверная часть системы прицеливания.
/// Добавляет EyeCursorOffsetComponent на оружие когда игрок его берёт.
/// Клиент читает этот компонент и двигает камеру за курсором.
/// </summary>
public sealed class GunAimingSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandsComponent, DidEquipHandEvent>(OnDidEquip);
        SubscribeLocalEvent<HandsComponent, DidUnequipHandEvent>(OnDidUnequip);
    }

    private void OnDidEquip(EntityUid uid, HandsComponent component, DidEquipHandEvent args)
    {
        if (!HasComp<GunComponent>(args.Equipped))
            return;

        var comp = EnsureComp<EyeCursorOffsetComponent>(args.Equipped);
        comp.MaxOffset = 4f;
        comp.OffsetSpeed = 0.6f;
        comp.PvsIncrease = 0.4f;
        Dirty(args.Equipped, comp);
    }

    private void OnDidUnequip(EntityUid uid, HandsComponent component, DidUnequipHandEvent args)
    {
        if (!HasComp<GunComponent>(args.Unequipped))
            return;

        // Проверяем нет ли этого оружия ещё в другой руке
        foreach (var handId in component.Hands.Keys)
        {
            if (_hands.TryGetHeldItem((uid, component), handId, out var held) && held == args.Unequipped)
                return;
        }

        RemComp<EyeCursorOffsetComponent>(args.Unequipped);
    }
}
