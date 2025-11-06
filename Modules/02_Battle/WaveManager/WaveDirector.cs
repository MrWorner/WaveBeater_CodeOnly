// НАЗНАЧЕНИЕ: "Мозг" системы волн. Анализирует производительность игрока, управляет уровнем угрозы и сериями успехов/неудач.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleLogger, BattleUnit, WaveStructures.
// ПРИМЕЧАНИЕ: Не занимается созданием врагов, а только определяет "бюджет" для следующей волны.
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class WaveDirector : MonoBehaviour
{
    #region Поля: Required
    private BattleUnit _hero;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS")]
    [BoxGroup("SETTINGS/Threat Calculation"), SerializeField] private float _initialThreatLevel = 1f;
    [BoxGroup("SETTINGS/Threat Calculation"), SerializeField] private float _baseWaveThreatBonus = 1f;
    [BoxGroup("SETTINGS/Threat Calculation"), SerializeField] private int _gracePeriodAfterStruggle = 1;
    [BoxGroup("SETTINGS/Streaks"), SerializeField] private int _shortStreakThreshold = 2;
    [BoxGroup("SETTINGS/Streaks"), SerializeField] private int _shortStreakThreatBonus = 1;
    [BoxGroup("SETTINGS/Streaks"), SerializeField] private int _longStreakThreshold = 4;
    [BoxGroup("SETTINGS/Streaks"), SerializeField] private int _longStreakThreatBonus = 2;
    [BoxGroup("SETTINGS/Performance Multipliers"), SerializeField, Range(1f, 1.5f)] private float _excellentPerformanceMultiplier = 1.15f;
    [BoxGroup("SETTINGS/Performance Multipliers"), SerializeField, Range(1f, 1.2f)] private float _goodPerformanceMultiplier = 1.05f;
    [BoxGroup("SETTINGS/Performance Multipliers"), SerializeField, Range(0.5f, 1.5f)] private float _normalPerformanceMultiplier = 1.0f;
    [BoxGroup("SETTINGS/Performance Multipliers"), SerializeField, Range(0.1f, 1f)] private float _badPerformanceMultiplier = 0.9f;
    [BoxGroup("SETTINGS/Performance Multipliers"), SerializeField, Range(0.1f, 1f)] private float _criticalPerformanceMultiplier = 0.8f;
    [BoxGroup("SETTINGS/General"), SerializeField] private bool _autoOpenReportOnDeath = true;

    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private float _currentThreatLevel;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _currentWaveNumber = 0;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _strongPlayStreakCounter = 0;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _sustainedSuccessStreak = 0;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _recoveryWavesRemaining = 0;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    public float CurrentThreatLevel => _currentThreatLevel;
    #endregion Свойства

    #region Публичные методы
    /// <summary>
    /// Инициализирует Режиссёра и подписывается на смерть героя.
    /// </summary>
    public void Initialize(BattleUnit hero)
    {
        _hero = hero;

        var selectedLevelProgression = GameInstance.Instance.SelectedLevelProgression;
        if (selectedLevelProgression != null)
        {
            _initialThreatLevel = selectedLevelProgression.InitialThreatLevel;
            _baseWaveThreatBonus = selectedLevelProgression.BaseWaveThreatBonus;
        }

        if (_hero != null && _hero.Health != null)
        {
            _hero.Health.OnDeath += HandleHeroDeath;
        }

        _currentThreatLevel = _initialThreatLevel;
        ColoredDebug.CLog(gameObject, "<color=cyan>WaveDirector:</color> Инициализация. Начальный уровень угрозы: <color=yellow>{0}</color>.", _ColoredDebug, _currentThreatLevel);
    }

    /// <summary>
    /// Анализирует завершенный бой и готовит данные для следующего.
    /// </summary>
    public void AnalyzeBattleAndPrepareForNext(List<(EnemySO so, EnemySO.EnemyVariant variant)> spawnedEnemies, int heroHealthAtStart)
    {
        _currentWaveNumber++;
        ColoredDebug.CLog(gameObject, "<color=cyan>WaveDirector:</color> Анализ волны <color=yellow>#{0}</color>.", _ColoredDebug, _currentWaveNumber);

        PlayerPerformance performance = EvaluatePlayerPerformance(heroHealthAtStart);
        UpdateCounters(performance);
        string finalDecision = UpdateThreatLevelAndGetDecision(performance);

        var report = new BattleReport
        {
            WaveNumber = _currentWaveNumber,
            Performance = performance,
            StrongPlayStreak = _strongPlayStreakCounter,
            SustainedSuccessStreak = _sustainedSuccessStreak,
            HeroHealthRatio = (float)_hero.Health.CurrentHealth / _hero.Stats.MaxHealth,
            ThreatLevel = _currentThreatLevel,
            SystemDecision = finalDecision
        };

        BattleLogger.Instance.LogWaveAnalysis(report);
    }
    #endregion Публичные методы

    #region Личные методы
    private void OnDestroy()
    {
        if (_hero != null && _hero.Health != null)
        {
            _hero.Health.OnDeath -= HandleHeroDeath;
        }
    }

    private void HandleHeroDeath()
    {
        ColoredDebug.CLog(gameObject, "<color=red>WaveDirector:</color> Получено событие смерти героя. Запись в лог.", _ColoredDebug);
        BattleLogger.Instance.LogBattleEnd("ПОРАЖЕНИЕ - ГЕРОЙ УНИЧТОЖЕН");
        if (_autoOpenReportOnDeath)
        {
            BattleLogger.Instance.OpenLog();
        }
    }

    private void UpdateCounters(PlayerPerformance performance)
    {
        if (performance == PlayerPerformance.Excellent || performance == PlayerPerformance.Good) _strongPlayStreakCounter++;
        else _strongPlayStreakCounter = 0;

        if (performance == PlayerPerformance.Excellent || performance == PlayerPerformance.Good || performance == PlayerPerformance.Normal) _sustainedSuccessStreak++;
        else _sustainedSuccessStreak = 0;

        if (performance == PlayerPerformance.Bad || performance == PlayerPerformance.Critical)
        {
            _recoveryWavesRemaining = _gracePeriodAfterStruggle;
        }
        else _recoveryWavesRemaining = Mathf.Max(0, _recoveryWavesRemaining - 1);

        ColoredDebug.CLog(gameObject, "<color=cyan>WaveDirector:</color> Счетчики обновлены. Серия сильной игры: <color=yellow>{0}</color>, Серия успеха: <color=yellow>{1}</color>.", _ColoredDebug, _strongPlayStreakCounter, _sustainedSuccessStreak);
    }

    private string UpdateThreatLevelAndGetDecision(PlayerPerformance performance)
    {
        float oldThreat = _currentThreatLevel;
        float multiplier = 1.0f;
        var sb = new StringBuilder("Решение Директора: ");

        switch (performance)
        {
            case PlayerPerformance.Excellent: multiplier = _excellentPerformanceMultiplier; sb.Append("Успешный бой."); break;
            case PlayerPerformance.Good: multiplier = _goodPerformanceMultiplier; sb.Append("Хороший бой."); break;
            case PlayerPerformance.Normal: multiplier = _normalPerformanceMultiplier; sb.Append("Нормальный бой."); break;
            case PlayerPerformance.Bad: multiplier = _badPerformanceMultiplier; sb.Append("Сложный бой."); break;
            case PlayerPerformance.Critical: multiplier = _criticalPerformanceMultiplier; sb.Append("Критический бой."); break;
        }

        float performanceBonus = (oldThreat * multiplier) - oldThreat;
        float maxThreatIncrease = _initialThreatLevel * (_currentWaveNumber / 50f) + 1.0f;
        performanceBonus = Mathf.Min(performanceBonus, maxThreatIncrease);

        _currentThreatLevel = oldThreat + _baseWaveThreatBonus + performanceBonus;
        _currentThreatLevel = Mathf.Max(_currentThreatLevel, _initialThreatLevel * 0.5f);

        if (_strongPlayStreakCounter >= _longStreakThreshold)
        {
            _currentThreatLevel += _longStreakThreatBonus;
        }
        else if (_strongPlayStreakCounter >= _shortStreakThreshold)
        {
            _currentThreatLevel += _shortStreakThreatBonus;
        }

        ColoredDebug.CLog(gameObject, "<color=cyan>WaveDirector:</color> Уровень угрозы обновлен. Был: <color=yellow>{0:F2}</color>, Стал: <color=lime>{1:F2}</color>.", _ColoredDebug, oldThreat, _currentThreatLevel);
        return sb.ToString();
    }

    private PlayerPerformance EvaluatePlayerPerformance(int heroHealthAtStart)
    {
        int damageTaken = heroHealthAtStart - _hero.Health.CurrentHealth;
        if (damageTaken <= 0) return PlayerPerformance.Excellent;

        float damageTakenPercent = (float)damageTaken / _hero.Stats.MaxHealth;

        if (damageTakenPercent <= 0.15f) return PlayerPerformance.Good;
        if (damageTakenPercent <= 0.30f) return PlayerPerformance.Normal;
        if (damageTakenPercent <= 0.50f) return PlayerPerformance.Bad;
        return PlayerPerformance.Critical;
    }
    #endregion Личные методы
}