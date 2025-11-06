// НАЗНАЧЕНИЕ: Управляет оружием и режимами атаки боевой единицы. Отвечает за боезапас, перезарядку и прицеливание.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: WeaponData, AttackMode.
// ПРИМЕЧАНИЕ: Является центральным компонентом для всей логики, связанной с использованием оружия.

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
public class BattleUnitArsenal : MonoBehaviour
{
    #region Поля
    [BoxGroup("SETTINGS"), SerializeField, Tooltip("Начальное оружие, с которым появляется юнит.")]
    private List<WeaponData> _initialWeapons = new List<WeaponData>();
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private List<WeaponData> _weapons = new List<WeaponData>();
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private Dictionary<AttackMode, int> _currentAmmo = new Dictionary<AttackMode, int>();
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private Dictionary<AttackMode, int> _reloadTurnsLeft = new Dictionary<AttackMode, int>();
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private Dictionary<AttackMode, int> _aimTurnsLeft = new Dictionary<AttackMode, int>();
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private List<AttackMode> _usedDisposableModes = new List<AttackMode>();
    #endregion Поля

    #region Свойства
    public IReadOnlyList<WeaponData> Weapons => _weapons;

    /// <summary>
    /// Возвращает список исходного оружия, назначенного в префабе. Используется для экспорта данных.
    /// </summary>
    public IReadOnlyList<WeaponData> InitialWeapons => _initialWeapons;

    public Dictionary<AttackMode, int> CurrentAmmo { get => _currentAmmo; }
    public Dictionary<AttackMode, int> ReloadTurnsLeft
    {
        get => _reloadTurnsLeft;
    }
    public Dictionary<AttackMode, int> AimTurnsLeft
    {
        get => _aimTurnsLeft;
    }
    public List<AttackMode> UsedDisposableModes
    {
        get => _usedDisposableModes;
    }

    /// <summary>
    /// Возвращает единый список всех режимов атаки со всего имеющегося оружия.
    /// </summary>
    public IEnumerable<AttackMode> GetAllAttackModes()
    {
        return _weapons.SelectMany(weapon => weapon.AttackModes);
    }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        foreach (var weaponData in _initialWeapons)
        {
            AddWeapon(Instantiate(weaponData));
        }
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Добавляет новое оружие в арсенал юнита во время игры.
    /// </summary>
    /// <param name="weapon">Данные оружия для добавления.</param>
    public void AddWeapon(WeaponData weapon)
    {
        if (weapon == null || _weapons.Contains(weapon)) return;
        _weapons.Add(weapon);
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitArsenal:</color> Добавлено оружие <color=yellow>{0}</color>.", _ColoredDebug, weapon.name);

        foreach (var mode in weapon.AttackModes)
        {
            if (mode.requiresReload)
            {
                _currentAmmo[mode] = mode.clipSize;
                ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitArsenal:</color> Режим <color=yellow>{0}</color> инициализирован с патронами: <color=lime>{1}</color>.", _ColoredDebug, mode.modeName, mode.clipSize);
            }
        }
    }

    /// <summary>
    /// Убирает оружие из арсенала юнита во время игры.
    /// </summary>
    /// <param name="weapon">Данные оружия для удаления.</param>
    public void RemoveWeapon(WeaponData weapon)
    {
        if (weapon != null && _weapons.Remove(weapon))
        {
            ColoredDebug.CLog(gameObject, "<color=orange>BattleUnitArsenal:</color> Удалено оружие <color=yellow>{0}</color>.", _ColoredDebug, weapon.name);
        }
    }

