using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using DG.Tweening;
using UnityEngine.Events;

public class LevelCardUI : MonoBehaviour
{
	#region Поля: Required
	[PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _levelLinePrefab;
	[PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Transform _containerLevelLines;
	[PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Image _bossIcon;
	[PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _wavesCount;
	[PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _lockedOverlay;
	[PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _isChosenOverlay;
	[PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Button _selectButton;
	[PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private CanvasGroup _mainContentCanvasGroup;
	#endregion Поля: Required

	#region Поля
	[BoxGroup("DEBUG"), SerializeField, ReadOnly] private LevelProgression _levelProgression;
	[BoxGroup("DEBUG"), SerializeField, ReadOnly] private List<LevelLineUI> _levelLines = new List<LevelLineUI>();
	[BoxGroup("DEBUG"), SerializeField, ReadOnly] private bool _isLocked;
	[BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;

	public LevelProgression LevelProgression { get => _levelProgression; }
	public bool IsLocked => _isLocked;
	#endregion Поля

	#region Методы UNITY
	private void Awake()
	{
		if (_levelLinePrefab == null) DebugUtils.LogMissingReference(this, nameof(_levelLinePrefab));
		if (_containerLevelLines == null) DebugUtils.LogMissingReference(this, nameof(_containerLevelLines));
		if (_bossIcon == null) DebugUtils.LogMissingReference(this, nameof(_bossIcon));
		if (_wavesCount == null) DebugUtils.LogMissingReference(this, nameof(_wavesCount));
		if (_lockedOverlay == null) DebugUtils.LogMissingReference(this, nameof(_lockedOverlay));
		if (_isChosenOverlay == null) DebugUtils.LogMissingReference(this, nameof(_isChosenOverlay));
		if (_selectButton == null) DebugUtils.LogMissingReference(this, nameof(_selectButton));
		if (_mainContentCanvasGroup == null) DebugUtils.LogMissingReference(this, nameof(_mainContentCanvasGroup));
	}
	#endregion Методы UNITY

	#region Публичные методы
	public void Initialize(LevelProgression progression, bool isLocked)
	{
		_levelProgression = progression;
		_isLocked = isLocked;

		if (_levelProgression == null)
		{
			ColoredDebug.CLog(gameObject, "<color=red>LevelCardUI:</color> Попытка инициализации с NULL LevelProgression!", _ColoredDebug);
			return;
		}

		ColoredDebug.CLog(gameObject, "<color=cyan>LevelCardUI:</color> Инициализация для прогрессии <color=yellow>{0}</color>. Заблокировано: <color=orange>{1}</color>.", _ColoredDebug, progression.name, _isLocked);

		ClearLines();

		foreach (LevelData levelData in _levelProgression.Levels)
		{
			GameObject lineInstance = Instantiate(_levelLinePrefab, _containerLevelLines);
			lineInstance.transform.SetAsFirstSibling();

			LevelLineUI lineUI = lineInstance.GetComponent<LevelLineUI>();

			if (lineUI != null)
			{
				lineUI.Initialize(levelData);
				_levelLines.Add(lineUI);
				ColoredDebug.CLog(gameObject, "<color=cyan>LevelCardUI:</color> Создана линия для уровня <color=yellow>{0}</color>.", _ColoredDebug, levelData.levelNumber);
			}
			else
			{
				ColoredDebug.CLog(gameObject, "<color=red>LevelCardUI:</color> Префаб линии не содержит компонент LevelLineUI! Уничтожаю созданный объект.", _ColoredDebug);
				Destroy(lineInstance);
			}
		}

		if (progression.FinalBoss != null && progression.FinalBoss.Prefab != null && progression.FinalBoss.Prefab.MainBodyPart != null)
		{
			_bossIcon.sprite = progression.FinalBoss.Prefab.MainBodyPart.GetComponent<SpriteRenderer>().sprite;
		}

		_wavesCount.text = "" + CalculateTotalBattles(progression);

		UpdateCardState();

		ColoredDebug.CLog(gameObject, "<color=green>LevelCardUI:</color> Инициализация завершена. Создано <color=yellow>{0}</color> линий.", _ColoredDebug, _levelLines.Count);
	}

	public void SetChosen(bool isChosen)
	{
		_isChosenOverlay.SetActive(isChosen);
		if (isChosen)
		{
			transform.DOKill(true);
			transform.DOScale(1.05f, 0.2f).SetEase(Ease.OutBack);
		}
		else
		{
			transform.DOKill(true);
			transform.DOScale(1f, 0.2f);
		}
	}

	public void SetButtonListener(UnityAction listener)
	{
		_selectButton.onClick.RemoveAllListeners();
		_selectButton.onClick.AddListener(listener);
	}
	#endregion Публичные методы

	#region Личные методы
	private void UpdateCardState()
	{
		_lockedOverlay.SetActive(_isLocked);
		_selectButton.interactable = !_isLocked;
		_isChosenOverlay.SetActive(false);

		if (_isLocked)
		{
			///_mainContentCanvasGroup.alpha = 0.5f;
			_mainContentCanvasGroup.blocksRaycasts = false;
		}
		else
		{
			///_mainContentCanvasGroup.alpha = 1.0f;
			_mainContentCanvasGroup.blocksRaycasts = true;
		}
	}

	private int CalculateTotalBattles(LevelProgression progression)
	{
		int totalBattles = 0;
		if (progression == null || progression.Levels == null)
		{
			return 0;
		}

		for (int i = 0; i < progression.Levels.Count; i++)
		{
			var levelData = progression.Levels[i];
			List<StageType> stagesForLevel;

			if (i == progression.Levels.Count - 1)
			{
				stagesForLevel = new List<StageType> { StageType.Shop, StageType.Hospital, StageType.BossFight, StageType.GameOver };
			}
			else
			{
				int levelPattern = (levelData.levelNumber - 1) % 3;
				switch (levelPattern)
				{
					case 0:
						stagesForLevel = new List<StageType>
						{
							StageType.Battle, StageType.Battle, StageType.Shop, StageType.Battle, StageType.Award
						};
						break;
					case 1:
						stagesForLevel = new List<StageType>
						{
							StageType.Battle, StageType.Battle, StageType.Shop, StageType.Battle,
							StageType.Battle, StageType.Shop, StageType.Battle, StageType.Award
						};
						break;
					case 2:
					default:
						stagesForLevel = new List<StageType>
						{
							StageType.Battle, StageType.Battle, StageType.Shop, StageType.Battle, StageType.Battle,
							StageType.Shop, StageType.Battle, StageType.Battle, StageType.Award
						};
						break;
				}
			}

			totalBattles += stagesForLevel.Count(IsBattleStage);
		}

		return totalBattles;
	}

	private bool IsBattleStage(StageType stageType)
	{
		switch (stageType)
		{
			case StageType.Battle:
			case StageType.Horde:
			case StageType.MiniBoss:
			case StageType.HighLevelBattle:
			case StageType.MixedBattle:
			case StageType.DoubleMiniBoss:
			case StageType.TripleMiniBoss:
			case StageType.BossFight:
				return true;
			default:
				return false;
		}
	}


	private void ClearLines()
	{
		ColoredDebug.CLog(gameObject, "<color=orange>LevelCardUI:</color> Очистка предыдущих линий...", _ColoredDebug);
		foreach (Transform child in _containerLevelLines)
		{
			Destroy(child.gameObject);
		}
		_levelLines.Clear();
	}
	#endregion Личные методы
}
