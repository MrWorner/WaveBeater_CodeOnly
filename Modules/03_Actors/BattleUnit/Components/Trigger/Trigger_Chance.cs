// НАЗНАЧЕНИЕ: Триггер срабатывает со случайной вероятностью.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: UnityEngine.Random.
// ПРИМЕЧАНИЕ: Используется для добавления непредсказуемости в поведение ИИ.
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "T_Chance", menuName = "AI/Triggers/Chance")]
public class Trigger_Chance : ActionTrigger
{
    #region Поля
    [BoxGroup("SETTINGS"), SerializeField, Range(1, 100)] private int _chancePercentage = 50;
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Проверяет, сработал ли триггер на основе случайного шанса.
    /// </summary>
    public override bool IsTriggered(BattleUnit performer)
    {
        bool isTriggered = Random.Range(1, 101) <= _chancePercentage;
        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка случайного шанса (Требуется: <= {0}%). Сработал: <color=yellow>{1}</color>.", false, _chancePercentage, isTriggered);
        return isTriggered;
    }
    #endregion Публичные методы
}