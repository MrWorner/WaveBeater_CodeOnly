// НАЗНАЧЕНИЕ: Является централизованным реестром для всех вражеских юнитов на сцене.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnit.
// ПРИМЕЧАНИЕ: Предоставляет единую точку доступа к списку активных врагов и управляет событием их смерти.
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;

public class EnemyManager : MonoBehaviour
{
    public event UnityAction<BattleUnit> OnEnemyDeath;

    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), ReadOnly, SerializeField] private List<BattleUnit> _enemies = new List<BattleUnit>();
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    private static EnemyManager _instance;
    public static EnemyManager Instance { get => _instance; }
    public List<BattleUnit> Enemies { get => _enemies; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null) { DebugUtils.LogInstanceAlreadyExists(this); } else { _instance = this; }
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Регистрирует нового врага в системе.
    /// </summary>
    /// <param name="e">Экземпляр врага для регистрации.</param>
    public void RegisterEnemy(BattleUnit e)
    {
        if (e != null && !_enemies.Contains(e))
        {
            _enemies.Add(e);
            ColoredDebug.CLog(gameObject, "<color=cyan>EnemyManager:</color> Враг <color=lime>{0}</color> зарегистрирован. Всего врагов: <color=yellow>{1}</color>.", _ColoredDebug, e.name, _enemies.Count);
        }
    }

    /// <summary>
    /// Добавляет слушателя на событие смерти врага.
    /// </summary>
    public void RegisterEnemyDeathListener(UnityAction<BattleUnit> listener)
    {
        OnEnemyDeath += listener;
    }

    /// <summary>
    /// Удаляет слушателя с события смерти врага.
    /// </summary>
    public void UnregisterEnemyDeathListener(UnityAction<BattleUnit> listener)
    {
        OnEnemyDeath -= listener;
    }

    /// <summary>
    /// Снимает врага с регистрации (обычно после его смерти).
    /// </summary>
    /// <param name="e">Экземпляр врага для снятия с регистрации.</param>
    public void UnregisterEnemy(BattleUnit e)
    {
        if (e != null && _enemies.Remove(e))
        {
            ColoredDebug.CLog(gameObject, "<color=orange>EnemyManager:</color> Враг <color=yellow>{0}</color> снят с регистрации. Осталось врагов: <color=yellow>{1}</color>.", _ColoredDebug, e.name, _enemies.Count);
            // Вызываем событие OnEnemyDeath при удалении врага
            OnEnemyDeath?.Invoke(e);
        }
    }
    #endregion Публичные методы
}