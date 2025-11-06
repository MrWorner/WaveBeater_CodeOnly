// НАЗНАЧЕНИЕ: Отвечает за генерацию случайной, но сбалансированной последовательности этапов для каждого уровня.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: Отсутствуют.
// ПРИМЕЧАНИЕ: Содержит логику для создания вариативности прохождения, заменяя некоторые этапы на альтернативные (Госпиталь, Орда и т.д.).
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class StageSequenceGenerator : MonoBehaviour
{
    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private HashSet<StageType> _usedShopReplacements = new HashSet<StageType>();
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private HashSet<StageType> _usedBattleReplacements = new HashSet<StageType>();
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Генерирует список этапов для указанного уровня.
    /// </summary>
    /// <param name="levelIndex">Индекс текущего уровня (начиная с 0).</param>
    /// <param name="isLastLevel">Является ли этот уровень последним в прогрессии.</param>
    /// <returns>Список сгенерированных типов этапов.</returns>
    public List<StageType> GenerateStages(int levelIndex, bool isLastLevel)
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>StageSequenceGenerator:</color> Генерация этапов для уровня <color=yellow>{0}</color>. Это последний уровень: <color=orange>{1}</color>.", _ColoredDebug, levelIndex + 1, isLastLevel);
        List<StageType> stages;

        if (isLastLevel)
        {
            stages = new List<StageType> { StageType.Shop, StageType.Hospital, StageType.BossFight, StageType.GameOver };
            ColoredDebug.CLog(gameObject, "<color=cyan>StageSequenceGenerator:</color> Сгенерирована финальная последовательность: <color=lime>Shop, Hospital, BossFight, GameOver</color>.", _ColoredDebug);
            return stages;
        }

        int levelPattern = levelIndex % 3;
        switch (levelPattern)
        {
            case 0:
                stages = new List<StageType> { StageType.Battle, StageType.Battle, StageType.Shop, StageType.Battle, StageType.Award };
                break;
            case 1:
                stages = new List<StageType> { StageType.Battle, StageType.Battle, StageType.Shop, StageType.Battle, StageType.Battle, StageType.Shop, StageType.Battle, StageType.Award };
                ReplaceShopStage(stages);
                break;
            case 2:
            default:
                stages = new List<StageType> { StageType.Battle, StageType.Battle, StageType.Shop, StageType.Battle, StageType.Battle, StageType.Shop, StageType.Battle, StageType.Battle, StageType.Award };
                ReplaceShopStage(stages);
                ReplaceBattleStage(stages);
                break;
        }

        ColoredDebug.CLog(gameObject, "<color=cyan>StageSequenceGenerator:</color> Сгенерирована последовательность для уровня <color=yellow>{0}</color> (паттерн {1}). Этапов: <color=lime>{2}</color>.", _ColoredDebug, levelIndex + 1, levelPattern, stages.Count);
        return stages;
    }
    #endregion

    #region Личные методы
    private void ReplaceShopStage(List<StageType> stages)
    {
        List<int> shopIndices = stages.Select((s, i) => s == StageType.Shop ? i : -1).Where(i => i != -1).ToList();
        if (shopIndices.Count == 0) return;

        int shopIndexToReplace = shopIndices[Random.Range(0, shopIndices.Count)];

        List<StageType> shopReplacements = new List<StageType> { StageType.Shop, StageType.Hospital, StageType.Treasure };
        var availableShopReplacements = shopReplacements.Where(type => !_usedShopReplacements.Contains(type)).ToList();
        if (availableShopReplacements.Count == 0)
        {
            _usedShopReplacements.Clear();
            availableShopReplacements = shopReplacements;
            ColoredDebug.CLog(gameObject, "<color=orange>StageSequenceGenerator:</color> Сброшен список использованных замен для магазина.", _ColoredDebug);
        }

        StageType chosenShopType = availableShopReplacements[Random.Range(0, availableShopReplacements.Count)];
        _usedShopReplacements.Add(chosenShopType);
        stages[shopIndexToReplace] = chosenShopType;
        ColoredDebug.CLog(gameObject, "<color=cyan>StageSequenceGenerator:</color> Этап 'Shop' на позиции <color=yellow>{0}</color> заменен на <color=lime>{1}</color>.", _ColoredDebug, shopIndexToReplace, chosenShopType);
    }

    private void ReplaceBattleStage(List<StageType> stages)
    {
        List<int> battleIndices = stages.Select((s, i) => s == StageType.Battle ? i : -1).Where(i => i > 0).ToList();
        if (battleIndices.Count == 0) return;

        int battleIndexToReplace = battleIndices[Random.Range(0, battleIndices.Count)];

        List<StageType> battleReplacements = new List<StageType> { StageType.Horde, StageType.MiniBoss, StageType.HighLevelBattle, StageType.MixedBattle, StageType.DoubleMiniBoss, StageType.TripleMiniBoss };
        var availableBattleReplacements = battleReplacements.Where(type => !_usedBattleReplacements.Contains(type)).ToList();
        if (availableBattleReplacements.Count == 0)
        {
            _usedBattleReplacements.Clear();
            availableBattleReplacements = battleReplacements;
            ColoredDebug.CLog(gameObject, "<color=orange>StageSequenceGenerator:</color> Сброшен список использованных замен для битв.", _ColoredDebug);
        }

        StageType chosenBattleType = availableBattleReplacements[Random.Range(0, availableBattleReplacements.Count)];
        _usedBattleReplacements.Add(chosenBattleType);
        stages[battleIndexToReplace] = chosenBattleType;
        ColoredDebug.CLog(gameObject, "<color=cyan>StageSequenceGenerator:</color> Этап 'Battle' на позиции <color=yellow>{0}</color> заменен на <color=lime>{1}</color>.", _ColoredDebug, battleIndexToReplace, chosenBattleType);
    }
    #endregion
}