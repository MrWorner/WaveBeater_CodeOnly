using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine.Events;

public class PlayerTravelController : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private StageProgressBar _stageProgressBar;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField, ReadOnly] private BattleUnit _hero;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Методы UNITY
    private void Start()
    {
        _hero = BattleUnit.Hero;
        if (_hero == null) DebugUtils.LogMissingReference(this, nameof(_hero));
    }
    #endregion

    #region Публичные методы
    public void StartTravel(int targetIndex, UnityAction onArrived)
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>PlayerTravelController:</color> Игрок начал движение к этапу <color=lime>{0}</color>. Переключаю камеру в режим <color=yellow>Travel</color>.", _ColoredDebug, targetIndex);
        DynamicDuelCamera.Instance.SwitchToTravelMode();
        SoundManager.Instance.StartFootsteps();
        ///----------_hero.HeroAnimation.PlayRunAnimation();
        StartCoroutine(MoveCoroutine(targetIndex, onArrived));
    }
    #endregion Публичные методы

    #region Личные методы
    private IEnumerator MoveCoroutine(int targetIndex, UnityAction onArrived)
    {
        float startValue = _stageProgressBar.GetStageProgress(targetIndex - 1);
        float targetValue = _stageProgressBar.GetStageProgress(targetIndex);
        float elapsed = 0f;

        while (elapsed < Settings.MovementDurationBetweenStages)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Settings.MovementDurationBetweenStages);
            _stageProgressBar.SetProgress(Mathf.Lerp(startValue, targetValue, t));
            yield return null;
        }

        _stageProgressBar.SetProgress(targetValue);
        ColoredDebug.CLog(gameObject, "<color=cyan>PlayerTravelController:</color> Игрок прибыл на этап <color=lime>{0}</color>. Переключаю камеру в режим <color=yellow>HeroFocus</color>.", _ColoredDebug, targetIndex);
        ///DynamicDuelCamera.ActiveInstance.SwitchToHeroFocusMode();
        SoundManager.Instance.StopFootsteps();
        ///----------_hero.HeroAnimation.SetStance(false);
        onArrived?.Invoke();
    }
    #endregion Личные методы
}
