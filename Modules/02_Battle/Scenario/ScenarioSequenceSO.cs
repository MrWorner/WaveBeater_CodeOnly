// НАЗНАЧЕНИЕ: Определяет последовательность боевых сценариев для тестирования или кастомных матчей.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleScenarioSO.
// ПРИМЕЧАНИЕ: Используется ScenarioManager для управления очередью боев.

using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "ScenarioSequence_New", menuName = "WaveBeater/Scenario/Scenario Sequence")]
public class ScenarioSequenceSO : ScriptableObject
{
    #region Поля
    [BoxGroup("SETTINGS"), Tooltip("Список сценариев, которые будут выполняться по порядку.")]
    [SerializeField] public List<BattleScenarioSO> Scenarios = new List<BattleScenarioSO>();

    [BoxGroup("SETTINGS"), Tooltip("Если true, после завершения последнего сценария последовательность начнется заново.")]
    [SerializeField] public bool LoopSequence = true;
    #endregion Поля
}