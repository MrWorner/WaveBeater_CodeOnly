// НАЗНАЧЕНИЕ: Управляет созданием боевых арен, переключаясь между случайной генерацией и применением предустановленных шаблонов, включая типы клеток.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: ArenaTemplateSO, StageFlowManager, BattleGridGenerator, BridgeManager, BattleGridPropManager, ScenarioManager.
// ПРИМЕЧАНИЕ: Является синглтоном и центральной точкой для всей логики генерации арен.
// Может переопределяться системой сценариев.
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ArenaManager : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleGridGenerator _battleGridGenerator;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BridgeManager _bridgeManager;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleGridPropManager _propManager;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), Tooltip("Если true, будет использоваться полностью случайная генерация. Если false - случайный шаблон из списка ниже."), SerializeField]
    private bool _useRandomGenerator = false;
    [BoxGroup("SETTINGS"), HideIf("_useRandomGenerator"), Tooltip("Список доступных шаблонов арен для случайного выбора."), SerializeField]
    private List<ArenaTemplateSO> _arenaTemplates = new List<ArenaTemplateSO>();
    [BoxGroup("SETTINGS/Random Generation"), ShowIf("_useRandomGenerator"), SerializeField, MinValue(5)] private int _randomMinWidth = 5;
    [BoxGroup("SETTINGS/Random Generation"), ShowIf("_useRandomGenerator"), SerializeField, MinValue(10)] private int _randomMaxWidth = 16;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    private static ArenaManager _instance;
    #endregion Поля

    #region Свойства
    public static ArenaManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ArenaManager>();
            }
            return _instance;
        }
    }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            DebugUtils.LogInstanceAlreadyExists(this, _instance);
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (_battleGridGenerator == null) DebugUtils.LogMissingReference(this, nameof(_battleGridGenerator));
        if (_bridgeManager == null) DebugUtils.LogMissingReference(this, nameof(_bridgeManager));
        if (_propManager == null) DebugUtils.LogMissingReference(this, nameof(_propManager));
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Генерирует новую арену в соответствии с текущими настройками (сценарий, шаблон или случайная).
    /// </summary>
    [Button("Сгенерировать Арену", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.4f)]
    [BoxGroup("ACTIONS", ShowLabel = false)]
    public void GenerateArena()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>ArenaManager:</color> Начало генерации новой арены...", _ColoredDebug);
        _propManager.RetireActiveProps();

        if (Settings.EnableScenarioMode && ScenarioManager.Instance != null)
        {
            var scenario = ScenarioManager.Instance.CurrentScenario;
            if (scenario != null && scenario.arenaTemplate != null)
            {
                ColoredDebug.CLog(gameObject, "<color=purple>ArenaManager:</color> РЕЖИМ СЦЕНАРИЯ АКТИВЕН. Применение шаблона: <color=yellow>{0}</color>.", _ColoredDebug, scenario.arenaTemplate.name);
                ApplyTemplate(scenario.arenaTemplate);
                return;
            }
            else
            {
                ColoredDebug.CLog(gameObject, "<color=orange>ArenaManager:</color> Режим сценария активен, но текущий сценарий или его шаблон не найдены. Переход к стандартной генерации.", _ColoredDebug);
            }
        }

        if (_useRandomGenerator)
        {
            GenerateRandomArena();
        }
        else
        {
            if (_arenaTemplates == null || _arenaTemplates.Count == 0)
            {
                Debug.LogError("[ArenaManager] Включено использование шаблонов, но список _arenaTemplates пуст! Переход к случайной генерации.");
                GenerateRandomArena();
                return;
            }

            ArenaTemplateSO selectedTemplate = _arenaTemplates[Random.Range(0, _arenaTemplates.Count)];
            ApplyTemplate(selectedTemplate);
        }
    }
    #endregion Публичные методы

    #region Личные методы
    /// <summary>
    /// Применяет данные из шаблона для построения арены, включая типы клеток.
    /// </summary>
    /// <param name="template">Шаблон для применения.</param>
    private void ApplyTemplate(ArenaTemplateSO template)
    {
        if (template == null)
        {
            Debug.LogError("[ArenaManager] Попытка применить пустой (null) шаблон!");
            return;
        }

        ColoredDebug.CLog(gameObject, "<color=cyan>ArenaManager:</color> Применение шаблона: <color=yellow>{0}</color>.", _ColoredDebug, template.name);
        var cellTypeMap = new Dictionary<Vector2Int, BattleCell.CellType>();
        foreach (var typeData in template.CellTypes)
        {
            cellTypeMap[typeData.Position] = typeData.Type;
        }
        ColoredDebug.CLog(gameObject, "<color=cyan>ArenaManager:</color> Создана карта типов клеток (<color=yellow>{0}</color> записей).", _ColoredDebug, cellTypeMap.Count);
        var missingCellsList = template.MissingCells.ToList();
        _battleGridGenerator.GenerateNewGrid(template.Width, template.Height, missingCellsList, cellTypeMap);
        _bridgeManager.GenerateNextBridge(template.Width, template.Height, missingCellsList, cellTypeMap);
        foreach (var missingCellPos in template.MissingCells)
        {
            BattleGrid.Instance.DeactivateCell(missingCellPos);
        }

        foreach (var cellData in template.CellStates)
        {
            BattleCell cell = BattleGrid.Instance.Grid[cellData.Position.x, cellData.Position.y];
            if (cell != null)
            {
                if (cell.Type != BattleCell.CellType.Indestructible)
                {
                    System.Reflection.FieldInfo stateField = typeof(BattleCell).GetField("_currentState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (stateField != null)
                    {
                        stateField.SetValue(cell, cellData.State);
                        System.Reflection.MethodInfo updateVisualsMethod = typeof(BattleCell).GetMethod("UpdatePhysicalCellVisuals", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        updateVisualsMethod?.Invoke(cell, null);

                        if (cellData.State == BattleCell.CellState.Hole && cell.SpriteRenderer != null)
                        {
                            cell.SpriteRenderer.color = Color.white;
                            ColoredDebug.CLog(gameObject, "<color=cyan>ArenaManager:</color> Установлен белый цвет для клетки-дыры <color=lime>{0}</color>.", _ColoredDebug, cellData.Position);
                        }

                        ColoredDebug.CLog(gameObject, "<color=cyan>ArenaManager:</color> Установлено состояние <color=yellow>{0}</color> для клетки <color=lime>{1}</color>.", _ColoredDebug, cellData.State, cellData.Position);
                    }
                    else
                    {
                        Debug.LogError($"[ArenaManager] Не удалось получить доступ к полю _currentState через рефлексию!");
                    }
                }
                else
                {
                    ColoredDebug.CLog(gameObject, "<color=orange>ArenaManager:</color> Пропуск установки состояния для Indestructible клетки <color=lime>{0}</color>.", _ColoredDebug, cellData.Position);
                }
            }
            else
            {
                ColoredDebug.CLog(gameObject, "<color=orange>ArenaManager:</color> Не найдена клетка <color=lime>{0}</color> для установки состояния.", _ColoredDebug, cellData.Position);
            }
        }

        foreach (var propData in template.PropPlacements)
        {
            _propManager.PlacePropAt(propData.Prop, propData.Position);
        }
    }

    /// <summary>
    /// Генерирует полностью случайную арену (только со стандартными клетками).
    /// </summary>
    private void GenerateRandomArena()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>ArenaManager:</color> Генерация полностью случайной арены.", _ColoredDebug);
        int newWidth = Random.Range(_randomMinWidth, _randomMaxWidth + 1);
        int newHeight = 4;
        _battleGridGenerator.GenerateNewGrid(newWidth, newHeight, new List<Vector2Int>(), null);
        // *** NO CHANGE NEEDED HERE *** The call below now matches the fixed BridgeManager signature
        _bridgeManager.GenerateNextBridge(newWidth, newHeight, new List<Vector2Int>(), null);
        _propManager.GenerateAndPlaceProps();
    }
    #endregion Личные методы
}