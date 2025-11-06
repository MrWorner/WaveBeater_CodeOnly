// НАЗНАЧЕНИЕ: Компонент-посредник, который определяет лучшую цель для юнита, используя стратегию из назначенного TargetingProfile.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnit, TargetingProfile.
// ПРИМЕЧАНИЕ: Если TargetingProfile не указан, система по умолчанию будет выбирать целью Героя.

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

public class BattleUnitTargetingSystem : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnit _performer;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TargetingProfile _targetingProfile;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    public TargetingProfile TargetingProfile { get => _targetingProfile; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_performer == null) DebugUtils.LogMissingReference(this, nameof(_performer));
    }
    #endregion Методы UNITY

    #region Публичные методы
    public BattleUnit GetBestTarget()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>TargetingSystem:</color> Начинаю поиск лучшей цели...", _ColoredDebug);

        // Собираем цели в зависимости от фракции того, кто выполняет действие
        List<BattleUnit> potentialTargets = new List<BattleUnit>();
        if (_performer.FactionType == BattleUnit.Faction.Hero || _performer.FactionType == BattleUnit.Faction.Friendly)
        {
            // Если атакует герой или союзник, целями являются все живые враги
            potentialTargets.AddRange(BattleUnit.Enemies.Where(e => e != null && e.IsAlive));
        }
        else // Если атакует враг
        {
            // Целью является герой
            if (BattleUnit.Hero != null && BattleUnit.Hero.IsAlive)
            {
                potentialTargets.Add(BattleUnit.Hero);
            }
            // TODO: Добавить сюда союзников героя, если они появятся в игре
        }

        if (!potentialTargets.Any())
        {
            ColoredDebug.CLog(gameObject, "<color=orange>TargetingSystem:</color> Не найдено потенциальных целей.", _ColoredDebug);
            return null;
        }

        // Если профиль не назначен, возвращаем первую попавшуюся цель из списка
        if (_targetingProfile == null)
        {
            BattleUnit defaultTarget = potentialTargets.FirstOrDefault();
            ColoredDebug.CLog(gameObject, "<color=yellow>TargetingSystem:</color> TargetingProfile не назначен. Выбрана цель по умолчанию: <color=lime>{0}</color>.", _ColoredDebug, defaultTarget != null ? defaultTarget.name : "NONE");
            return defaultTarget;
        }

        BattleUnit bestTarget = _targetingProfile.SelectTarget(_performer, potentialTargets);
        ColoredDebug.CLog(gameObject, "<color=cyan>TargetingSystem:</color> Профиль <color=yellow>{0}</color> выбрал цель: <color=lime>{1}</color>.", _ColoredDebug, _targetingProfile.name, bestTarget != null ? bestTarget.name : "NONE");
        return bestTarget;
    }
    #endregion Публичные методы
}