using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class GameInstance : MonoBehaviour
{
	private static GameInstance _instance;

	#region Поля
	[BoxGroup("Persistent Player Data"), SerializeField] private int _playerExperience;
	[BoxGroup("Persistent Player Data"), SerializeField] private int _playerCurrency;
	[BoxGroup("Session Choices"), Tooltip("Данные о выбранном герое. Устанавливается в главном меню."), SerializeField] private HeroDataSO _selectedHeroData;
	[BoxGroup("Session Choices"), Tooltip("Выбранный набор уровней. Устанавливается в главном меню."), SerializeField] private LevelProgression _selectedLevelProgression;
	[BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
	#endregion Поля

	#region Свойства
	public static GameInstance Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = FindFirstObjectByType<GameInstance>();

				if (_instance == null)
				{
					GameObject singletonObject = new GameObject(typeof(GameInstance).Name);
					_instance = singletonObject.AddComponent<GameInstance>();
					ColoredDebug.CLog(_instance.gameObject, "<color=yellow>GameInstance:</color> Экземпляр синглтона не найден на сцене. Создаю новый объект.", _instance._ColoredDebug);
				}

				DontDestroyOnLoad(_instance.gameObject);
				ColoredDebug.CLog(_instance.gameObject, "<color=lime>GameInstance:</color> Синглтон инициализирован и установлен как DontDestroyOnLoad.", _instance._ColoredDebug);
			}

			return _instance;
		}
	}
	public int PlayerExperience => _playerExperience;
	public int PlayerCurrency => _playerCurrency;
	public HeroDataSO SelectedHeroData => _selectedHeroData;
	public LevelProgression SelectedLevelProgression => _selectedLevelProgression;
	#endregion Свойства

	#region Методы UNITY
	private void Awake()
	{
		ColoredDebug.CLog(gameObject, "<color=lime>GameInstance:</color> Попытка инициализации синглтона.", _ColoredDebug);
		if (Instance != null && Instance != this)
		{
			ColoredDebug.CLog(gameObject, "<color=red>GameInstance:</color> Другой экземпляр синглтона уже существует! Уничтожаю текущий объект.", _ColoredDebug);
			Destroy(gameObject);
			return;
		}
		_instance = this;
		DontDestroyOnLoad(gameObject);
		ColoredDebug.CLog(gameObject, "<color=lime>GameInstance:</color> Синглтон инициализирован и установлен как DontDestroyOnLoad.", _ColoredDebug);
	}
	#endregion Методы UNITY

	#region Публичные методы
	public void SetSelectedHeroData(HeroDataSO selectedHeroData)
	{
		_selectedHeroData = selectedHeroData;
		ColoredDebug.CLog(gameObject, $"<color=cyan>GameInstance:</color> Установлены данные выбранного героя: <color=yellow>{(selectedHeroData != null ? selectedHeroData.HeroName : "NONE")}</color>.", _ColoredDebug);
	}

	public void SetSelectedLevelProgression(LevelProgression selectedProgression)
	{
		_selectedLevelProgression = selectedProgression;
		ColoredDebug.CLog(gameObject, $"<color=cyan>GameInstance:</color> Установлен выбранный набор уровней: <color=yellow>{(selectedProgression != null ? selectedProgression.name : "NONE")}</color>.", _ColoredDebug);
	}
	#endregion Публичные методы
}