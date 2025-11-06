// НАЗНАЧЕНИЕ: Отвечает за фактическую сборку состава волны на основе бюджета угрозы, типа этапа и данных уровня.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: LevelData, EnemySO.
// ПРИМЕЧАНИЕ: Содержит всю сложную логику и эвристики по подбору врагов для разных сценариев (Орда, Мини-босс и т.д.).
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System;

public class WaveAssembler : MonoBehaviour
{
    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private int _armyLimit = 3;
    [BoxGroup("SETTINGS"), SerializeField] private float _extraSlotPurchaseCost = 1.0f;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;

    private HashSet<string> _previousWaveSignatures = new HashSet<string>();
    #endregion

    #region Публичные методы
    /// <summary>
    /// Основная корутина, которая собирает волну врагов.
    /// </summary>
    public IEnumerator AssembleWave(float threatBudget, StageType stageType, LevelData levelData, Action<(List<(EnemySO so, EnemySO.EnemyVariant variant)> wave, string waveType)> onComplete)
    {
        var reportBuilder = new StringBuilder(); // Для будущих логов, если понадобится
        List<(EnemySO, EnemySO.EnemyVariant)> wave = null;
        string waveType = null;
        Action<(List<(EnemySO, EnemySO.EnemyVariant)>, string)> waveAssemblyCallback = (result) => { wave = result.Item1; waveType = result.Item2; };

        switch (stageType)
        {
            case StageType.MiniBoss: yield return StartCoroutine(AssembleMiniBossWave(reportBuilder, waveAssemblyCallback, threatBudget, levelData)); break;
            // ... добавьте другие типы волн по аналогии ...
            default: yield return StartCoroutine(AssembleStandardBattleWave(reportBuilder, waveAssemblyCallback, threatBudget, levelData)); break;
        }

        // Гарантируем, что волна не пустая
        if (wave == null || wave.Count == 0)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>WaveAssembler:</color> Не удалось собрать волну. Принудительно добавляю одного самого дешевого врага.", _ColoredDebug);
            var cheapestEnemy = GetChoicesForLevel(levelData, false).OrderBy(e => e.variant.threat).FirstOrDefault();
            if (cheapestEnemy.so != null)
            {
                wave.Add(cheapestEnemy);
                waveType += " (Fallback)";
            }
            else
            {
                ColoredDebug.CLog(gameObject, "<color=red>WaveAssembler:</color> КРИТИЧЕСКАЯ ОШИБКА! Нет доступных врагов для спавна даже в fallback-режиме.", _ColoredDebug);
            }
        }

        // Проверка на уникальность волны
        string waveSignature = GenerateWaveSignature(wave);
        if (_previousWaveSignatures.Contains(waveSignature))
        {
            var cheapestEnemy = GetChoicesForLevel(levelData, false).OrderBy(e => e.variant.threat).FirstOrDefault();
            if (cheapestEnemy.so != null)
            {
                wave.Add(cheapestEnemy);
                waveType += " (Unique)";
            }
        }
        _previousWaveSignatures.Add(waveSignature);

