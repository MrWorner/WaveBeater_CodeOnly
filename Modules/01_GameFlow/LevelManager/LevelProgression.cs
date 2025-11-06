// НАЗНАЧЕНИЕ: Является ScriptableObject'ом, который определяет полную последовательность уровней для одной игровой сессии.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: LevelData, EnemySO.
// ПРИМЕЧАНИЕ: Содержит списки врагов для каждого уровня, настройки босса и награды. Используется LevelManager'ом для управления прогрессом.
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class LevelData
{
    #region Поля
    [BoxGroup("SETTINGS"), Tooltip("Минимальный уровень врагов, которые могут появиться на этом уровне"), SerializeField] public BattleUnit.UnitLevel minEnemyLevel;
    [BoxGroup("SETTINGS"), Tooltip("Список доступных обычных врагов для этого уровня"), SerializeField] public List<EnemySO> availableNormalEnemies = new List<EnemySO>();
    [BoxGroup("SETTINGS"), Tooltip("Список доступных мини-боссов для этого уровня"), SerializeField] public List<EnemySO> availableMiniBosses = new List<EnemySO>();
    [BoxGroup("DEBUG"), Tooltip("Номер уровня (устанавливается автоматически)"), SerializeField, ReadOnly] public int levelNumber;
    [BoxGroup("DEBUG"), Tooltip("Вариант фона (время суток)"), SerializeField, ReadOnly] public BackgroundVariant backgroundVariant;
    #endregion Поля
}

[CreateAssetMenu(fileName = "LevelProgression_New", menuName = "--->WaveBeater/Level Progression Pack", order = 10000)]
public class LevelProgression : ScriptableObject
{
    public string debugName = "A";
    public bool IsLocked = false;
    #region Поля
    [BoxGroup("SETTINGS"), Tooltip("Тип фона (локация) для всех уровней в этом пакете"), SerializeField] private BackgroundType _backgroundType = BackgroundType.City;
    [BoxGroup("SETTINGS"), ListDrawerSettings(ShowIndexLabels = true, AddCopiesLastElement = true), Tooltip("Список уровней, которые предстоит пройти игроку в рамках этого пакета"), SerializeField, OnCollectionChanged("UpdateLevelSettings")] private List<LevelData> _levels = new List<LevelData>();
    [BoxGroup("SETTINGS"), Tooltip("Финальный босс, который появится после прохождения всех уровней"), SerializeField] private EnemySO _finalBoss;
    [BoxGroup("SETTINGS"), Tooltip("Уровень (вариант) финального босса"), SerializeField] private BattleUnit.UnitLevel _finalBossVariant;
    [BoxGroup("SETTINGS"), Tooltip("Награда за прохождение всего пакета уровней (например, новая карта в магазин)"), SerializeField] private UpgradeCardDataSO _newBonusReward;
    [Title("Обычные враги"), BoxGroup("Batch Operations", Order = 1), InfoBox("Используйте эту секцию для массового добавления врагов в уровни."), SerializeField, Tooltip("Противник, которого нужно добавить в уровни.")] private EnemySO _normalEnemyToAdd;
    [BoxGroup("Batch Operations"), SerializeField, Tooltip("Минимальный номер уровня (levelNumber), с которого нужно начать добавление.")] private int _minLevelToAddNormalEnemy = 0;
    [BoxGroup("Batch Operations"), SerializeField, Tooltip("Мини-босс, которого нужно добавить в уровни.")] private EnemySO _miniBossToAdd;
    [BoxGroup("Batch Operations"), SerializeField, Tooltip("Минимальный номер уровня (levelNumber), с которого нужно начать добавление.")] private int _minLevelToAddMiniBoss = 0;
    [BoxGroup("EnemySalary"), SerializeField] private float _initialThreatLevel = 1f;
    [BoxGroup("EnemySalary"), SerializeField] protected float _baseWaveThreatBonus = 1f;

    [BoxGroup("DEBUG"), SerializeField] private bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    public BackgroundType BackgroundType => _backgroundType;
    public List<LevelData> Levels => _levels;
    public EnemySO FinalBoss => _finalBoss;
    public BattleUnit.UnitLevel FinalBossVariant => _finalBossVariant;
    public UpgradeCardDataSO NewBonusReward => _newBonusReward;

    public float InitialThreatLevel => _initialThreatLevel;
    public float BaseWaveThreatBonus => _baseWaveThreatBonus;
    #endregion Свойства

    #region Публичные методы
    // --- НОВЫЙ МЕТОД ВАЛИДАЦИИ ---
    [Button("Проверить корректность данных", ButtonSizes.Large), GUIColor(1f, 0.8f, 0.2f)]
    [BoxGroup("Batch Operations")]
    public void ValidateProgressionData()
    {
        if (_levels == null || _levels.Count == 0)
        {
            Debug.LogError($"[Validation] '{this.name}': Список уровней пуст!", this);
            return;
        }

        bool hasErrors = false;
        for (int i = 0; i < _levels.Count; i++)
        {
            var level = _levels[i];
            if (level.availableNormalEnemies == null || level.availableNormalEnemies.Count == 0 || level.availableNormalEnemies.Any(e => e == null))
            {
                Debug.LogError($"[Validation] '{this.name}': Уровень {i + 1} не имеет назначенных обычных врагов (availableNormalEnemies) или содержит пустые элементы!", this);
                hasErrors = true;
            }
        }

        if (_finalBoss == null && _levels.Count > 0)
        {
            Debug.LogError($"[Validation] '{this.name}': Не назначен финальный босс.", this);
        }

        if (!hasErrors)
        {
            Debug.Log($"<color=green>[Validation] '{this.name}': Проверка успешно пройдена! Критических ошибок в данных не найдено.</color>", this);
        }
    }

