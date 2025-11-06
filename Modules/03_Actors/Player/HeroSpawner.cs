// НАЗНАЧЕНИЕ: Отвечает за создание префаба героя на сцене и его корректную инициализацию на боевой сетке.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: GameInstance, HeroDataSO, BattleUnit, BattleGrid.
// ПРИМЕЧАНИЕ: Является точкой входа для появления героя в игровом мире.
using Sirenix.OdinInspector;
using UnityEngine;

public class HeroSpawner : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private SpriteRenderer _icon;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] protected bool _ColoredDebug;
    [BoxGroup("DEBUG"), SerializeField] private BattleUnit _heroUnitInstance;
    #endregion Поля

    #region Свойства
    public SpriteRenderer Icon { get => _icon; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>HeroSpawner:</color> Awake - Подготовка к созданию героя.", _ColoredDebug);
        if (_icon == null) DebugUtils.LogMissingReference(this, nameof(_icon));
        _icon.enabled = false;

        if (GameInstance.Instance == null)
        {
            ColoredDebug.CLog(gameObject, "<color=red>HeroSpawner:</color> GameInstance не найден! Герой не может быть создан.", true);
            return;
        }

        var heroData = GameInstance.Instance.SelectedHeroData;
        if (heroData != null && heroData.HeroPrefab != null)
        {
            // 1. Просто создаем префаб и сохраняем ссылку на компонент BattleUnit
            GameObject newHeroObject = Instantiate(heroData.HeroPrefab);
            newHeroObject.name = $"HeroPrefab: {heroData.name}";
            newHeroObject.transform.position = transform.position; // Временная позиция
            _heroUnitInstance = newHeroObject.GetComponent<BattleUnit>();

            if (_heroUnitInstance == null)
            {
                Debug.LogError($"[HeroSpawner] На префабе героя отсутствует компонент BattleUnit!");
                Destroy(newHeroObject);
            }
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=red>HeroSpawner:</color> В GameInstance не выбраны данные героя или отсутствует префаб!", true);
        }
    }

    private void Start()
    {
        // Если герой не был создан в Awake, выходим
        if (_heroUnitInstance == null) return;

        ColoredDebug.CLog(gameObject, "<color=cyan>HeroSpawner:</color> Start - Размещение героя на сетке.", _ColoredDebug);

        var grid = BattleGrid.Instance;
        if (grid == null || grid.Grid == null)
        {
            Debug.LogError("[HeroSpawner] BattleGrid или его внутренняя сетка не инициализированы к моменту Start()!");
            Destroy(_heroUnitInstance.gameObject);
            return;
        }

        // 2. Рассчитываем целевую позицию: X=0, Y=центр
        Vector2Int heroSize = _heroUnitInstance.Stats.UnitSize;
        int targetY = (grid.Height / 2) - (heroSize.y / 2);
        BattleCell preferedCell = grid.Grid[0, targetY];

        if (preferedCell == null)
        {
            Debug.LogError($"[HeroSpawner] Не удалось получить ячейку в целевой позиции (0, {targetY})!");
            Destroy(_heroUnitInstance.gameObject);
            return;
        }

        // 3. Ищем ближайшее доступное место, начиная с целевой позиции
        BattleCell startCell = BattleGridUtils.FindNearestAvailableAnchor(preferedCell, heroSize);

        // 4. Инициализируем героя на найденном месте
        if (startCell != null)
        {
            _heroUnitInstance.transform.position = startCell.WorldPosition;
            _heroUnitInstance.Initialize(startCell);
            ColoredDebug.CLog(gameObject, "<color=cyan>HeroSpawner:</color> Герой <color=lime>{0}</color> успешно размещен на якорной позиции <color=yellow>{1}</color>.", _ColoredDebug, _heroUnitInstance.name, startCell.Position);
        }
        else
        {
            Debug.LogError($"[HeroSpawner] КРИТИЧЕСКАЯ ОШИБКА! На поле нет свободного места для размещения героя размером {heroSize} рядом с позицией (0, {targetY}).");
            Destroy(_heroUnitInstance.gameObject);
        }
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Создает и инициализирует префаб героя на сетке.
    /// </summary>
    /// <param name="heroDataSO">Данные героя для спавна.</param>
    public void SpawnHero(HeroDataSO heroDataSO)
    {
        if (heroDataSO.HeroPrefab == null)
        {
            Debug.LogError($"[HeroSpawner] У героя '{heroDataSO.name}' не назначен префаб!");
            return;
        }

        GameObject newHeroObject = Instantiate(heroDataSO.HeroPrefab);
        newHeroObject.name = $"HeroPrefab: {heroDataSO.name}";

        BattleUnit heroUnit = newHeroObject.GetComponent<BattleUnit>();
        if (heroUnit == null)
        {
            Debug.LogError($"[HeroSpawner] На префабе героя отсутствует компонент BattleUnit!");
            Destroy(newHeroObject);
            return;
        }

        // 1. Находим предпочтительную ячейку под спавнером
        BattleCell preferedCell = BattleGrid.Instance.GetCellFromWorldPosition(transform.position);
        if (preferedCell == null)
        {
            Debug.LogError("[HeroSpawner] Не удалось найти стартовую ячейку на сетке для героя!");
            Destroy(newHeroObject);
            return;
        }

        // 2. Используем новую утилиту для поиска ближайшего ДОСТУПНОГО места
        Vector2Int heroSize = heroUnit.Stats.UnitSize;
        BattleCell startCell = BattleGridUtils.FindNearestAvailableAnchor(preferedCell, heroSize);

        // 3. Проверяем результат
        if (startCell != null)
        {
            // Ставим героя на найденное место и инициализируем
            newHeroObject.transform.position = startCell.WorldPosition; // Сразу ставим на якорь
            heroUnit.Initialize(startCell);
            ColoredDebug.CLog(gameObject, "<color=cyan>HeroSpawner:</color> Герой <color=lime>{0}</color> создан и размещен на якорной позиции <color=yellow>{1}</color>.", _ColoredDebug, heroDataSO.name, startCell.Position);
        }
        else
        {
            // Если свободных мест нет, выводим критическую ошибку
            Debug.LogError($"[HeroSpawner] КРИТИЧЕСКАЯ ОШИБКА! На поле нет свободного места для размещения героя '{heroDataSO.name}' размером {heroSize}.");
            Destroy(newHeroObject);
        }
    }
    #endregion Публичные методы
}