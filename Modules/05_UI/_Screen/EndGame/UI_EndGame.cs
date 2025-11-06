using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine.Events;

public class UI_EndGame : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Image _background;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private RectTransform _panel;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Button _restartButton;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private float _backgroundAlpha = 1f;
    [BoxGroup("SETTINGS"), SerializeField] private float _backgroundFadeDuration = 0.4f;
    [BoxGroup("SETTINGS"), SerializeField] private float _panelAnimationDuration = 0.5f;
    [BoxGroup("SETTINGS"), SerializeField] private Vector2 _hiddenPosition;
    [BoxGroup("SETTINGS"), SerializeField] private Vector2 _shownPosition;
    [BoxGroup("DEBUG"), SerializeField] private bool _coloredDebug;
    private static UI_EndGame _instance;
    #endregion Поля

    #region Свойства
    public static UI_EndGame Instance => _instance;
    public Image Background => _background;
    public RectTransform Panel => _panel;
    public Button RestartButton => _restartButton;
    public float BackgroundAlpha => _backgroundAlpha;
    public float BackgroundFadeDuration => _backgroundFadeDuration;
    public float PanelAnimationDuration => _panelAnimationDuration;
    public Vector2 HiddenPosition => _hiddenPosition;
    public Vector2 ShownPosition => _shownPosition;
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            DebugUtils.LogInstanceAlreadyExists(this);
            Destroy(gameObject);
            return;
        }
        _instance = this;
        if (_background == null) DebugUtils.LogMissingReference(this, nameof(_background));
        if (_panel == null) DebugUtils.LogMissingReference(this, nameof(_panel));
        if (_restartButton == null) DebugUtils.LogMissingReference(this, nameof(_restartButton));
        _restartButton.onClick.AddListener(OnRestartButtonPressed);
    }
    #endregion Методы UNITY

    #region Публичные методы
    [Button]
    public void ShowEndScreen()
    {
        ColoredDebug.CLog(gameObject, "<color=red>UI_EndGame:</color> Запускаю анимацию экрана ShowEndScreen.", _coloredDebug);
        _background.DOFade(_backgroundAlpha, _backgroundFadeDuration).OnComplete(() =>
        {
            _panel.DOAnchorPos(_shownPosition, _panelAnimationDuration).SetEase(Ease.OutBack);
            ColoredDebug.CLog(gameObject, "<color=red>UI_EndGame:</color> Панель сдвигается на позицию <color=yellow>{0}</color>.", _coloredDebug, _shownPosition);
        });
        ColoredDebug.CLog(gameObject, "<color=red>UI_EndGame:</color> Фон затемняется.", _coloredDebug);
    }
    #endregion Публичные методы

    #region Личные методы
    private void OnRestartButtonPressed()
    {
        ColoredDebug.CLog(gameObject, "<color=red>UI_EndGame:</color> Нажата кнопка перезапуска. Запускаю анимацию выхода.", _coloredDebug);
        SoundManager.Instance.PlayOneShot(SoundType.ButtonClick);
        _panel.DOAnchorPos(_hiddenPosition, _panelAnimationDuration).SetEase(Ease.InBack).OnComplete(() =>
        {
            ColoredDebug.CLog(gameObject, "<color=red>UI_EndGame:</color> Панель скрылась. Перезагружаю сцену.", _coloredDebug);
            _background.DOFade(0, _backgroundFadeDuration).OnComplete(() =>
            {
                SceneLoader.Instance.LoadNextScene(GameScene.MainMenu);
            });
        });
    }
    #endregion Личные методы
}