    /// <summary>
    /// Обновляет таймеры перезарядки и прицеливания в начале хода.
    /// </summary>
    public void TickTurn()
    {
        var keys = _reloadTurnsLeft.Keys.ToList();
        foreach (var key in keys)
        {
            _reloadTurnsLeft[key]--;
            ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitArsenal:</color> Режим <color=yellow>{0}</color>. До перезарядки: <color=yellow>{1}</color>.", _ColoredDebug, key.modeName, _reloadTurnsLeft[key]);
            if (_reloadTurnsLeft[key] <= 0)
            {
                _reloadTurnsLeft.Remove(key);
                _currentAmmo[key] = key.clipSize;
                ColoredDebug.CLog(gameObject, "<color=lime>BattleUnitArsenal:</color> Перезарядка <color=yellow>{0}</color> завершена. Патронов: <color=lime>{1}</color>.", _ColoredDebug, key.modeName, key.clipSize);
            }
        }

        keys = _aimTurnsLeft.Keys.ToList();
        foreach (var key in keys)
        {
            _aimTurnsLeft[key]--;
            ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitArsenal:</color> Режим <color=yellow>{0}</color>. До прицеливания: <color=yellow>{1}</color>.", _ColoredDebug, key.modeName, _aimTurnsLeft[key]);
            if (_aimTurnsLeft[key] <= 0)
            {
                _aimTurnsLeft.Remove(key);
                ColoredDebug.CLog(gameObject, "<color=lime>BattleUnitArsenal:</color> Прицеливание для <color=yellow>{0}</color> завершено.", _ColoredDebug, key.modeName);
            }
        }
    }

    /// <summary>
    /// Проверяет, можно ли использовать данный режим атаки.
    /// </summary>
    /// <param name="mode">Режим для проверки.</param>
    /// <returns>True, если режим можно использовать.</returns>
    public bool CanUseMode(AttackMode mode)
    {
        if (_usedDisposableModes.Contains(mode))
        {
            ColoredDebug.CLog(gameObject, "<color=red>BattleUnitArsenal:</color> Режим <color=yellow>{0}</color> нельзя использовать: одноразовый и уже использован.", _ColoredDebug, mode.modeName);
            return false;
        }
        if (_reloadTurnsLeft.ContainsKey(mode))
        {
            ColoredDebug.CLog(gameObject, "<color=red>BattleUnitArsenal:</color> Режим <color=yellow>{0}</color> нельзя использовать: идет перезарядка.", _ColoredDebug, mode.modeName);
            return false;
        }
        if (mode.requiresReload && _currentAmmo.ContainsKey(mode) && _currentAmmo[mode] <= 0)
        {
            ColoredDebug.CLog(gameObject, "<color=red>BattleUnitArsenal:</color> Режим <color=yellow>{0}</color> нельзя использовать: нет патронов.", _ColoredDebug, mode.modeName);
            return false;
        }
        if (mode.requiresAim && !_aimTurnsLeft.ContainsKey(mode))
        {
            ColoredDebug.CLog(gameObject, "<color=red>BattleUnitArsenal:</color> Режим <color=yellow>{0}</color> нельзя использовать: требует прицеливания, которое не завершено.", _ColoredDebug, mode.modeName);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Регистрирует использование режима атаки, тратит патроны и запускает перезарядку.
    /// </summary>
    /// <param name="mode">Использованный режим атаки.</param>
    public void UseMode(AttackMode mode)
    {
        if (!CanUseMode(mode)) return;
        if (mode.isDisposable)
        {
            _usedDisposableModes.Add(mode);
            ColoredDebug.CLog(gameObject, "<color=yellow>BattleUnitArsenal:</color> Режим <color=yellow>{0}</color> использован как <color=red>одноразовый</color>.", _ColoredDebug, mode.modeName);
        }

        if (mode.requiresReload)
        {
            _currentAmmo[mode]--;
            ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitArsenal:</color> Режим <color=yellow>{0}</color> использован. Патронов: <color=lime>{1}</color>.", _ColoredDebug, mode.modeName, _currentAmmo[mode]);
            if (_currentAmmo[mode] <= 0 && !_reloadTurnsLeft.ContainsKey(mode))
            {
                _reloadTurnsLeft[mode] = mode.reloadTimeTurns;
                ColoredDebug.CLog(gameObject, "<color=yellow>BattleUnitArsenal:</color> Начата перезарядка <color=yellow>{0}</color> на <color=yellow>{1}</color> ход(а).", _ColoredDebug, mode.modeName, mode.reloadTimeTurns);
            }
        }
    }

    /// <summary>
    /// Начинает процесс прицеливания для указанного режима.
    /// </summary>
    /// <param name="mode">Режим, для которого начинается прицеливание.</param>
    public void StartAiming(AttackMode mode)
    {
        if (mode.requiresAim)
        {
            _aimTurnsLeft[mode] = mode.turnsToAim;
            ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitArsenal:</color> Начато прицеливание для <color=yellow>{0}</color> на <color=yellow>{1}</color> ход(а).", _ColoredDebug, mode.modeName, mode.turnsToAim);
        }
    }
    #endregion Публичные методы
}