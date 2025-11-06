// НАЗНАЧЕНИЕ: Управляет всеми элементами UI, связанными с отображением прогресса по этапам (прогресс-бар, текстовые поля).
// ОСНОВНЫЕ ЗАВИСИМОСТИ: StageProgressBar, TextMeshProUGUI.
// ПРИМЕЧАНИЕ: Этот класс изолирует логику обновления UI от основной логики управления этапами.
using Sirenix.OdinInspector;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class StageUIController : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private StageProgressBar _stageProgressBar;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _textCurrentLevel;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _textCurrentStageNum;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Методы UNITY
    private void Awake()
    {
        if (_stageProgressBar == null) DebugUtils.LogMissingReference(this, nameof(_stageProgressBar));
        if (_textCurrentLevel == null) DebugUtils.LogMissingReference(this, nameof(_textCurrentLevel));
        if (_textCurrentStageNum == null) DebugUtils.LogMissingReference(this, nameof(_textCurrentStageNum));
    }
    #endregion

    #region Публичные методы
    /// <summary>
    /// Инициализирует UI с новой последовательностью этапов.
    /// </summary>
    /// <param name="stages">Список типов этапов.</param>
    public void Initialize(List<StageType> stages)
    {
        _stageProgressBar.Init(stages);
        ColoredDebug.CLog(gameObject, "<color=cyan>StageUIController:</color> Прогресс-бар инициализирован с <color=yellow>{0}</color> этапами.", _ColoredDebug, stages.Count);
    }

    /// <summary>
    /// Обновляет все текстовые элементы UI.
    /// </summary>
    /// <param name="currentLevel">Номер текущего уровня.</param>
    /// <param name="currentStageNum">Номер текущего этапа.</param>
    /// <param name="totalStages">Общее количество этапов.</param>
    public void UpdateUI(int currentLevel, int currentStageNum, int totalStages)
    {
        _textCurrentLevel.text = "Level: " + currentLevel;
        _textCurrentStageNum.text = "Stage: " + currentStageNum + "/" + totalStages;
        ColoredDebug.CLog(gameObject, "<color=cyan>StageUIController:</color> UI обновлен. Уровень: <color=lime>{0}</color>, Этап: <color=lime>{1}/{2}</color>.", _ColoredDebug, currentLevel, currentStageNum, totalStages);
    }
    #endregion
}