        onComplete?.Invoke((wave, waveType));
    }
    #endregion

    #region Методы сборки волн
    private IEnumerator AssembleStandardBattleWave(StringBuilder reportBuilder, Action<(List<(EnemySO, EnemySO.EnemyVariant)>, string)> onComplete, float threatBudget, LevelData levelData)
    {
        var wave = new List<(EnemySO so, EnemySO.EnemyVariant variant)>();
        float pointsLeft = threatBudget;
        var availableEnemies = GetChoicesForLevel(levelData, false);
        var minEnemyLevel = levelData.minEnemyLevel;

        ColoredDebug.CLog(gameObject, "<color=#FFE4B5>AssembleWave:</color> Бюджет: <color=yellow>{0:F1}</color>. Доступно вариантов: <color=yellow>{1}</color>.", _ColoredDebug, pointsLeft, availableEnemies.Count);

        int iterations = 0;
        while (iterations < 100)
        {
            iterations++;
            if (wave.Count >= _armyLimit)
            {
                int slotMultiplier = wave.Count / _armyLimit;
                float slotCost = _extraSlotPurchaseCost * slotMultiplier + (1 * (int)minEnemyLevel);
                if (pointsLeft < slotCost)
                {
                    ColoredDebug.CLog(gameObject, "<color=#FFE4B5>AssembleWave:</color> Недостаточно бюджета для доп. слота (<color=red>{0:F1}</color>).", _ColoredDebug, slotCost);
                    break;
                }
                pointsLeft -= slotCost;
            }

            var affordableBaseEnemies = availableEnemies.Where(e => e.variant.level == minEnemyLevel && e.variant.threat <= pointsLeft).ToList();
            if (!affordableBaseEnemies.Any())
            {
                ColoredDebug.CLog(gameObject, "<color=#FFE4B5>AssembleWave:</color> Не найдено доступных врагов в рамках бюджета.", _ColoredDebug);
                break;
            }

            const int maxSameType = 3;
            var enemyGroups = wave.GroupBy(e => e.so.prefab.CurrentUnitType);
            var spammyTypes = enemyGroups.Where(g => g.Count() >= maxSameType).Select(g => g.Key).ToList();
            if (spammyTypes.Any())
            {
                var nonSpammyChoices = affordableBaseEnemies.Where(e => !spammyTypes.Contains(e.so.prefab.CurrentUnitType)).ToList();
                if (nonSpammyChoices.Any()) affordableBaseEnemies = nonSpammyChoices;
            }

            var chosenEnemy = affordableBaseEnemies[UnityEngine.Random.Range(0, affordableBaseEnemies.Count)];
            wave.Add(chosenEnemy);
            pointsLeft -= chosenEnemy.variant.threat;
            ColoredDebug.CLog(gameObject, "<color=#FFE4B5>AssembleWave:</color> Добавлен <color=white>{0} (Lvl {1})</color>. Остаток: <color=yellow>{2:F1}</color>.", _ColoredDebug, chosenEnemy.so.name, chosenEnemy.variant.level, pointsLeft);

            if (wave.Count % 3 == 0)
            {
                for (int i = wave.Count - 3; i < wave.Count; i++)
                {
                    if (i < 0) continue;
                    var originalEnemy = wave[i];
                    if (originalEnemy.variant.level > minEnemyLevel) continue;
                    var upgradedVariant = availableEnemies.FirstOrDefault(e => e.so == originalEnemy.so && (int)e.variant.level == (int)minEnemyLevel + 1);
                    if (upgradedVariant.so != null)
                    {
                        float costDifference = upgradedVariant.variant.threat - originalEnemy.variant.threat;
                        if (pointsLeft >= costDifference)
                        {
                            pointsLeft -= costDifference;
                            wave[i] = upgradedVariant;
                            ColoredDebug.CLog(gameObject, "<color=#90EE90>AssembleWave:</color> Улучшен <color=white>{0}</color> до <color=lime>{1}</color>. Остаток: <color=yellow>{2:F1}</color>.", _ColoredDebug, upgradedVariant.so.name, upgradedVariant.variant.level, pointsLeft);
                            break;
                        }
                    }
                }
            }

            yield return null;
        }
        onComplete?.Invoke((wave, "Стандартная Битва"));
    }

    private IEnumerator AssembleMiniBossWave(StringBuilder reportBuilder, Action<(List<(EnemySO so, EnemySO.EnemyVariant variant)>, string)> onComplete, float threatBudget, LevelData levelData)
    {
        var wave = new List<(EnemySO so, EnemySO.EnemyVariant variant)>();
        if (levelData.availableMiniBosses.Count == 0)
        {
            yield return StartCoroutine(AssembleStandardBattleWave(reportBuilder, onComplete, threatBudget, levelData));
            yield break;
        }
        onComplete?.Invoke((wave, "Мини-Босс"));
        yield return null;
    }
    #endregion

    #region Хелперы
    private List<(EnemySO so, EnemySO.EnemyVariant variant)> GetChoicesForLevel(LevelData levelData, bool includeMiniBosses)
    {
        var choices = new List<(EnemySO so, EnemySO.EnemyVariant variant)>();
        if (levelData == null)
        {
            ColoredDebug.CLog(gameObject, "<color=red>GetChoicesForLevel:</color> LevelData is NULL!", _ColoredDebug);
            return choices;
        }

        var enemySourceList = new List<EnemySO>(levelData.availableNormalEnemies);
        if (includeMiniBosses)
        {
            enemySourceList.AddRange(levelData.availableMiniBosses);
        }
        enemySourceList = enemySourceList.Where(so => so != null).Distinct().ToList();

        if (enemySourceList.Count == 0)
        {
            ColoredDebug.CLog(gameObject, "<color=red>GetChoicesForLevel:</color> Список врагов для уровня {0} в ассете '{1}' ПУСТ!", _ColoredDebug, levelData.levelNumber, "LevelProgression");
        }

        foreach (var so in enemySourceList)
        {
            foreach (var variant in so.AvailableVariants)
            {
                choices.Add((so, variant));
            }
        }
        return choices;
    }

    private string GenerateWaveSignature(List<(EnemySO so, EnemySO.EnemyVariant variant)> wave)
    {
        if (wave == null || wave.Count == 0) return "";
        return string.Join(";", wave.OrderBy(e => e.so.name).ThenBy(e => e.variant.level).Select(e => $"{e.so.name}:{(int)e.variant.level}"));
    }
    #endregion
}