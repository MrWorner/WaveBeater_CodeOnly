// НАЗНАЧЕНИЕ: Управляет переключением между двумя экземплярами BridgeGrid (A и B) и их генерацией при смене этапов.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: StageManager, BridgeGrid, BridgeGenerator, BridgeMaskGenerator.
// ПРИМЕЧАНИЕ: Является центральной точкой доступа к активному мосту через Singleton.

using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

public class BridgeManager : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private BridgeGrid _bridgeA;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private BridgeGrid _bridgeB;
    [Title("Генераторы")]
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private BridgeGenerator _bridgeGeneratorA;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private BridgeGenerator _bridgeGeneratorB;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private BridgeMaskGenerator _bridgeMaskGeneratorA;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private BridgeMaskGenerator _bridgeMaskGeneratorB;
    #endregion

    #region Поля
    [BoxGroup("DEBUG"), SerializeField]
    private bool _ColoredDebug;
    private static BridgeManager _instance;
    #endregion

    #region Свойства
    /// <summary>
    /// Статический экземпляр для глобального доступа к менеджеру мостов.
    /// </summary>
    public static BridgeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<BridgeManager>();
            }
            return _instance;
        }
    }

    /// <summary>
    /// Возвращает текущий активный экземпляр BridgeGrid.
    /// </summary>
    public BridgeGrid ActiveBridge { get; private set; }
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null)
        {
            DebugUtils.LogInstanceAlreadyExists(this, _instance);
            Destroy(gameObject);
            return;
        }
        _instance = this;
        if (_bridgeA == null) DebugUtils.LogMissingReference(this, nameof(_bridgeA));
        if (_bridgeB == null) DebugUtils.LogMissingReference(this, nameof(_bridgeB));
        if (_bridgeGeneratorA == null) DebugUtils.LogMissingReference(this, nameof(_bridgeGeneratorA));
        if (_bridgeGeneratorB == null) DebugUtils.LogMissingReference(this, nameof(_bridgeGeneratorB));
        if (_bridgeMaskGeneratorA == null) DebugUtils.LogMissingReference(this, nameof(_bridgeMaskGeneratorA));
        if (_bridgeMaskGeneratorB == null) DebugUtils.LogMissingReference(this, nameof(_bridgeMaskGeneratorB));

        if (_bridgeA.Set != BridgeGrid.BridgeSet.A) Debug.LogError($"[BridgeManager] _bridgeA должен иметь сет 'A', а у него '{_bridgeA.Set}'!", _bridgeA);
        if (_bridgeB.Set != BridgeGrid.BridgeSet.B) Debug.LogError($"[BridgeManager] _bridgeB должен иметь сет 'B', а у него '{_bridgeB.Set}'!", _bridgeB);
        ActiveBridge = _bridgeA;
    }
    private void Start()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageChanged += OnStageChanged;
            ColoredDebug.CLog(gameObject, "<color=olive>BridgeManager:</color> <color=green>Успешно подписался</color> на событие OnStageChanged.", _ColoredDebug);
        }
        else
        {
            Debug.LogError("[BridgeManager] StageManager не найден!");
        }
    }
    #endregion

    #region Публичные методы
    /// <summary>
    /// Генерирует мост и маску для СЛЕДУЮЩЕГО этапа, который в данный момент неактивен.
    /// </summary>
    /// <param name="width">Новая ширина.</param>
    /// <param name="height">Новая высота.</param>
    /// <param name="cellsToSkip">Список координат клеток, которые не нужно создавать.</param>
    public void GenerateNextBridge(int width, int height, List<Vector2Int> cellsToSkip, Dictionary<Vector2Int, BattleCell.CellType> cellTypeMap)
    {
        ActiveBridge.ResetAllPhysicalCells();
        BridgeGenerator nextGenerator = (ActiveBridge == _bridgeA) ? _bridgeGeneratorA : _bridgeGeneratorB;
        BridgeMaskGenerator nextMaskGenerator = (ActiveBridge == _bridgeA) ? _bridgeMaskGeneratorA : _bridgeMaskGeneratorB;
        string nextBridgeName = (ActiveBridge == _bridgeA) ? "A" : "B";

        ColoredDebug.CLog(gameObject, $"<color=olive>BridgeManager:</color> Запуск генерации для следующего моста <color=yellow>'{nextBridgeName}'</color> с размерами <color=lime>{width}x{height}</color>.", _ColoredDebug);
        nextGenerator.GenerateNewBridge(width, height, cellsToSkip, cellTypeMap);
        nextMaskGenerator.GenerateNewMask(width, 15, cellsToSkip);
    }
    #endregion

    #region Личные методы

    /// <summary>
    /// Обработчик события смены этапа. Переключает активный мост.
    /// </summary>
    private void OnStageChanged(StageType newStageType)
    {
        ActiveBridge = (ActiveBridge == _bridgeA) ? _bridgeB : _bridgeA;
        BridgeMaskGenerator nextMaskGenerator = (ActiveBridge == _bridgeA) ? _bridgeMaskGeneratorA : _bridgeMaskGeneratorB;
        switch (newStageType)
        {
            case StageType.Battle:
            case StageType.Horde:
            case StageType.MiniBoss:
            case StageType.BossFight:
            case StageType.HighLevelBattle:
            case StageType.MixedBattle:
            case StageType.DoubleMiniBoss:
            case StageType.TripleMiniBoss:
                ActiveBridge.gameObject.SetActive(true);
                nextMaskGenerator.gameObject.SetActive(true);
                break;
            default:
                ActiveBridge.gameObject.SetActive(false);
                nextMaskGenerator.gameObject.SetActive(false);
                break;
        }

        ColoredDebug.CLog(gameObject, $"<color=olive>BridgeManager:</color> Активный мост переключен на <color=yellow>{ActiveBridge.Set}</color>.", _ColoredDebug);
    }
    #endregion
}