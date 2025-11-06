// НАЗНАЧЕНИЕ: Представляет собой ScriptableObject-шаблон для оружия снайперского типа.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: WeaponData.
// ПРИМЕЧАНИЕ: Используется для быстрой настройки ассетов оружия в редакторе Unity.

using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WD_RangedEnemy", menuName = "AI/Weapon Data/Ranged Enemy Weapon")]
public class RangedEnemyWeapon : WeaponData
{
    public RangedEnemyWeapon()
    {
        _weaponName = "Sniper Rifle";
        _attackModes = new List<AttackMode>
        {
            new AttackMode
            {
                modeName = "Aimed Shot",
                damage = 3,
                range = 7,
                hitChance = 0.9f,
                isMelee = false,
                shotsPerAction = 1,
                requiresReload = true,
                clipSize = 1,
                reloadTimeTurns = 1,
                requiresAim = true,
                turnsToAim = 1
            }
        };
    }
}