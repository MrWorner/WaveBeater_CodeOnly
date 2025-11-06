using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Linq;
using Sirenix.OdinInspector;
using System.Collections.Generic;

public class AIBrain : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnit _unit;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private AIProfile _profile;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _actionPointsLeft;
    private Dictionary<AIAction, int> _actionUsesThisTurn = new Dictionary<AIAction, int>();
    #endregion Поля

    #region Свойства
    public BattleUnit Unit { get => _unit; }
    public AIProfile Profile { get => _profile; }
    public int ActionPointsLeft { get => _actionPointsLeft; }
    #endregion Свойства

    private void Awake()
    {
        if (_unit == null) DebugUtils.LogMissingReference(this, nameof(_unit));
        if (_profile == null) DebugUtils.LogMissingReference(this, nameof(_profile));
    }

    public void ExecuteTurn(UnityAction onTurnCompleted)
    {
        ColoredDebug.CLog(gameObject, "<color=magenta>AIBrain:</color> Начинаю ход.", _ColoredDebug);
        StartCoroutine(TurnRoutine(onTurnCompleted));
    }

    private IEnumerator TurnRoutine(UnityAction onTurnCompleted)
    {
        if (_profile == null)
        {
            ColoredDebug.CLog(gameObject, "<color=red>AIBrain:</color> Отсутствует AI Profile. Ход завершен.", _ColoredDebug);
            onTurnCompleted?.Invoke();
            yield break;
        }

        _actionUsesThisTurn.Clear();
        _actionPointsLeft = _unit.Stats.MaxActionPoints;
        _unit.Arsenal.TickTurn();
        ColoredDebug.CLog(gameObject, "<color=magenta>AIBrain:</color> Начинаю цикл действий. Доступно ОД: <color=yellow>{0}</color>.", _ColoredDebug, _actionPointsLeft);
        while (_actionPointsLeft > 0 && !_unit.IsBusy)
        {
            if (_profile.behaviorPattern == null)
            {
                ColoredDebug.CLog(gameObject, "<color=red>AIBrain:</color> Отсутствует Behavior Pattern. Ход прерван.", _ColoredDebug, _profile.name);
                break;
            }

            var executableActions = _profile.availableActions
                .Where(action =>
                {
                    _actionUsesThisTurn.TryGetValue(action, out int uses);
                    bool canUse = (action.maxUsesPerTurn == 0 || uses < action.maxUsesPerTurn) &&
                                  (_actionPointsLeft >= action.actionPointCost) &&
                                  action.CanExecute(_unit, _actionPointsLeft);
                    return canUse;
                })
                .ToList();
            if (executableActions.Count == 0)
            {
                ColoredDebug.CLog(gameObject, "<color=yellow>AIBrain:</color> Нет доступных действий. Завершаю ход.", _ColoredDebug);
                break;
            }

            AIAction chosenAction = _profile.behaviorPattern.DecideAction(_unit, executableActions);
            if (chosenAction != null)
            {
                ColoredDebug.CLog(gameObject, "<color=lime>AIBrain:</color> Выбрано действие: <color=lime>{0}</color> (стоимость: {1})", _ColoredDebug, chosenAction.actionName, chosenAction.actionPointCost);
                bool actionIsComplete = false;
                chosenAction.Execute(_unit, () => { actionIsComplete = true; });

                _actionPointsLeft -= chosenAction.actionPointCost;
                _actionUsesThisTurn.TryGetValue(chosenAction, out int uses);
                _actionUsesThisTurn[chosenAction] = uses + 1;

                ColoredDebug.CLog(gameObject, "<color=lime>AIBrain:</color> Действие выполнено. Осталось ОД: <color=yellow>{0}</color>.", _ColoredDebug, _actionPointsLeft);
                yield return new WaitUntil(() => actionIsComplete || !_unit.IsAlive);

                // Гарантируем, что корутина уступит управление хотя бы на один кадр,
                // чтобы избежать бесконечного цикла при мгновенном выполнении действий.
                yield return null;
            }
            else
            {
                ColoredDebug.CLog(gameObject, "<color=yellow>AIBrain:</color> Behavior Pattern не выбрал действие. Завершаю ход.", _ColoredDebug);
                break;
            }
        }

        ColoredDebug.CLog(gameObject, "<color=magenta>AIBrain:</color> Ход завершен.", _ColoredDebug);
        onTurnCompleted?.Invoke();
    }

    public void ConsumeActionPoints(int cost)
    {
        _actionPointsLeft -= cost;
        _actionPointsLeft = Mathf.Max(0, _actionPointsLeft); // Убедимся, что не ушли в минус
    }

    public void RecordActionUse(AIAction action)
    {
        _actionUsesThisTurn.TryGetValue(action, out int uses);
        _actionUsesThisTurn[action] = uses + 1;
    }

    public bool CanUseAction(AIAction action)
    {
        if (action == null) return false;
        _actionUsesThisTurn.TryGetValue(action, out int uses);
        bool canUse = (action.maxUsesPerTurn == 0 || uses < action.maxUsesPerTurn) &&
                        (_actionPointsLeft >= action.actionPointCost) &&
                        action.CanExecute(_unit, _actionPointsLeft);
        return canUse;
    }

    public void ResetTurnState()
    {
        _actionUsesThisTurn.Clear();
        _actionPointsLeft = _unit.Stats.MaxActionPoints;
        ColoredDebug.CLog(gameObject, "<color=magenta>AIBrain:</color> Состояние хода сброшено. Доступно ОД: <color=yellow>{0}</color>.", _ColoredDebug, _actionPointsLeft);
    }
}