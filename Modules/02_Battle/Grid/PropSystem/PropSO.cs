using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "PropSO_New", menuName = "WaveBeater/Prop SO")]
public class PropSO : ScriptableObject
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField, PreviewField(75)]
    private GameObject _propPrefab;
    #endregion

    #region Поля
    [BoxGroup("SETTINGS"), InfoBox("Размер пропа (Ширина x Высота). 'Якорь' находится в левой нижней клетке."), SerializeField]
    private Vector2Int _propSize = Vector2Int.one;
    [BoxGroup("SETTINGS"), Tooltip("Можно ли проходить через этот объект? Если нет, клетки под ним будут заблокированы."), SerializeField]
    private bool _isPassable = false;
    [BoxGroup("SETTINGS"), Tooltip("Можно ли перепрыгнуть через этот объект?"), SerializeField]
    private bool _isJumpable = true; // Default to true for backward compatibility
    [BoxGroup("SETTINGS"), Tooltip("Можно ли уничтожить этот объект?"), SerializeField]
    private bool _isDestructible = false;
    [BoxGroup("SETTINGS"), ShowIf("_isDestructible"), Tooltip("Максимальное здоровье пропа."), SerializeField, MinValue(1)]
    private int _maxHealth = 1;
    [BoxGroup("DEBUG"), SerializeField]
    protected bool _ColoredDebug;
    #endregion

    #region Свойства
    /// <summary> Префаб игрового объекта пропа. </summary>
    public GameObject PropPrefab => _propPrefab; 
    /// <summary> Размер, который проп занимает на сетке (в клетках). </summary>
    public Vector2Int PropSize => _propSize;
    /// <summary> Определяет, можно ли проходить через клетки, занятые этим пропом. </summary>
    public bool IsPassable => _isPassable;
    /// <summary> Определяет, можно ли перепрыгнуть через этот проп. </summary>
    public bool IsJumpable => _isJumpable; // New Property
    /// <summary> Определяет, можно ли уничтожить этот проп. </summary>
    public bool IsDestructible => _isDestructible; 
    /// <summary> Максимальное здоровье пропа (если он разрушаемый). </summary>
    public int MaxHealth => _maxHealth; // New Property
    #endregion
}