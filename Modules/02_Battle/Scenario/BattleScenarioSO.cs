// НАЗНАЧЕНИЕ: Определяет один полный боевой сценарий, включая карту и точный состав врагов.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: ArenaTemplateSO, EnemySO.
// ПРИМЕЧАНИЕ: Является строительным блоком для ScenarioSequenceSO.

using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "BattleScenario_New", menuName = "WaveBeater/Scenario/Battle Scenario")]
public class BattleScenarioSO : ScriptableObject
{
    [System.Serializable]
    public class EnemySpawnData
    {
        public EnemySO enemySO;
        public BattleUnit.UnitLevel level;
    }

    #region Поля
    [BoxGroup("SETTINGS"), Tooltip("Шаблон арены, который будет использован в этом сценарии.")]
    [SerializeField] public ArenaTemplateSO arenaTemplate;

    [BoxGroup("SETTINGS"), Tooltip("Точный список врагов, которые будут заспавнены в этом сценарии.")]
    [SerializeField] public List<EnemySpawnData> enemiesToSpawn = new List<EnemySpawnData>();
    #endregion Поля
}