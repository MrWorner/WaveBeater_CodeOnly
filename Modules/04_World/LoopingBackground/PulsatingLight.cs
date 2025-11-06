using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

public class PulsatingLight : MonoBehaviour
{
    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private Color _color2 = Color.yellow;
    [BoxGroup("SETTINGS"), SerializeField] private float _duration = 1.0f;

    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private Color _originalColor;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;

    private Tween _pulsationTween;
    private SpriteRenderer _spriteRenderer;
    private bool _isPulsating = false;
    #endregion Поля

    #region Методы UNITY
    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null)
        {
            _originalColor = _spriteRenderer.color;
        }

        // Регистрируем эту "лампочку" в нашем статическом менеджере.
        PulsatingLightManager.RegisterLight(this);
    }

    private void OnDestroy()
    {
        // Убиваем твин при уничтожении объекта, чтобы избежать утечек.
        _pulsationTween?.Kill();

        // Это обязательно, чтобы менеджер не пытался управлять уже уничтоженным объектом.
        PulsatingLightManager.UnregisterLight(this);
    }
    #endregion Методы UNITY

    #region Публичные методы
    [Button]
    public void EnablePulsation()
    {
        if (_isPulsating) return;
        _isPulsating = true;

        ColoredDebug.CLog(gameObject, "<color=cyan>PulsatingLight:</color> Включена пульсация.", _ColoredDebug);

        _spriteRenderer.DOKill();

        _pulsationTween = _spriteRenderer.DOColor(_color2, _duration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    [Button]
    public void DisablePulsation()
    {
        if (!_isPulsating) return;
        _isPulsating = false;

        ColoredDebug.CLog(gameObject, "<color=cyan>PulsatingLight:</color> Выключена пульсация.", _ColoredDebug);

        _pulsationTween?.Kill();
        _spriteRenderer.color = _originalColor;
    }
    #endregion Публичные методы
}