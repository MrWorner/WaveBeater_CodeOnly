using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;

public class FPSCounter : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _fpsText;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private float _updateInterval = 0.5f;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    private float _accumulatedFrames = 0;
    private float _timeLeft;
    private int _lastFPS;
    #endregion Свойства

    #region Методы UNITY
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        if (_fpsText == null)
        {
            ColoredDebug.CLog(gameObject, "<color=#FF6347>FPSCounter:</color> Отсутствует ссылка на текстовый компонент <color=yellow>_fpsText</color>! Скрипт будет отключен.", _ColoredDebug);
            DebugUtils.LogMissingReference(this, nameof(_fpsText));
            enabled = false;
            return;
        }

        _timeLeft = _updateInterval;
        ColoredDebug.CLog(gameObject, "<color=lime>FPSCounter:</color> Успешная инициализация. Интервал обновления: <color=cyan>{0}</color> сек.", _ColoredDebug, _updateInterval);
    }

    private void Update()
    {
        _timeLeft -= Time.unscaledDeltaTime;
        _accumulatedFrames++;

        if (_timeLeft <= 0.0f)
        {
            _lastFPS = (int)(_accumulatedFrames / _updateInterval);
            _fpsText.text = "FPS: " + _lastFPS;

            ///ColoredDebug.CLog(gameObject, "<color=cyan>FPSCounter:</color> Обновление значения. Текущий FPS: <color=yellow>{0}</color>.", _ColoredDebug, _lastFPS);

            _timeLeft = _updateInterval;
            _accumulatedFrames = 0;
        }
    }
    #endregion Методы UNITY
}