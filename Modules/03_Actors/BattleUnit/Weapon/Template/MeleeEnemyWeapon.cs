// НАЗНАЧЕНИЕ: Представляет собой ScriptableObject-шаблон для оружия ближнего боя.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: WeaponData.
// ПРИМЕЧАНИЕ: Используется для быстрой настройки ассетов оружия в редакторе Unity.

using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WD_MeleeEnemy", menuName = "AI/Weapon Data/Melee Enemy Weapon")]
public class MeleeEnemyWeapon : WeaponData
{
    public MeleeEnemyWeapon()
    {
        _weaponName = "Brute Hammer";
        _attackModes = new List<AttackMode>
        {
            new AttackMode
            {
                modeName = "Hammer Smash",
                damage = 2,
                range = 1,
                hitChance = 0.85f,
                isMelee = true,
                shotsPerAction = 1
            },
            new AttackMode
            {
                modeName = "Hammer Throw",
                damage = 1,
                range = 4,
                hitChance = 0.6f,
                isMelee = false,
                shotsPerAction = 1,
                isDisposable = true
            }
        };
    }
}