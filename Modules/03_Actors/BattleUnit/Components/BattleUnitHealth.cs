// НАЗНАЧЕНИЕ: Управляет здоровьем боевой единицы, обработкой урона, исцелением и смертью. Вызывает повреждение клетки при смерти.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: BattleUnit, BattleUnitStats, BattleUnitAbilities, BattleUnitAnimator, BattleUnitUI, BattleLogger, BattleCell.
// ПРИМЕЧАНИЕ: Логика TakeDamage() взаимодействует с способностями (щиты, броня) и шансами уклонения.
using Sirenix.OdinInspector;
using System.Collections.Generic; // Добавлено для List<BattleCell>
using UnityEngine;
using UnityEngine.Events;

public class BattleUnitHealth : MonoBehaviour
{
    public event UnityAction OnDeath;
    public event UnityAction<int, BattleUnit> OnDamageTaken;

    private const string FIRST_DAMAGE_FLAG = "FirstDamageTaken";
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnit _battleUnit;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitStats _unitStats;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitAbilities _unitAbilities;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitAnimator _unitAnimator;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitUI _unitUI;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _currentHealth;
    [BoxGroup("DEBUG"), SerializeField] private bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    public int CurrentHealth => _currentHealth;
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_battleUnit == null) DebugUtils.LogMissingReference(this, nameof(_battleUnit));
        if (_unitStats == null) DebugUtils.LogMissingReference(this, nameof(_unitStats));
        if (_unitAbilities == null) DebugUtils.LogMissingReference(this, nameof(_unitAbilities));
        if (_unitAnimator == null) DebugUtils.LogMissingReference(this, nameof(_unitAnimator));
        if (_unitUI == null) DebugUtils.LogMissingReference(this, nameof(_unitUI));
    }
    #endregion Методы UNITY

    #region Публичные методы
    public void Initialize(int maxHealth)
    {
        _currentHealth = maxHealth;
        ColoredDebug.CLog(gameObject, "<color=green>BattleUnitHealth:</color> Инициализация. Здоровье: <color=lime>{0}/{1}</color>.", _ColoredDebug, _currentHealth, _unitStats.MaxHealth);
    }

    public void TakeDamage(int damage, BattleUnit attacker = null, bool isCritical = false)
    {
        if (!_battleUnit.IsAlive) return;
        string attackerName = attacker != null ? attacker.name : "UNKNOWN";

        if (isCritical && (damage >= _currentHealth) && Random.value < _unitStats.CriticalHitEvasionChance)
        {
            ColoredDebug.CLog(gameObject, "<color=magenta>BattleUnitHealth:</color> <color=lime>ЧУДОМ УКЛОНИЛСЯ</color> от смертельного критического удара!", _ColoredDebug);
            _unitAnimator.PlayDodgeAnimation();
            _unitUI.ShowEvasionText();
            return;
        }

        if (Random.value < _unitStats.EvasionChance)
        {
            ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitHealth:</color> <color=yellow>{0}</color> <color=lime>УКЛОНИЛСЯ</color> от атаки <color=yellow>{1}</color>! (Шанс: <color=white>{2:P0}</color>)", _ColoredDebug, name, attackerName, _unitStats.EvasionChance);
            _unitAnimator.PlayDodgeAnimation();
            _unitUI.ShowEvasionText();
            SoundManager.Instance.PlayOneShot(SoundType.MeleeMiss);
            return;
        }

        ColoredDebug.CLog(gameObject, "<color=orange>BattleUnitHealth:</color> <color=yellow>{0}</color> получает <color=red>{1}</color> урона от <color=yellow>{2}</color>. Крит: <color=magenta>{3}</color>.", _ColoredDebug, name, damage, attackerName, isCritical);
        if (damage > 0 && _battleUnit.State != null && !_battleUnit.State.HasFlag(FIRST_DAMAGE_FLAG))
        {
            _battleUnit.State.SetFlag(FIRST_DAMAGE_FLAG);
        }

        int incomingDamage = _unitAbilities.ProcessDamage(damage);
        if (incomingDamage > 0)
        {
            _currentHealth -= incomingDamage;
            _currentHealth = Mathf.Max(0, _currentHealth);
            _unitUI.UpdateHealthDisplay(_currentHealth);
            _unitUI.ShowDamageText(incomingDamage, isCritical);

            OnDamageTaken?.Invoke(incomingDamage, attacker);

            BattleLogger.Instance.LogHealthChange(_battleUnit, -incomingDamage, $"Damage from {attackerName}", isCritical);
        }

        ColoredDebug.CLog(gameObject, "<color=orange>BattleUnitHealth:</color> Итоговый урон: <color=red>{0}</color>. Текущее здоровье: <color=lime>{1}/{2}</color>.", _ColoredDebug, incomingDamage, _currentHealth, _unitStats.MaxHealth);
        if (_currentHealth <= 0)
        {
            if (_unitAbilities.CanUseEmergencySystem())
            {
                _unitAbilities.UseEmergencySystem();
                int healAmount = Mathf.RoundToInt(_unitStats.MaxHealth * 0.5f);
                Heal(healAmount);
                SoundManager.Instance.PlayOneShot(SoundType.Resurrection);
            }
            else
            {
                HandleDeath();
            }
        }
        else if (damage > 0 && incomingDamage > 0)
        {
            _unitAnimator.PlayHitAnimation();
            SoundManager.Instance.PlayOneShot(SoundType.PlayerHit);
        }

        if (attacker != null && attacker.FactionType == BattleUnit.Faction.Enemy && _battleUnit.FactionType == BattleUnit.Faction.Hero)
        {
            _unitAbilities.TryBacklash(attacker);
        }
    }

    public void Heal(int amount)
    {
        if (!_battleUnit.IsAlive || _currentHealth >= _unitStats.MaxHealth) return;
        SoundManager.Instance.PlayOneShot(SoundType.PlayerHeal);
        int realHealAmount = Mathf.Min(amount, _unitStats.MaxHealth - _currentHealth);
        _currentHealth += realHealAmount;
        _unitUI.UpdateHealthDisplay(_currentHealth);
        _unitUI.ShowHealText(realHealAmount);
        ColoredDebug.CLog(gameObject, "<color=green>BattleUnitHealth:</color> Исцеление на <color=lime>{0}</color>. Текущее здоровье: <color=lime>{1}/{2}</color>.", _ColoredDebug, realHealAmount, _currentHealth, _unitStats.MaxHealth);

        BattleLogger.Instance.LogHealthChange(_battleUnit, realHealAmount, "Heal");
    }

    public void SetHealth(int current, int max)
    {
        _currentHealth = Mathf.Min(current, max);
    }

    public void AutoHeal()
    {
        if (_unitStats.AutoHealValue > 0)
        {
            Heal(_unitStats.AutoHealValue);
        }
    }
    #endregion Публичные методы

    #region Личные методы
    /// <summary>
    /// Обрабатывает смерть юнита, вызывает событие OnDeath и повреждает клетки под ним.
    /// </summary>
    private void HandleDeath()
    {
        ColoredDebug.CLog(gameObject, "<color=red><b>BattleUnitHealth ({0}):</b></color> <color=red><b>УНИЧТОЖЕН</b></color>.", _ColoredDebug, name); // Используем имя из _battleUnit

        if (_battleUnit.FactionType == BattleUnit.Faction.Enemy && EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy(_battleUnit);
        }

        SoundManager.Instance.PlayOneShot(SoundType.Explosion);
        GameObject explosionEffect = ObjectPoolExplosion.Instance.GetObject();
        explosionEffect.transform.position = transform.position;
        if (_battleUnit.MainBodyPart != null)
        {
            _battleUnit.MainBodyPart.SetActive(false);
        }

        if (_battleUnit.Movement != null)
        {
            var cellsToDamage = new List<BattleCell>(_battleUnit.Movement.OccupiedCells);
            _battleUnit.Movement.ClearOccupation();
            foreach (var cell in cellsToDamage)
            {
                if (cell != null)
                {
                    cell.TakeDamage();
                    ColoredDebug.CLog(gameObject, "<color=orange>BattleUnitHealth:</color> Клетка <color=yellow>{0}</color> получила урон при смерти юнита.", _ColoredDebug, cell.Position);
                }
            }
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=orange>BattleUnitHealth:</color> Компонент Movement не найден, клетки не повреждены.", _ColoredDebug);
        }

        int finalBounty = (int)(_unitStats.Bounty * Settings.BountyMultiplier);
        if (finalBounty <= 0) finalBounty = 1;

        CurrencyManager.Instance.AddCurrency(finalBounty);
        _unitUI.ShowBountyText(finalBounty);

        BattleLogger.Instance.LogDeath(_battleUnit);
        OnDeath?.Invoke();

        // Уничтожение объекта происходит в BattleUnit
        // Destroy(gameObject, 0.1f); 
    }
    #endregion
}