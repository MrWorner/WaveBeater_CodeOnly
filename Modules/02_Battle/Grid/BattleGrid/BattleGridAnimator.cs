using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Events;

public class BattleGridAnimator : MonoBehaviour
{
    private static BattleGridAnimator _instance;

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private Ease _easeType = Ease.OutQuad;

    [BoxGroup("SETTINGS/Show Animation"), Tooltip("Минимальная задержка между ячейками в конце анимации появления.")]
    [SerializeField] private float _minStaggerDelayOnShow = 0.001f;

    [BoxGroup("SETTINGS/Show Animation"), Tooltip("Кривая ускорения для анимации появления. Начало (слева) - медленно, конец (справа) - быстро.")]
    [SerializeField] private AnimationCurve _showAccelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [BoxGroup("SETTINGS/Hide Animation"), Tooltip("Минимальная задержка между ячейками в конце анимации скрытия.")]
    [SerializeField] private float _minStaggerDelayOnHide = 0.001f;

    [BoxGroup("SETTINGS/Hide Animation"), Tooltip("Кривая ускорения для анимации скрытия. Начало (слева) - медленно, конец (справа) - быстро.")]
    [SerializeField] private AnimationCurve _hideAccelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion

    #region Свойства
    public static BattleGridAnimator Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<BattleGridAnimator>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("BattleGridAnimator");
                    _instance = go.AddComponent<BattleGridAnimator>();
                }
            }
            return _instance;
        }
    }
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
        StartCoroutine(HideGridOnStart());
    }
    #endregion

    #region Публичные методы
    public void HideGridInstantly()
    {
        if (BattleGrid.Instance == null || BattleGrid.Instance.AllCells == null) return;

        foreach (var cell in BattleGrid.Instance.AllCells)
        {
            if (cell != null)
            {
                if (cell.SpriteRenderer != null)
                {
                    Color color = cell.SpriteRenderer.color;
                    color.a = 0f;
                    cell.SpriteRenderer.color = color;
                }
                cell.gameObject.SetActive(false);
            }
        }
    }

    private System.Collections.IEnumerator HideGridOnStart()
    {
        yield return null;
        HideGridInstantly();
    }

    [Button("Показать сетку")]
    public void ShowGrid(UnityAction onComplete = null)
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleGridAnimator:</color> Запрос на показ сетки. Переключаю камеру в режим <color=yellow>GridFocus</color>.", _ColoredDebug);
        DynamicDuelCamera.Instance.SwitchToGridFocusMode();
        AnimateGrid(true, onComplete);
    }

    [Button("Скрыть сетку")]
    public void HideGrid(UnityAction onComplete = null)
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleGridAnimator:</color> Запрос на скрытие сетки.", _ColoredDebug);
        AnimateGrid(false, onComplete);
    }
    #endregion

    #region Личные методы
    private void AnimateGrid(bool show, UnityAction onComplete)
    {
        if (BattleGrid.Instance == null || BattleGrid.Instance.AllCells == null || BattleGrid.Instance.AllCells.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        List<BattleCell> cells = BattleGrid.Instance.AllCells;
        Sequence sequence = DOTween.Sequence();
        float targetAlpha = show ? 1f : 0f;

        foreach (var cell in cells)
        {
            if (cell != null && cell.SpriteRenderer != null)
            {
                cell.SpriteRenderer.DOKill();
            }
        }

        float accumulatedDelay = 0f;

        for (int i = 0; i < cells.Count; i++)
        {
            BattleCell cell = cells[i];
            if (cell != null && cell.SpriteRenderer != null)
            {
                float maxStaggerDelay = Settings.BattleGridAnimator_staggerDelay;
                float minStaggerDelay = show ? _minStaggerDelayOnShow : _minStaggerDelayOnHide;
                AnimationCurve curve = show ? _showAccelerationCurve : _hideAccelerationCurve;

                float progress = (cells.Count > 1) ? (float)i / (cells.Count - 1) : 1f;
                float curveValue = curve.Evaluate(progress);
                float currentStagger = Mathf.Lerp(maxStaggerDelay, minStaggerDelay, curveValue);

                float startTime = accumulatedDelay;

                if (show)
                {
                    sequence.InsertCallback(startTime, () =>
                    {
                        Color c = cell.SpriteRenderer.color;
                        c.a = 0;
                        cell.SpriteRenderer.color = c;
                        cell.gameObject.SetActive(true);
                    });
                    sequence.Insert(startTime, cell.SpriteRenderer.DOFade(targetAlpha, Settings.BattleGridAnimator_fadeDuration).SetEase(_easeType));
                }
                else // Скрытие
                {
                    var tween = cell.SpriteRenderer.DOFade(targetAlpha, Settings.BattleGridAnimator_fadeDuration)
                                                       .SetEase(_easeType)
                                                       .OnComplete(() => cell.gameObject.SetActive(false));
                    sequence.Insert(startTime, tween);
                }

                accumulatedDelay += currentStagger;
            }
        }

        sequence.OnComplete(() =>
        {
            if (!show) // Вызываем переключение камеры только при СКРЫТИИ сетки
            {
                ColoredDebug.CLog(gameObject, "<color=cyan>BattleGridAnimator:</color> Анимация скрытия завершена. Переключаю камеру в режим <color=yellow>HeroFocus</color>.", _ColoredDebug);
                DynamicDuelCamera.Instance.SwitchToHeroFocusMode();
            }
            onComplete?.Invoke();
        });
    }
    #endregion
}