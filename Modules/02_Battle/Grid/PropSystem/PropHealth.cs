using UnityEngine;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Prop))]
public class PropHealth : MonoBehaviour, IDamageable
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Prop _prop;
    #endregion

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _currentHealth;
    [BoxGroup("DEBUG"), SerializeField] private bool _ColoredDebug = false;
    #endregion

    #region Свойства
    public int CurrentHealth => _currentHealth;
    public bool IsAlive => _currentHealth > 0;
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (_prop == null) _prop = GetComponent<Prop>();
        if (_prop == null) Debug.LogError($"[PropHealth] Component 'Prop' not found on {gameObject.name}!");
    }
    #endregion

    #region Public Methods
    public void Initialize()
    {
        if (_prop == null || _prop.PropSO == null) return;

        if (_prop.PropSO.IsDestructible)
        {
            _currentHealth = _prop.PropSO.MaxHealth;
            ColoredDebug.CLog(gameObject, "<color=cyan>PropHealth:</color> Initialized. Health: <color=lime>{0}/{1}</color>.", _ColoredDebug, _currentHealth, _prop.PropSO.MaxHealth);
        }
        else
        {
            _currentHealth = 1; // Non-destructible props technically have health > 0
        }
    }

    public void ResetHealth()
    {
        if (_prop != null && _prop.PropSO != null && _prop.PropSO.IsDestructible)
        {
            _currentHealth = _prop.PropSO.MaxHealth;
        }
        else
        {
            _currentHealth = 1;
        }
    }

    #endregion

    #region IDamageable Implementation
    public void TakeDamage(int damage, BattleUnit attacker = null, bool isCritical = false)
    {
        if (_prop == null || _prop.PropSO == null || !_prop.PropSO.IsDestructible || !IsAlive)
        {
            return;
        }

        string attackerName = attacker != null ? attacker.name : "UNKNOWN";
        int damageToTake = Mathf.Max(1, damage);

        _currentHealth -= damageToTake;
        _currentHealth = Mathf.Max(0, _currentHealth);

        ColoredDebug.CLog(gameObject, "<color=orange>PropHealth:</color> Prop '{0}' takes <color=red>{1}</color> damage from {2}. Health: <color=lime>{3}/{4}</color>.", _ColoredDebug, _prop.PropSO.name, damageToTake, attackerName, _currentHealth, _prop.PropSO.MaxHealth);

        // Show floating text
        if (FloatingTextManager.Instance != null)
        {
            FloatingTextManager.Instance.SpawnFloatingText(damageToTake.ToString(), FloatingText.TextType.Damage, GetDamagePoint().position);
        }


        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    public Transform GetDamagePoint()
    {
        return transform;
    }

    public void Heal(int amount)
    {
        ColoredDebug.CLog(gameObject, "<color=orange>PropHealth:</color> Heal called, but props usually don't heal.", _ColoredDebug);
    }

    public void Die()
    {
        ColoredDebug.CLog(gameObject, "<color=red>PropHealth:</color> Prop '{0}' destroyed!", _ColoredDebug, _prop.PropSO.name);
        BattleGridPropManager.Instance.DestroyProp(gameObject);
    }
    #endregion
}