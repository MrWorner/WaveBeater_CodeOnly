using Sirenix.OdinInspector;
using UnityEngine;

public class Prop : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private PropSO _propSO; 
    [PropertyOrder(-1), BoxGroup("Required"), SerializeField] private PropHealth _propHealth;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug; 
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private Vector2Int _anchorPosition;
    #endregion Поля

    #region Свойства
    public PropSO PropSO => _propSO; 
    public Vector2Int AnchorPosition => _anchorPosition; 
    public PropHealth PropHealth => _propHealth;
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_propSO != null && _propSO.IsDestructible)
        {
            if (_propHealth == null) _propHealth = GetComponent<PropHealth>();
            if (_propHealth == null)
            {
                Debug.LogWarning($"[Prop] Prop '{_propSO.name}' is marked as Destructible but is missing the PropHealth component! Adding it automatically.", gameObject);
                _propHealth = gameObject.AddComponent<PropHealth>();
            }
        }
    }
    #endregion

    #region Публичные методы
    /// <summary>
    /// Устанавливает якорную позицию пропа на сетке.
    /// Вызывается из BattleGridPropManager при создании.
    /// </summary>
    public void SetAnchorPosition(Vector2Int position)
    {
        _anchorPosition = position;
    }
    #endregion Публичные методы
}