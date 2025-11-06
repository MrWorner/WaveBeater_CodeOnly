// НАЗНАЧЕНИЕ: Хранит все общие структуры данных (классы и перечисления), используемые системой волн, режиссера и отчетов.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: Отсутствуют. Является файлом-определением.
// ПРИМЕЧАНИЕ: Централизация этих структур в одном файле решает проблемы с зависимостями и ошибками "type or namespace name could not be found".
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

public enum PlayerPerformance
{
    [LabelText("Отлично (0% урона)")]
    Excellent,
    [LabelText("Хорошо (1-15% урона)")]
    Good,
    [LabelText("Нормально (16-30% урона)")]
    Normal,
    [LabelText("Плохо (31-50% урона)")]
    Bad,
    [LabelText("Критическое (>50% урона)")]
    Critical
}

[System.Serializable]
public class BattleReport
{
    public int WaveNumber;
    [ReadOnly] public PlayerPerformance Performance;
    [ReadOnly] public int StrongPlayStreak;
    [ReadOnly] public int SustainedSuccessStreak;
    [ReadOnly] public float HeroHealthRatio;
    [ReadOnly] public float ThreatLevel;
    [ReadOnly] public string SystemDecision;
    [ReadOnly] public List<SpawnedEnemyInfo> SpawnedEnemies;
    [Header("Состояние игрока в конце боя")]
    [ReadOnly] public int HeroHealthAtEnd;
    [ReadOnly] public int HeroMaxHealth;
    [ReadOnly] public int HeroAttackDamage;
    [ReadOnly] public int HeroCurrency;
    [ReadOnly] public int GridWidth;
}

[System.Serializable]
public class SpawnedEnemyInfo
{
    public string Name;
    public string Level;
    public int MaxHealth;
    public int AttackDamage;
    public string Type;
    public string Accuracy;
    public int ActionPoints;
}

/// <summary>
/// Описывает состав врагов в одной волне. (УСТАРЕЛО)
/// </summary>
[System.Serializable]
public class EnemySpawnInfo
{
    public BattleUnit.UnitType_DEPRECATED type;
    public int level;
}

/// <summary>
/// Контейнер для данных о волне, с возможностью сравнения. (УСТАРЕЛО)
/// </summary>
public class WaveData
{
    public List<EnemySpawnInfo> Enemies;
    private string _canonicalKey = null;

    public WaveData(List<EnemySpawnInfo> enemies)
    {
        Enemies = enemies;
    }

    public string GetCanonicalKey()
    {
        if (_canonicalKey != null) return _canonicalKey;
        if (Enemies == null || Enemies.Count == 0) return "";
        var sortedEnemies = Enemies.OrderBy(e => e.type).ThenBy(e => e.level).ToList();
        _canonicalKey = string.Join(";", sortedEnemies.Select(e => $"{e.type}:{e.level}"));
        return _canonicalKey;
    }

    public override bool Equals(object obj)
    {
        if (obj is WaveData other)
        {
            return this.GetCanonicalKey() == other.GetCanonicalKey();
        }
        return false;
    }

    public override int GetHashCode()
    {
        return GetCanonicalKey().GetHashCode();
    }
}


/// <summary>
/// Хранит запись о производительности для одной конкретной волны.
/// </summary>
public class WavePerformanceRecord
{
    public WaveData WaveComposition { get; private set; }
    public float DamageDealtToPlayerPercent { get; private set; }

    public PlayerPerformance Rating { get; private set; }

    public WavePerformanceRecord(WaveData waveData, float damagePercent)
    {
        WaveComposition = waveData;
        DamageDealtToPlayerPercent = damagePercent;
        Rating = CalculateRating(damagePercent);
    }

    private PlayerPerformance CalculateRating(float damagePercent)
    {
        if (damagePercent == 0) return PlayerPerformance.Excellent;
        if (damagePercent <= 15) return PlayerPerformance.Good;
        if (damagePercent <= 30) return PlayerPerformance.Normal;
        if (damagePercent <= 50) return PlayerPerformance.Bad;
        return PlayerPerformance.Critical;
    }
}