using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "HeroData_New", menuName = "WaveBeater/Hero_DEPRECATED Data")]
public class HeroDataSO : ScriptableObject
{
	#region Поля: Required
	[PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField, PreviewField(75)] private Sprite _heroSprite;
	[PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField, PreviewField(75)] private GameObject _heroPrefab;
	#endregion Поля: Required

	#region Поля
	[BoxGroup("SETTINGS"), Tooltip("Имя героя для отображения в UI"), SerializeField] private string _heroName = "Hero_DEPRECATED";
	[BoxGroup("SETTINGS"), Tooltip("Краткое описание или история героя"), SerializeField, TextArea] private string _description = "A brave hero ready for battle.";
	//[BoxGroup("SETTINGS"), Tooltip("Начальное максимальное здоровье героя"), SerializeField, MinValue(1)] private int _initialHealth = 10;
	//[BoxGroup("SETTINGS"), Tooltip("Начальный урон героя"), SerializeField, MinValue(1)] private int _initialAttackDamage = 1;
	[BoxGroup("SETTINGS"), Tooltip("Начальный урон героя"), SerializeField, MinValue(1)] private int _requiredPlayerLevelToUnlock = 0;
	[BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
	#endregion Поля

	#region Свойства
	public string HeroName => _heroName;
	public string Description => _description;
	public Sprite HeroSprite => _heroSprite;

    public int RequiredPlayerLevelToUnlock { get => _requiredPlayerLevelToUnlock; }
    public GameObject HeroPrefab { get => _heroPrefab;  }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
	{
		//if (_heroPrefab == null) DebugUtils.LogMissingReference(gameObject, nameof(_heroPrefab));
	}
	#endregion Методы UNITY
}