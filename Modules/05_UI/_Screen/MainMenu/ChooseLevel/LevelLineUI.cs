using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class LevelLineUI : MonoBehaviour
{
	#region Поля: Required
	[PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Image _levelBackground;
	[PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Image _dayIcon;
	#endregion Поля: Required

	#region Поля
	[BoxGroup("SETTINGS"), SerializeField] private Sprite _daySprite;
	[BoxGroup("SETTINGS"), SerializeField] private Sprite _eveningSprite;
	[BoxGroup("SETTINGS"), SerializeField] private Sprite _nightSprite;
	[BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
	#endregion Поля

	#region Методы UNITY
	private void Awake()
	{
		if (_levelBackground == null) DebugUtils.LogMissingReference(this, nameof(_levelBackground));
		if (_dayIcon == null) DebugUtils.LogMissingReference(this, nameof(_dayIcon));
	}
	#endregion Методы UNITY

	#region Публичные методы
	public void Initialize(LevelData levelData)
	{
		ColoredDebug.CLog(gameObject, "<color=cyan>LevelLineUI:</color> Инициализация для уровня <color=yellow>{0}</color>.", _ColoredDebug, levelData.levelNumber);
		SetBackgroundColor(levelData.minEnemyLevel);
		SetTimeOfDayIcon(levelData.backgroundVariant);
	}
	#endregion Публичные методы

	#region Личные методы
	private void SetBackgroundColor(BattleUnit.UnitLevel enemyLevel)
	{
		Color newColor = GetColorForEnemyLevel(enemyLevel);
		if (_levelBackground != null)
		{
			_levelBackground.color = newColor;
			ColoredDebug.CLog(gameObject, "<color=cyan>LevelLineUI:</color> Установлен цвет фона для уровня врага <color=yellow>{0}</color>.", _ColoredDebug, enemyLevel);
		}
	}

	private void SetTimeOfDayIcon(BackgroundVariant backgroundVariant)
	{
		Sprite newSprite = null;
		switch (backgroundVariant)
		{
			case BackgroundVariant.Day: newSprite = _daySprite; break;
			case BackgroundVariant.Evening: newSprite = _eveningSprite; break;
			case BackgroundVariant.Night: newSprite = _nightSprite; break;
		}

		if (_dayIcon != null && newSprite != null)
		{
			_dayIcon.sprite = newSprite;
			ColoredDebug.CLog(gameObject, "<color=cyan>LevelLineUI:</color> Установлена иконка времени суток: <color=yellow>{0}</color>.", _ColoredDebug, backgroundVariant);
		}
	}

	private Color GetColorForEnemyLevel(BattleUnit.UnitLevel level)
	{
		switch (level)
		{
			case BattleUnit.UnitLevel.Green_00: return new Color(0.2f, 0.8f, 0.2f);
			case BattleUnit.UnitLevel.Blue_01: return new Color(0.0f, 0.8f, 1.0f);
			case BattleUnit.UnitLevel.Yellow_02: return new Color(1.0f, 1.0f, 0.0f);
			case BattleUnit.UnitLevel.Orange_03: return new Color(1.0f, 0.5f, 0.0f);
			case BattleUnit.UnitLevel.Red_04: return new Color(0.9f, 0.1f, 0.1f);
			case BattleUnit.UnitLevel.Pink_05: return new Color(1.0f, 0.4f, 0.7f);
			case BattleUnit.UnitLevel.Turquoise_06: return new Color(0.25f, 0.88f, 0.82f);
			case BattleUnit.UnitLevel.DarkBlue_07: return new Color(0.0f, 0.2f, 0.8f);
			case BattleUnit.UnitLevel.Violet_08: return new Color(0.6f, 0.2f, 1.0f);
			case BattleUnit.UnitLevel.Maroon_09: return new Color(0.6f, 0.0f, 0.0f);
			case BattleUnit.UnitLevel.Indigo_10: return new Color(0.29f, 0.0f, 0.5f);
			case BattleUnit.UnitLevel.Grey_11: return new Color(0.5f, 0.5f, 0.5f);
			case BattleUnit.UnitLevel.Brown_12: return new Color(0.6f, 0.4f, 0.2f);
			case BattleUnit.UnitLevel.Silver_13: return new Color(0.75f, 0.75f, 0.75f);
			case BattleUnit.UnitLevel.Gold_14: return new Color(1.0f, 0.84f, 0.0f);
			case BattleUnit.UnitLevel.White_15: return new Color(1.0f, 1.0f, 1.0f);
			case BattleUnit.UnitLevel.Black_16: return new Color(0.1f, 0.1f, 0.1f);
			default: return Color.grey;
		}
	}
	#endregion Личные методы
}
