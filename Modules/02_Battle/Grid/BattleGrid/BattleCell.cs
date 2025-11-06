// НАЗНАЧЕНИЕ: Представляет одну ячейку на боевой сетке.
// Управляет своим логическим состоянием (тип, занята, повреждена) и передает команды на изменение визуального состояния связанному BridgeCellElement.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BridgeManager, BridgeGrid, BridgeCellElement.
// ПРИМЕЧАНИЕ: Является "контроллером", который не имеет собственных визуальных эффектов, а делегирует их физическому представлению.
// Может быть разных типов (Standard, Indestructible, Glass).

using Sirenix.OdinInspector;
using UnityEngine;

public class BattleCell : MonoBehaviour
{
    public enum CellState { Intact, Cracked, Hole }
    public enum CellType { Standard, Indestructible, Glass }

    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Vector2Int _pos;
    ///Координаты клетки.
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private SpriteRenderer _spriteRenderer; ///Визуальное представление (для отладки/цвета).
    #endregion

    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private object _occupant;
    ///Объект, занимающий клетку (BattleUnit, Prop).
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private CellState _currentState = CellState.Intact; ///Текущее состояние (целая, треснутая, дыра).
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private Color _originalColor; ///Исходный цвет (для восстановления).
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private bool _isDeactivated = false;
    ///Является ли клетка "пропастью".
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private CellType _cellType = CellType.Standard; ///Тип клетки.
    #endregion

