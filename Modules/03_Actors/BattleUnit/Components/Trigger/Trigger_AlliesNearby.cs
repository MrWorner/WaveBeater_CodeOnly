// НАЗНАЧЕНИЕ: Триггер срабатывает, если рядом с юнитом находится достаточное количество союзников.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnit, BattleGridUtils.
// ПРИМЕЧАНИЕ: Позволяет создавать командное поведение и синергию между юнитами.
using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "T_AlliesNearby", menuName = "AI/Triggers/Allies Nearby")]
public class Trigger_AlliesNearby : ActionTrigger
{
    #region Поля
    [BoxGroup("SETTINGS"), SerializeField, MinValue(1)] private int _requiredAllies = 1;
    [BoxGroup("SETTINGS"), SerializeField, MinValue(1)] private int _searchRadius = 3;
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Проверяет количество союзников в указанном радиусе.
    /// </summary>
    public override bool IsTriggered(BattleUnit performer)
    {
        if (performer.FactionType != BattleUnit.Faction.Enemy) return false;

        // Находим всех союзников, кроме самого себя, в заданном радиусе
        int alliesFound = BattleUnit.Enemies
            .Count(ally => ally != performer && BattleGridUtils.GetDistance(performer, ally) <= _searchRadius);

        bool isTriggered = alliesFound >= _requiredAllies;
        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка союзников (Нужно: {0}, Найдено: {1} в радиусе {2}). Сработал: <color=yellow>{3}</color>.", false, _requiredAllies, alliesFound, _searchRadius, isTriggered);
        return isTriggered;
    }
    #endregion Публичные методы
}