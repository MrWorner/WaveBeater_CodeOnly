// НАЗНАЧЕНИЕ: Представляет собой ScriptableObject-шаблон.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: WeaponData.
// ПРИМЕЧАНИЕ: Используется для быстрой настройки ассетов оружия в редакторе Unity.

using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "StandardHeroAssaultRifle", menuName = "AI/Weapon Data/Standard Hero Assault Rifle")]
public class StandardHeroAssaultRifle : WeaponData
{
    public StandardHeroAssaultRifle()
    {
        _weaponName = "Standard Hero Assault Rifle";
        _attackModes = new List<AttackMode>
        {
            new AttackMode
            {
                modeName = "Fast Shot",
                damage = 1,
                range = 99,
                hitChance = 1f,
                isMelee = false,
                shotsPerAction = 1,
                requiresReload = true,
                clipSize = 10,
                reloadTimeTurns = 1,
                requiresAim = false,
                turnsToAim = 0
            }
        };
    }
}