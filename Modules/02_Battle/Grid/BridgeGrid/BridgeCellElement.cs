// НАЗНАЧЕНИЕ: Представляет один физический, разрушаемый элемент моста.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: BattleCell (для enum'а CellState).
// ПРИМЕЧАНИЕ: Является "представлением" (View), которое получает команды от BattleCell (Controller) для изменения своего визуального состояния. Разные типы клеток (Standard, Glass, Indestructible) реализуются через разные префабы с этим компонентом.
using UnityEngine;
using Sirenix.OdinInspector;

public class BridgeCellElement : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), SerializeField] private GameObject _crackedEffect;
    [PropertyOrder(-1), BoxGroup("Required"), SerializeField] private GameObject _holeEffect;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _mainCellVisual;
    #endregion

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private Vector2Int _position;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion

    #region Свойства
    public Vector2Int Position => _position;
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (_mainCellVisual == null) DebugUtils.LogMissingReference(this, nameof(_mainCellVisual));
        if (_crackedEffect) _crackedEffect.SetActive(false);
        if (_holeEffect) _holeEffect.SetActive(false);
    }
    #endregion

    #region Публичные методы
    /// <summary>
    /// Инициализирует позицию физической ячейки.
    /// </summary>
    public void Init(Vector2Int pos)
    {
        _position = pos;
        ColoredDebug.CLog(gameObject, "<color=cyan>BridgeElement ({0}):</color> Init.", _ColoredDebug, _position);
    }

    /// <summary>
    /// Обновляет визуальное состояние ячейки в зависимости от логического состояния BattleCell.
    /// </summary>
    public void UpdateVisualState(BattleCell.CellState state)
    {
        bool isCracked = state == BattleCell.CellState.Cracked;
        bool isHole = state == BattleCell.CellState.Hole;

        if (_crackedEffect != null) _crackedEffect.SetActive(isCracked);
        if (_holeEffect != null) _holeEffect.SetActive(isHole);
        //if (_mainCellVisual != null) _mainCellVisual.SetActive(!isHole); // Скрываем основную ячейку, когда она становится дырой 

        ColoredDebug.CLog(gameObject, "<color=grey>BridgeElement ({0}):</color> Visual state updated to {1}.", _ColoredDebug, _position, state);
    }

    /// <summary>
    /// Сбрасывает визуальное состояние ячейки до исходного (целая).
    /// </summary>
    public void ResetState()
    {
        if (_crackedEffect != null) _crackedEffect.SetActive(false);
        if (_holeEffect != null) _holeEffect.SetActive(false);
        if (_mainCellVisual != null) _mainCellVisual.SetActive(true);
        ColoredDebug.CLog(gameObject, "<color=yellow>BridgeElement ({0}):</color> Visual state reset.", _ColoredDebug, _position);
    }
    #endregion
}