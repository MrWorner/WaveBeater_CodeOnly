using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using DG.Tweening;

public class CurrentStageUI : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private Image _stageIconImage;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private Image _stageBackgroundImage;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private TextMeshProUGUI _stageNameText;
    #endregion

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField]
    private float _fadeDuration = 0.3f;
    [BoxGroup("DEBUG"), SerializeField]
    protected bool _ColoredDebug;
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (_stageIconImage == null) DebugUtils.LogMissingReference(this, nameof(_stageIconImage));
        if (_stageBackgroundImage == null) DebugUtils.LogMissingReference(this, nameof(_stageBackgroundImage));
        if (_stageNameText == null) DebugUtils.LogMissingReference(this, nameof(_stageNameText));

    }

    private void Start()
    {
        if (CurrentStageUIManager.Instance != null)
        {
            CurrentStageUIManager.Instance.RegisterListener(this);
        }
    }
    #endregion

    #region Публичные методы
    public void UpdateUI(CurrentStageUIManager.StageUIConfig config)
    {
        if (config == null)
        {
            ColoredDebug.CLog(gameObject, "Конфигурация UI этапа пуста.", _ColoredDebug);
            return;
        }

        if (_stageIconImage != null)
        {
            _stageIconImage.sprite = config.Icon;
        }

        if (_stageNameText != null)
        {
            _stageNameText.text = config.DisplayName;
        }

        if (_stageBackgroundImage != null)
        {
            _stageBackgroundImage.DOKill();
            _stageBackgroundImage.DOColor(config.BackgroundColor, _fadeDuration).SetEase(Ease.OutQuad);
        }
    }
    #endregion
}