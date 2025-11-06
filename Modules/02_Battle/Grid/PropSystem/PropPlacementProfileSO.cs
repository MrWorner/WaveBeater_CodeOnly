// НАЗНАЧЕНИЕ: ScriptableObject для определения правил и вероятностей появления пропов (объектов окружения) на боевой сетке.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: PropSO.
// ПРИМЕЧАНИЕ: Позволяет создавать различные наборы правил для разных типов уровней (например, "Городской мусор", "Лесной бурелом").
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PropPlacementProfile_New", menuName = "WaveBeater/Prop Placement Profile")]
public class PropPlacementProfileSO : ScriptableObject
{
    [System.Serializable]
    public class PropChance
    {
        public PropSO Prop;
        [Range(0f, 100f)]
        public float SpawnChance = 50f;
    }

    #region Поля
    [BoxGroup("SETTINGS"), Tooltip("Список пропов и их шансы на появление в рамках этого профиля.")]
    [SerializeField] private List<PropChance> _props = new List<PropChance>();

    [BoxGroup("SETTINGS"), Tooltip("Минимальное количество пропов, которое будет создано на поле."), MinValue(0)]
    [SerializeField] private int _minPropsToSpawn = 0;

    [BoxGroup("SETTINGS"), Tooltip("Максимальное количество пропов, которое может быть создано на поле."), MinValue(0)]
    [SerializeField] private int _maxPropsToSpawn = 3;

    [BoxGroup("SETTINGS"), Tooltip("Минимальное расстояние (в клетках) между сгенерированными пропами."), MinValue(0)]
    [SerializeField] private int _minDistanceBetweenProps = 1;
    #endregion

    #region Свойства
    public List<PropChance> Props => _props;
    public int MinPropsToSpawn => _minPropsToSpawn;
    public int MaxPropsToSpawn => _maxPropsToSpawn;
    public int MinDistanceBetweenProps => _minDistanceBetweenProps;
    #endregion
}