    #region Свойства
    public Vector2Int Position => _pos;
    public CellState CurrentState => _currentState;
    public object Occupant => _occupant;
    public bool IsPassable => _currentState != CellState.Hole && !_isDeactivated;
    public Vector3 WorldPosition => transform.position;
    public SpriteRenderer SpriteRenderer => _spriteRenderer;
    public bool IsDeactivated => _isDeactivated;
    public CellType Type => _cellType;
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (_spriteRenderer == null) DebugUtils.LogMissingReference(this, nameof(_spriteRenderer));
        if (_spriteRenderer != null)
        {
            _originalColor = _spriteRenderer.color;
        }
    }
    #endregion

    #region Публичные методы
    /// <summary> Инициализирует ячейку.
    /// </summary>
    public void Init(Vector2Int pos, Vector3 worldPos, CellType type)
    {
        _pos = pos;
        transform.position = worldPos;
        _cellType = type;
        ColoredDebug.CLog(gameObject, "<color=cyan>CELL ({0}):</color> Init. Type: <color=lime>{1}</color>.", _ColoredDebug, _pos, _cellType);
        if (_spriteRenderer != null)
        {
            _originalColor = _spriteRenderer.color;
        }
        UpdatePhysicalCellVisuals();
    }

    /// <summary> Наносит урон ячейке.
    /// </summary>
    public void TakeDamage()
    {
        if (_currentState == CellState.Hole || _isDeactivated) return;
        CellState previousState = _currentState;

        switch (_cellType)
        {
            case CellType.Standard:
                _currentState++;
                ColoredDebug.CLog(gameObject, "<color=orange>CELL ({0}) [Standard]:</color> Took damage. State: <color=orange>{1}</color> -> <color=red>{2}</color>.", _ColoredDebug, _pos, previousState, _currentState);
                break;
            case CellType.Glass:
                _currentState = CellState.Hole;
                ColoredDebug.CLog(gameObject, "<color=orange>CELL ({0}) [Glass]:</color> Took damage. State: <color=orange>{1}</color> -> <color=red>{2}</color>.", _ColoredDebug, _pos, previousState, _currentState);
                break;
            case CellType.Indestructible:
                ColoredDebug.CLog(gameObject, "<color=grey>CELL ({0}) [Indestructible]:</color> Took damage, but state remains <color=grey>{1}</color>.", _ColoredDebug, _pos, _currentState);
                return;
        }

        if (_currentState == CellState.Hole)
        {
            if (_occupant != null)
            {
                string occupantName = (_occupant as Component)?.gameObject.name ??
_occupant.GetType().Name;
                ColoredDebug.CLog(gameObject, "<color=red>CELL ({0}):</color> Became a HOLE while occupied by {1}!", _ColoredDebug, _pos, occupantName);
                SetOccupant(null);
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.white;
                ColoredDebug.CLog(gameObject, "<color=red>CELL ({0}):</color> Клетка стала дырой. Цвет установлен на БЕЛЫЙ.", _ColoredDebug, _pos);
            }
        }

        UpdatePhysicalCellVisuals();
    }

    /// <summary> Сбрасывает состояние ячейки к исходному.
    /// </summary>
    public void ResetState()
    {
        bool wasOccupied = _occupant != null;
        CellState previousState = _currentState;
        bool wasDeactivated = _isDeactivated;

        _currentState = CellState.Intact;
        _occupant = null;
        _isDeactivated = false;
        gameObject.SetActive(true);
        if (_spriteRenderer != null)
        {
            _spriteRenderer.enabled = true;
            _spriteRenderer.color = _originalColor;
        }

        if (wasOccupied || previousState != CellState.Intact || wasDeactivated)
        {
            ColoredDebug.CLog(gameObject, "<color=yellow>CELL ({0}):</color> State reset to Intact/Empty (Type: {1}).", _ColoredDebug, _pos, _cellType);
        }

        ShowPhysicalCellVisuals();
        UpdatePhysicalCellVisuals();
    }

    /// <summary> Деактивирует ячейку ("пропасть").
    /// </summary>
    public void Deactivate()
    {
        if (_isDeactivated) return;
        _isDeactivated = true;
        _currentState = CellState.Hole;
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = Color.white;
            _spriteRenderer.enabled = false;
        }

        if (_occupant != null)
        {
            string occupantName = (_occupant as Component)?.gameObject.name ??
_occupant.GetType().Name;
            ColoredDebug.CLog(gameObject, "<color=red>CELL ({0}):</color> Deactivated (Abyss) while occupied by {1}!", _ColoredDebug, _pos, occupantName);
            _occupant = null;
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=red>CELL ({0}):</color> Deactivated (Abyss).", _ColoredDebug, _pos);
        }

        HidePhysicalCellVisuals();
    }

    /// <summary> Устанавливает или снимает "оккупанта".
    /// </summary>
    public void SetOccupant(object occupant)
    {
        string occupantName = "NULL";
        if (occupant == _occupant)
            return;

        object previousOccupant = _occupant;
        _occupant = occupant;

        if (_spriteRenderer != null && !_isDeactivated) // Не меняем цвет, если это пропасть
        {
            if (_occupant != null)
            {
                _spriteRenderer.color = Color.white;
                ColoredDebug.CLog(gameObject, "<color=grey>CELL ({0}):</color> Клетка занята. Цвет установлен на БЕЛЫЙ.", _ColoredDebug, _pos);
            }
            else if (_currentState != CellState.Hole) // Не восстанавливаем цвет, если это дыра
            {
                _spriteRenderer.color = _originalColor;
                ColoredDebug.CLog(gameObject, "<color=grey>CELL ({0}):</color> Клетка освобождена. Цвет восстановлен.", _ColoredDebug, _pos);
            }
        }

        if (_occupant is BattleUnit bu) occupantName = bu.name;
        else if (_occupant != null) occupantName = (_occupant as Component)?.gameObject.name ?? _occupant.GetType().Name;
        string previousOccupantName = "NULL";
        if (previousOccupant is BattleUnit pbu) previousOccupantName = pbu.name;
        else if (previousOccupant != null) previousOccupantName = (previousOccupant as Component)?.gameObject.name ?? previousOccupant.GetType().Name;
        ColoredDebug.CLog(gameObject, "<color=#ADD8E6>CELL ({0}):</color> SetOccupant. From <color=orange>{1}</color> To <color=lime>{2}</color>.", _ColoredDebug, _pos, previousOccupantName, occupantName);
    }

    /// <summary> Восстанавливает клетку из состояния дыры.
    /// </summary>
    public void RepairCell(CellType newType)
    {
        if (_currentState != CellState.Hole || _isDeactivated)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>CELL ({0}):</color> Попытка починить не дыру или деактивированную клетку.", _ColoredDebug, _pos);
            return;
        }
        _currentState = CellState.Intact;
        _cellType = newType;

        BridgeGrid activeBridge = BridgeManager.Instance?.ActiveBridge;
        if (activeBridge != null)
        {
            BridgeCellElement newElement = activeBridge.CreateOrReplacePhysicalCell(_pos, _cellType);
            if (newElement != null)
            {
                newElement.UpdateVisualState(_currentState);
                ColoredDebug.CLog(gameObject, "<color=lime>CELL ({0}):</color> Клетка восстановлена. Физический элемент заменен на тип {1}.", _ColoredDebug, _pos, _cellType);
            }
            else
            {
                ColoredDebug.CLog(gameObject, "<color=red>CELL ({0}):</color> Ошибка при замене физического элемента во время починки!", _ColoredDebug, _pos);
            }
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=red>CELL ({0}):</color> Не найден активный мост для замены физического элемента!", _ColoredDebug, _pos);
        }

        if (_spriteRenderer != null && BattleGridGenerator.Instance != null)
        {
            Color newColor = BattleGridGenerator.Instance.GetColorForCell(_pos);
            _spriteRenderer.color = newColor;
            _originalColor = newColor; // Обновляем оригинальный цвет
            ColoredDebug.CLog(gameObject, "<color=lime>CELL ({0}):</color> Цвет клетки восстановлен по градиенту.", _ColoredDebug, _pos);
        }
    }

    /// <summary> "Строит" клетку на месте пропасти.
    /// </summary>
    public void BuildCell(CellType newType)
    {
        if (!_isDeactivated)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>CELL ({0}):</color> Попытка построить на не деактивированной клетке.", _ColoredDebug, _pos);
            return;
        }
        _isDeactivated = false;
        _currentState = CellState.Intact;
        _cellType = newType;
        BridgeGrid activeBridge = BridgeManager.Instance?.ActiveBridge;
        if (activeBridge != null)
        {
            BridgeCellElement newElement = activeBridge.CreateOrReplacePhysicalCell(_pos, _cellType);
            if (newElement != null)
            {
                newElement.UpdateVisualState(_currentState);
                ColoredDebug.CLog(gameObject, "<color=lime>CELL ({0}):</color> Клетка построена (восстановлена из пропасти). Физический элемент создан с типом {1}.", _ColoredDebug, _pos, _cellType);
            }
            else
            {
                ColoredDebug.CLog(gameObject, "<color=red>CELL ({0}):</color> Ошибка при создании физического элемента во время строительства!", _ColoredDebug, _pos);
            }
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=red>CELL ({0}):</color> Не найден активный мост для создания физического элемента!", _ColoredDebug, _pos);
        }

        if (_spriteRenderer != null)
        {
            _spriteRenderer.enabled = true;
            if (BattleGridGenerator.Instance != null)
            {
                Color newColor = BattleGridGenerator.Instance.GetColorForCell(_pos);
                _spriteRenderer.color = newColor;
                _originalColor = newColor; // Обновляем оригинальный цвет
                ColoredDebug.CLog(gameObject, "<color=lime>CELL ({0}):</color> Цвет новой клетки установлен по градиенту.", _ColoredDebug, _pos);
            }
        }
    }

    public bool IsEmpty() => _occupant == null && IsPassable;
    public bool IsOccupied() => _occupant != null;

    public void SetColor(Color color)
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = color;
            _originalColor = color;
        }
    }
    #endregion

    #region Личные методы
    private void UpdatePhysicalCellVisuals()
    {
        if (_isDeactivated) return;
        BridgeGrid activeBridge = BridgeManager.Instance?.ActiveBridge;
        if (activeBridge == null) return;

        BridgeCellElement physicalCell = activeBridge.GetPhysicalCell(_pos);
        if (physicalCell != null)
        {
            physicalCell.UpdateVisualState(_currentState);
        }
        else if (_currentState != CellState.Hole && !_isDeactivated)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>CELL ({0}):</color> Не найден физический элемент для обновления визуала (Состояние: {1}, Тип: {2}).", _ColoredDebug, _pos, _currentState, _cellType);
        }
    }

    private void HidePhysicalCellVisuals()
    {
        BridgeGrid activeBridge = BridgeManager.Instance?.ActiveBridge;
        if (activeBridge == null) return;

        BridgeCellElement physicalCell = activeBridge.GetPhysicalCell(_pos);
        if (physicalCell != null && physicalCell.gameObject.activeSelf)
        {
            physicalCell.gameObject.SetActive(false);
            ColoredDebug.CLog(gameObject, "<color=grey>CELL ({0}):</color> Physical cell hidden.", _ColoredDebug, _pos);
        }
    }

    private void ShowPhysicalCellVisuals()
    {
        BridgeGrid activeBridge = BridgeManager.Instance?.ActiveBridge;
        if (activeBridge == null) return;

        BridgeCellElement physicalCell = activeBridge.GetPhysicalCell(_pos);
        if (physicalCell != null && !physicalCell.gameObject.activeSelf)
        {
            physicalCell.gameObject.SetActive(true);
            ColoredDebug.CLog(gameObject, "<color=grey>CELL ({0}):</color> Physical cell shown.", _ColoredDebug, _pos);
        }
        else if (physicalCell == null && !_isDeactivated)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>CELL ({0}):</color> Не найден физический элемент для показа (Состояние: {1}, Тип: {2}).", _ColoredDebug, _pos, _currentState, _cellType);
        }
    }
    #endregion
}