    [Button("Добавить обычного врага", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    [BoxGroup("Batch Operations")]
    public void AddNormalEnemyToLevels()
    {
        AddEnemyToLevels(_normalEnemyToAdd, _minLevelToAddNormalEnemy, false);
    }

    [Button("Добавить мини-босса", ButtonSizes.Large), GUIColor(1f, 0.6f, 0.4f)]
    [BoxGroup("Batch Operations")]
    public void AddMiniBossToLevels()
    {
        AddEnemyToLevels(_miniBossToAdd, _minLevelToAddMiniBoss, true);
    }

    [OnInspectorInit]
    public void OnInspectorInit()
    {
        UpdateLevelSettings();
        ColoredDebug.CLog(null, "<color=cyan>LevelProgression:</color> Инспектор инициализирован. Номера уровней обновлены.", _ColoredDebug);
    }

    [Button("Исправить номера уровней", ButtonSizes.Medium), GUIColor(0.8f, 0.8f, 0.8f)]
    [BoxGroup("Batch Operations")]
    public void FixLevelNumbers()
    {
        UpdateLevelSettings();
        ColoredDebug.CLog(null, "<color=cyan>LevelProgression:</color> Номера уровней исправлены.", _ColoredDebug);
    }
    #endregion Публичные методы

    #region Личные методы
    private void UpdateLevelSettings()
    {
        ColoredDebug.CLog(null, "<color=cyan>LevelProgression:</color> Обновляю настройки уровней...", _ColoredDebug);
        for (int i = 0; i < _levels.Count; i++)
        {
            if (_levels[i] != null)
            {
                _levels[i].levelNumber = i + 1;
                _levels[i].backgroundVariant = (BackgroundVariant)(i % 3);
                ColoredDebug.CLog(null, $"<color=cyan>LevelProgression:</color> Уровень <color=yellow>{_levels[i].levelNumber}</color> установлен. Вариант фона: <color=lime>{_levels[i].backgroundVariant}</color>.", _ColoredDebug);
            }
        }
        ColoredDebug.CLog(null, $"<color=cyan>LevelProgression:</color> Настройки для <color=yellow>{_levels.Count}</color> уровней обновлены.", _ColoredDebug);
    }

    private void AddEnemyToLevels(EnemySO enemy, int minLevel, bool isMiniBoss)
    {
        ColoredDebug.CLog(null, $"<color=cyan>LevelProgression:</color> Запускаю массовое добавление врагов. Враг: <color=yellow>{(enemy != null ? enemy.name : "NONE")}</color>, Начиная с уровня: <color=yellow>{minLevel}</color>.", _ColoredDebug);
        if (enemy == null)
        {
            ColoredDebug.CLog(null, "<color=red>LevelProgression:</color> Не выбран противник для добавления. Операция отменена.", _ColoredDebug);
            return;
        }

        int levelsAffected = 0;
        foreach (var level in _levels)
        {
            if (level.levelNumber >= minLevel)
            {
                var targetList = isMiniBoss ? level.availableMiniBosses : level.availableNormalEnemies;
                if (!targetList.Contains(enemy))
                {
                    targetList.Add(enemy);
                    levelsAffected++;
                    ColoredDebug.CLog(null, $"<color=cyan>LevelProgression:</color> Добавлен <color=yellow>{enemy.name}</color> в уровень <color=lime>{level.levelNumber}</color>.", _ColoredDebug);
                }
                else
                {
                    ColoredDebug.CLog(null, $"<color=cyan>LevelProgression:</color> <color=yellow>{enemy.name}</color> уже существует в уровне <color=lime>{level.levelNumber}</color>. Пропускаю.", _ColoredDebug);
                }
            }
        }

        string enemyType = isMiniBoss ? "Мини-босс" : "Противник";
        ColoredDebug.CLog(null, $"<color=lime>LevelProgression:</color> {enemyType} '{enemy.name}' добавлен в <color=yellow>{levelsAffected}</color> уровней, начиная с уровня <color=yellow>{minLevel}</color>.", _ColoredDebug);
    }
    #endregion Личные методы

#if UNITY_EDITOR
    #region Data Management
    [BoxGroup("Data Management", Order = 2), Button("SaveData", ButtonSizes.Large), GUIColor(0.2f, 0.8f, 0.2f)]
    private void SaveData()
    {
        string assetPath = AssetDatabase.GetAssetPath(this);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError("Не удалось получить путь к ассету. Убедитесь, что это сохраненный ассет.");
            return;
        }
        string txtPath = Path.ChangeExtension(assetPath, ".txt");
        try
        {
            string jsonData = JsonUtility.ToJson(this, true);
            File.WriteAllText(txtPath, jsonData);
            Debug.Log($"<color=green>Данные из '{this.name}' успешно сохранены в файл:</color> {txtPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка при сохранении данных: {e.Message}");
        }
    }

    [BoxGroup("Data Management"), Button("LoadData", ButtonSizes.Large), GUIColor(1f, 0.6f, 0.2f)]
    private void LoadData()
    {
        string assetPath = AssetDatabase.GetAssetPath(this);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError("Не удалось получить путь к ассету. Убедитесь, что это сохраненный ассет.");
            return;
        }

        string txtPath = Path.ChangeExtension(assetPath, ".txt");

        if (!File.Exists(txtPath))
        {
            Debug.LogError($"Файл для загрузки не найден: {txtPath}");
            return;
        }

        try
        {
            string jsonData = File.ReadAllText(txtPath);
            JsonUtility.FromJsonOverwrite(jsonData, this);
            UpdateLevelSettings();

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=green>Данные успешно загружены из файла '{Path.GetFileName(txtPath)}' в ассет '{this.name}'.</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка при загрузке данных: {e.Message}");
        }
    }
    #endregion
#endif
}