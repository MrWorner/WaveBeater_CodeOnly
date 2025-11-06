// НАЗНАЧЕНИЕ: Управляет логикой боевого этапа, включая подготовку поля, запуск волн и очистку после боя.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: WaveManager, BattleGridAnimator, BattleGrid, ScenarioManager.
// ПРИМЕЧАНИЕ: Является связующим звеном между общей логикой этапов (StageManager) и системами, специфичными для боя.

using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;

public class BattleStageHandler : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private WaveManager _waveManager;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    private UnityAction _onBattleFinished;
    private StageType _previousStage;
    #endregion Поля

    #region Методы UNITY
    private void Awake()
    {
        if (_waveManager == null) DebugUtils.LogMissingReference(this, nameof(_waveManager));
    }
    #endregion Методы UNITY

    #region Публичные методы
    public void StartBattle(UnityAction onFinished, LevelData levelData, StageType stageType)
    {
        _previousStage = stageType;
        _onBattleFinished = onFinished;

        if (stageType != StageType.Battle)
        {
            MusicManager.Instance.PlayBattleMusic();
        }

        if (BattleUnit.Hero != null && BattleUnit.Hero.Movement != null)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>BattleStageHandler:</color> Принудительная переустановка Героя на сетке.", _ColoredDebug);
            BattleUnit.Hero.Movement.ReoccupyCurrentCells();
        }
        else
        {
            Debug.LogError("[BattleStageHandler] Не удалось найти героя или его компонент Movement для повторной установки!");
        }

        SoundManager.Instance.PlayOneShot(SoundType.EnemyComing);

        BattleGridAnimator.Instance.ShowGrid(() =>
        {
            _waveManager.StartWave(OnBattleEnded, levelData, stageType);
        });
    }
    #endregion Публичные методы

    #region Личные методы
    private void OnBattleEnded()
    {
        if (_previousStage != StageType.Battle)
        {
            MusicManager.Instance.PlayWanderingAroundMusic();
        }

        if (Settings.EnableScenarioMode && ScenarioManager.Instance != null)
        {
            ColoredDebug.CLog(gameObject, "<color=purple>BattleStageHandler:</color> Бой в режиме сценария завершен. Переход к следующему сценарию.", _ColoredDebug);
            ScenarioManager.Instance.Advance();
        }

        BattleGridAnimator.Instance.HideGrid(() =>
        {
            _onBattleFinished?.Invoke();
        });
    }
    #endregion Личные методы
}