using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

/// <summary>
/// Профиль таргетинга, имитирующий продвинутую логику выбора цели,
/// аналогичную старому BattleUnitTargetingSystem. Он оценивает врагов по нескольким
/// критериям для выбора наиболее опасной или уязвимой цели.
/// </summary>
[CreateAssetMenu(fileName = "TP_HeroAdvanced", menuName = "AI/Targeting Profiles/Hero Advanced Targeting")]
public class HeroTargetingProfile : TargetingProfile
{
    private class EnemyThreatInfo
    {
        public BattleUnit Enemy;
        public float ProximityScore;
        public float HealthScore;
        public float DamageScore;
        public float KillabilityScore;
        public string DebugReason;
        public float TotalThreatScore => ProximityScore + HealthScore + DamageScore + KillabilityScore;

        public EnemyThreatInfo(BattleUnit enemy)
        {
            Enemy = enemy;
        }
    }

    #region Поля
    [BoxGroup("SETTINGS", order: 1)]
    [BoxGroup("SETTINGS/Threat Weights"), Tooltip("Огромный бонус для юнитов ближнего боя, достигших цели.")]
    [SerializeField] private float _meleeAtGoalBonus = 2000f;
    [BoxGroup("SETTINGS/Threat Weights"), Tooltip("Бонус для дальнобойных врагов, которые уже целятся в игрока.")]
    [SerializeField] private float _rangedIsAimingBonus = 700f;
    [BoxGroup("SETTINGS/Threat Weights"), Tooltip("Бонус для юнитов дальнего боя, которые просто находятся в зоне атаки.")]
    [SerializeField] private float _rangedInRangeBonus = 300f;
    [BoxGroup("SETTINGS/Threat Weights"), Tooltip("Основной множитель угрозы от близости к игроку.")]
    [SerializeField] private float _distanceThreatMultiplier = 50f;
    [BoxGroup("SETTINGS/Threat Weights"), Tooltip("Множитель, повышающий приоритет раненых врагов.")]
    [SerializeField] private float _healthThreatMultiplier = 20f;
    [BoxGroup("SETTINGS/Threat Weights"), Tooltip("Множитель, чтобы из двух одинаково опасных врагов выбрать того, у кого выше урон.")]
    [SerializeField] private float _damageThreatMultiplier = 10f;
    [BoxGroup("SETTINGS/Threat Weights"), Tooltip("Множитель, повышающий приоритет врагов, которых можно быстро убить.")]
    [SerializeField] private float _killabilityThreatMultiplier = 500f;
    [BoxGroup("DEBUG", order: 2), SerializeField] private bool _ColoredDebug;
    #endregion Поля

    public override BattleUnit SelectTarget(BattleUnit performer, List<BattleUnit> potentialTargets)
    {
        ColoredDebug.CLog(performer.gameObject, "<color=cyan>HeroTargeting:</color> Начинаю анализ целей. Врагов: <color=yellow>{0}</color>.", _ColoredDebug, potentialTargets?.Count ?? 0);
        if (potentialTargets == null || !potentialTargets.Any())
        {
            ColoredDebug.CLog(performer.gameObject, "<color=red>HeroTargeting:</color> Нет доступных врагов для анализа.", _ColoredDebug);
            return null;
        }

        List<EnemyThreatInfo> threatInfos = potentialTargets
            .Where(enemy => enemy != null && enemy.IsAlive)
            .Select(enemy => CalculateThreat(performer, enemy))
            .ToList();

        if (!threatInfos.Any())
        {
            ColoredDebug.CLog(performer.gameObject, "<color=red>HeroTargeting:</color> В списке нет живых врагов.", _ColoredDebug);
            return null;
        }

        var bestTargetInfo = threatInfos.OrderByDescending(info => info.TotalThreatScore).First();
        ColoredDebug.CLog(performer.gameObject, "<color=magenta>HeroTargeting:</color> Финальный выбор: <color=lime>{0}</color> с общим счетом <color=yellow>{1:F2}</color>.", _ColoredDebug, bestTargetInfo.Enemy.name, bestTargetInfo.TotalThreatScore);

        return bestTargetInfo.Enemy;
    }

    private EnemyThreatInfo CalculateThreat(BattleUnit performer, BattleUnit enemy)
    {
        var threatInfo = new EnemyThreatInfo(enemy);
        float proximityScore = 0f;

        // 1. Приоритет по типу атаки и позиции
        var meleeMode = enemy.Arsenal.GetAllAttackModes().FirstOrDefault(m => m.isMelee);
        if (meleeMode != null && enemy.CurrentPosition.x <= meleeMode.range)
        {
            proximityScore += _meleeAtGoalBonus;
            threatInfo.DebugReason = "Атакует в ближнем бою!";
        }
        else
        {
            var rangedMode = enemy.Arsenal.GetAllAttackModes().FirstOrDefault(m => !m.isMelee);
            if (rangedMode != null)
            {
                if (enemy.UI.IsAiming)
                {
                    proximityScore += _rangedIsAimingBonus;
                    threatInfo.DebugReason = "Целится!";
                }
                else if (enemy.CurrentPosition.x <= rangedMode.range)
                {
                    proximityScore += _rangedInRangeBonus;
                    threatInfo.DebugReason = "В зоне атаки (дальний бой)!";
                }
            }
        }

        // 2. Близость к цели
        int gridWidth = BattleGrid.Instance != null ? BattleGrid.Instance.Width : 20; // Безопасное значение по умолчанию
        proximityScore += (gridWidth - enemy.CurrentPosition.x) * _distanceThreatMultiplier;
        threatInfo.ProximityScore = proximityScore;

        // 3. Раненые враги
        float healthRatio = (float)enemy.Health.CurrentHealth / enemy.Stats.MaxHealth;
        threatInfo.HealthScore = (1 - healthRatio) * _healthThreatMultiplier;

        // 4. Урон (как тай-брейкер)
        var primaryAttack = enemy.Arsenal.GetAllAttackModes().OrderByDescending(m => m.damage).FirstOrDefault();
        if (primaryAttack != null)
        {
            threatInfo.DamageScore = (float)primaryAttack.damage * _damageThreatMultiplier;
        }

        // 5. "Убиваемость"
        var performerPrimaryAttack = performer.Arsenal.GetAllAttackModes().OrderByDescending(m => m.damage).FirstOrDefault();
        if (performerPrimaryAttack != null && performerPrimaryAttack.damage > 0)
        {
            float hitsToKill = Mathf.Ceil((float)enemy.Health.CurrentHealth / performerPrimaryAttack.damage);
            if (hitsToKill > 0)
            {
                threatInfo.KillabilityScore = (1f / hitsToKill) * _killabilityThreatMultiplier;
            }
        }

        ColoredDebug.CLog(performer.gameObject,
            "<color=lime>HeroTargeting:</color> Расчет угрозы для <color=white>{0}</color>:\n" +
            "  - Угроза близости: <color=yellow>{1:F2}</color> ({2})\n" +
            "  - Бонус здоровья: <color=cyan>{3:F2}</color>\n" +
            "  - Бонус урона: <color=orange>{4:F2}</color>\n" +
            "  - Бонус \"убиваемости\": <color=purple>{5:F2}</color>\n" +
            "  - <b>ИТОГО: <color=red>{6:F2}</color></b>",
            _ColoredDebug,
            enemy.name, threatInfo.ProximityScore, threatInfo.DebugReason ?? "Движется к цели",
            threatInfo.HealthScore, threatInfo.DamageScore, threatInfo.KillabilityScore, threatInfo.TotalThreatScore);

        return threatInfo;
    }
}