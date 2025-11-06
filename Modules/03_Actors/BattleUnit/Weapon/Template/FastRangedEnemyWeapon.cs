// НАЗНАЧЕНИЕ: Представляет собой ScriptableObject-шаблон для скорострельного оружия.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: WeaponData.
// ПРИМЕЧАНИЕ: Используется для быстрой настройки ассетов оружия в редакторе Unity.

using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WD_FastRangedEnemy", menuName = "AI/Weapon Data/Fast Ranged Enemy Weapon")]
public class FastRangedEnemyWeapon : WeaponData
{
    public FastRangedEnemyWeapon()
    {
        _weaponName = "Assault Carbine";
        _attackModes = new List<AttackMode>
        {
            new AttackMode
            {
                modeName = "Rapid Shot",
                damage = 1,
                range = 5,
                hitChance = 0.7f,
                isMelee = false,
                shotsPerAction = 1,
                requiresReload = true,
                clipSize = 4,
                reloadTimeTurns = 1
            },
            new AttackMode
            {
                modeName = "Bayonet Stab",
                damage = 1,
                range = 1,
                hitChance = 0.8f,
                isMelee = true,
                shotsPerAction = 1
            }
        };
    }
}