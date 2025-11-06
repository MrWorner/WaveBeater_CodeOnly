// НАЗНАЧЕНИЕ: Отвечает за создание префаба врага на сцене, его корректную инициализацию на боевой сетке и применение уровня.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: EnemySO, EnemyPlacementService, EnemyManager, BattleUnit.
// ПРИМЕЧАНИЕ: Является центральной точкой для появления врагов в игровом мире во время боя.
using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyFactory : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private EnemyPlacementService _placementService;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Transform _spawnPoint;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private SpriteRenderer _spawnPointSprite;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private BattleUnit _lastSpawnedEnemy;
    #endregion Поля

    #region Свойства
    public EnemyPlacementService PlacementService { get => _placementService; }
    public Transform SpawnPoint { get => _spawnPoint; }
    public BattleUnit LastSpawnedEnemy { get => _lastSpawnedEnemy; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_placementService == null) DebugUtils.LogMissingReference(this, nameof(_placementService));
        if (_spawnPoint == null) DebugUtils.LogMissingReference(this, nameof(_spawnPoint));

        _spawnPointSprite.enabled = false;
    }
    #endregion Методы UNITY

    #region Публичные методы
    public BattleUnit SpawnEnemy(EnemySO enemySO, BattleUnit.UnitLevel level)
    {
        if (enemySO == null || enemySO.prefab == null)
        {
            Debug.LogError("<color=red>EnemyFactory:</color> Attempted to spawn an enemy with null SO or prefab.");
            return null;
        }

        // 1. Создаем экземпляр базового префаба
        BattleUnit enemyInstance = Instantiate(enemySO.prefab);
        enemyInstance.gameObject.name = $"{enemySO.name}_{level}";
        enemyInstance.transform.position = _spawnPoint.position;

        // 2. Сначала размещаем и инициализируем (это сохранит базовые статы)
        _placementService.PlaceEnemy(enemyInstance);

        // 3. Только потом меняем уровень (это применит бонусы поверх базовых статов)
        enemyInstance.ChangeUnitLevel(level);

        // 4. Регистрируем в менеджере
        EnemyManager.Instance.RegisterEnemy(enemyInstance);

        _lastSpawnedEnemy = enemyInstance;
        return enemyInstance;
    }
    #endregion Публичные методы
}