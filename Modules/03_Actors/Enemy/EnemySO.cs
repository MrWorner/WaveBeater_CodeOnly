using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using static UnityEngine.Debug;

[CreateAssetMenu(fileName = "EnemySO_New", menuName = "--->WaveBeater/EnemySO ", order = 9999)]
public class EnemySO : ScriptableObject
{
    #region Поля: Required
    [BoxGroup("Required"), Tooltip("The prefab game object for this enemy."), Required(InfoMessageType.Error)]
    public BattleUnit prefab;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), Tooltip("The display name of the enemy type.")]
    public string enemyName = "Enemy";

    [BoxGroup("SETTINGS"), Tooltip("Множитель, который отражает уникальное поведение врага. 1.0 = стандарт. >1.0 = опаснее (берсерк, пулеметчик). <1.0 = менее опасен.")]
    [Range(0.1f, 100.0f)]
    public float _threat = 1.0f;

    [BoxGroup("SETTINGS"), Tooltip("A list of all available variants for this enemy type.")]
    [ListDrawerSettings(ShowItemCount = true)]
    public List<EnemyVariant> AvailableVariants = new List<EnemyVariant>();

    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    public string EnemyName { get => enemyName; }
    public BattleUnit Prefab { get => prefab; }
    public List<EnemyVariant> Variants { get => AvailableVariants; }
    #endregion Свойства

    [System.Serializable]
    public class EnemyVariant
    {
        [Tooltip("The level of this enemy variant, which determines its stats and appearance.")]
        public BattleUnit.UnitLevel level;
        public float threat;

        public EnemyVariant(BattleUnit.UnitLevel level, float threat)
        {
            this.level = level;
            this.threat = threat;
        }
    }

#if UNITY_EDITOR
    [TitleGroup("Инструменты")]
    [Button("Заполнить список вариантов", ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 0.4f)]
    [PropertyOrder(-99)]
    private void FillAvailableVariants()
    {
        AvailableVariants.Clear();
        var allDefinedLevels = Enum.GetValues(typeof(BattleUnit.UnitLevel)).Cast<BattleUnit.UnitLevel>();

        foreach (var level in allDefinedLevels)
        {
            float newThreat = _threat * ((int)level + 1);
            AvailableVariants.Add(new EnemyVariant(level, newThreat));
        }

        EditorUtility.SetDirty(this);
    }
#endif
}