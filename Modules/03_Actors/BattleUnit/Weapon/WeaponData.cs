// НАЗНАЧЕНИЕ: ScriptableObject, определяющий характеристики оружия, включая различные режимы атаки.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: Отсутствуют.
// ПРИМЕЧАНИЕ: Является контейнером данных для BattleUnitArsenal.

using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[System.Serializable]
public class AttackMode
{
    public string modeName = "Single Shot";
    [Header("Basic Stats")]
    public int damage = 1;
    public int minRange = 0;
    public int range = 5;
    [Range(0f, 1f)]
    public float hitChance = 0.75f;
    public bool isMelee = false;
    [Header("Firing Mode")]
    public int shotsPerAction = 1;
    [Tooltip("Задержка между выстрелами в одной очереди")]
    public float delayBetweenShots = 0.1f;
    [Header("Ammo & Reload")]
    public bool requiresReload = false;
    public int clipSize = 0;
    public int reloadTimeTurns = 1;

    [Header("Special Properties")]
    public bool requiresAim = false;
    public int turnsToAim = 1;
    public float areaOfEffectRadius = 0f;
    public bool isDisposable = false;
}

[CreateAssetMenu(fileName = "New Weapon", menuName = "AI/Weapon Data")]
public class WeaponData : ScriptableObject
{
    #region Поля
    // NOTE: Модификатор доступа изменен на protected, чтобы дочерние классы-шаблоны могли устанавливать значения по умолчанию в конструкторе.
    [BoxGroup("SETTINGS"), SerializeField] protected string _weaponName = "Assault Rifle";
    [BoxGroup("SETTINGS"), SerializeField] protected List<AttackMode> _attackModes = new List<AttackMode>();
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    public string WeaponName { get => _weaponName; }
    public List<AttackMode> AttackModes { get => _attackModes; }
    #endregion Свойства
}