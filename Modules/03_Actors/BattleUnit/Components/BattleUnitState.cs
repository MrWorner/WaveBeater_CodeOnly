using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

/// <summary>
/// Универсальный компонент для хранения уникальных состояний (флагов) для любого BattleUnit.
/// Это замена специфичным MeleeEnemyState, RangedEnemyState и т.д.
/// </summary>
public class BattleUnitState : MonoBehaviour
{
    #region Поля
    [BoxGroup("DEBUG"), ShowInInspector, ReadOnly]
    private HashSet<string> _stateFlags = new HashSet<string>();
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Проверяет, установлен ли у юнита определенный флаг состояния.
    /// </summary>
    public bool HasFlag(string flag)
    {
        return _stateFlags.Contains(flag);
    }

    /// <summary>
    /// Устанавливает флаг состояния для юнита.
    /// </summary>
    public void SetFlag(string flag)
    {
        if (_stateFlags.Add(flag))
        {
            ColoredDebug.CLog(gameObject, "<color=purple>BattleUnitState:</color> Установлен флаг: <color=yellow>{0}</color>.", false, flag);
        }
    }

    /// <summary>
    /// Снимает флаг состояния у юнита.
    /// </summary>
    public void ClearFlag(string flag)
    {
        if (_stateFlags.Remove(flag))
        {
            ColoredDebug.CLog(gameObject, "<color=purple>BattleUnitState:</color> Снят флаг: <color=yellow>{0}</color>.", false, flag);
        }
    }
    #endregion Публичные методы
}
