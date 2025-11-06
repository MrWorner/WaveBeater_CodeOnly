// НАЗНАЧЕНИЕ: Хранит глобальные настройки и константы игры, такие как скорости, задержки и отладочные флаги.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: Obvious.Soap (для ScriptableObject-переменных), Sirenix.OdinInspector.
// ПРИМЕЧАНИЕ: Является синглтоном для удобного доступа к настройкам из любой точки кода.

using Obvious.Soap;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class Settings : MonoBehaviour
{
    #region Поля: Required
    // --- Movement & Speed Settings ---
    [TabGroup("Tabs", "Movement & Speed")][SerializeField] private FloatReference _MovementDuringBattleSpeed;
    [TabGroup("Tabs", "Movement & Speed")][SerializeField] private FloatReference _MovementDuringMeleeAttackSpeed;
    [TabGroup("Tabs", "Movement & Speed")][SerializeField] private FloatReference _MovementSpeedDuringSpawn;
    [TabGroup("Tabs", "Movement & Speed")][SerializeField] private FloatReference _ProjectileSpeed;
    [TabGroup("Tabs", "Movement & Speed")][SerializeField] private FloatReference _MovementDurationBetweenStages;

    // --- Timings & Delays Settings ---
    [TabGroup("Tabs", "Timings & Delays")][SerializeField] private FloatReference _IntroWait;
    [TabGroup("Tabs", "Timings & Delays")][SerializeField] private FloatReference _FadeDuration;
    [TabGroup("Tabs", "Timings & Delays")][SerializeField] private FloatReference _WaitForNewStage;
    [TabGroup("Tabs", "Timings & Delays")][SerializeField] private FloatReference _StageStartDelay;
    [TabGroup("Tabs", "Timings & Delays")][SerializeField] private FloatReference _WaitAfterAllEnemySpawned;
    [TabGroup("Tabs", "Timings & Delays")][SerializeField] private FloatReference _WaitAfterUnitAction_Moved;
    [TabGroup("Tabs", "Timings & Delays")][SerializeField] private FloatReference _WaitAfterUnitAction_RangedAttackFailed;
    [TabGroup("Tabs", "Timings & Delays")][SerializeField] private FloatReference _WaitAfterUnitAction_RangedAttackSucess;
    [TabGroup("Tabs", "Timings & Delays")][SerializeField] private FloatReference _WaitAfterUnitAction_Melee;
    [TabGroup("Tabs", "Timings & Delays")][SerializeField] private FloatReference _WaitAfterUnitAction_RangedAttackFired;
    [TabGroup("Tabs", "Timings & Delays")][SerializeField] private FloatReference _WaitAfterUnitAction_Heal;
    [TabGroup("Tabs", "Timings & Delays")][SerializeField] private FloatReference _QuickEnemyMoveWaitTime;

    // --- Combat & FX Settings ---
    [TabGroup("Tabs", "Combat & FX")][SerializeField] private FloatReference _hitAttackAnimationSpeed;
    [TabGroup("Tabs", "Combat & FX")][SerializeField] private FloatReference _FloatingTextDelayedTime;
    [TabGroup("Tabs", "Combat & FX")][SerializeField] private FloatReference _backlashEffectDuration;
    [TabGroup("Tabs", "Combat & FX")][SerializeField] private FloatReference _backgroundChangeDuration;
    [TabGroup("Tabs", "Combat & FX")][SerializeField] private FloatReference _BountyMultiplier;

    // --- Shop & UI Settings ---
    [TabGroup("Tabs", "Shop & UI")][SerializeField] private FloatReference _shop_backgroundFadeDuration;
    [TabGroup("Tabs", "Shop & UI")][SerializeField] private FloatReference _shop_panelAnimationDuration;
    [TabGroup("Tabs", "Shop & UI")][SerializeField] private BoolReference _autoBuyUpgradesInShop;

    // --- Camera & Animation ---
    [TabGroup("Tabs", "Camera & Animation")][SerializeField] private FloatReference _BattleGridAnimator_fadeDuration;
    [TabGroup("Tabs", "Camera & Animation")][SerializeField] private FloatReference _BattleGridAnimator_staggerDelay;
    [TabGroup("Tabs", "Camera & Animation")][SerializeField] private FloatReference _DynamicDuelCamera_smoothTime;

    // --- Debug & Scenario Settings ---
    [TabGroup("Tabs", "Debug & Scenario")][SerializeField] private BoolReference _EnableQuickMoveOptimization;
    [TabGroup("Tabs", "Debug & Scenario")][SerializeField] private BoolReference _Debug_ForceShowColordebug;
    [TabGroup("Tabs", "Debug & Scenario")]
    [SerializeField, Tooltip("Включает режим проигрывания сценариев, игнорируя стандартную генерацию волн и арен.")]
    private bool _enableScenarioMode = false;
    [TabGroup("Tabs", "Debug & Scenario")]
    [ShowIf("_enableScenarioMode")]
    [SerializeField, Tooltip("Последовательность сценариев, которая будет проигрываться в бою.")]
    private ScenarioSequenceSO _activeScenarioSequence;
    #endregion

    #region Поля
    private static Settings _instance;
    [TabGroup("Tabs", "Debug & Scenario")][SerializeField] private bool _speedMultiplierON;
    [TabGroup("Tabs", "Debug & Scenario")][SerializeField, Range(1, 16)] private float _speedMultiplier = 1f;
    private Dictionary<FloatReference, float> _originalFloatValues = new Dictionary<FloatReference, float>();
    #endregion

    #region Свойства
    public static float MovementDuringBattleSpeed { get => Instance._MovementDuringBattleSpeed.Value; }
    public static float HitAttackAnimationSpeed { get => Instance._hitAttackAnimationSpeed.Value; }
    public static float IntroWait { get => Instance._IntroWait.Value; }
    public static float FadeDuration { get => Instance._FadeDuration.Value; }
    public static float WaitForNewStage { get => Instance._WaitForNewStage.Value; }
    public static float MovementDurationBetweenStages { get => Instance._MovementDurationBetweenStages.Value; }
    public static float ProjectileSpeed { get => Instance._ProjectileSpeed.Value; }
    public static float WaitAfterUnitAction_Moved { get => Instance._WaitAfterUnitAction_Moved.Value; }
    public static float WaitAfterUnitAction_RangedAttackFailed { get => Instance._WaitAfterUnitAction_RangedAttackFailed.Value; }
    public static float WaitAfterUnitAction_RangedAttackSucess { get => Instance._WaitAfterUnitAction_RangedAttackSucess.Value; }
    public static float WaitAfterUnitAction_Melee { get => Instance._WaitAfterUnitAction_Melee.Value; }
    public static float WaitAfterUnitAction_RangedAttackFired { get => Instance._WaitAfterUnitAction_RangedAttackFired.Value; }
    public static float MovementDuringMeleeAttackSpeed { get => Instance._MovementDuringMeleeAttackSpeed.Value; }
    public static float WaitAfterUnitAction_Heal { get => Instance._WaitAfterUnitAction_Heal.Value; }
    public static float QuickEnemyMoveWaitTime { get => Instance._QuickEnemyMoveWaitTime.Value; }
    public static float MovementSpeedDuringSpawn { get => Instance._MovementSpeedDuringSpawn.Value; }
    public static bool EnableQuickMoveOptimization { get => Instance._EnableQuickMoveOptimization.Value; }
    public static float FloatingTextDelayedTime { get => Instance._FloatingTextDelayedTime.Value; }
    public static float StageStartDelay { get => Instance._StageStartDelay.Value; }
    public static float Shop_backgroundFadeDuration { get => Instance._shop_backgroundFadeDuration.Value; }
    public static float Shop_panelAnimationDuration { get => Instance._shop_panelAnimationDuration.Value; }
    public static float BountyMultiplier { get => Instance._BountyMultiplier.Value; }
    public static float WaitAfterAllEnemySpawned { get => Instance._WaitAfterAllEnemySpawned.Value; }
    public static float BacklashEffectDuration { get => Instance._backlashEffectDuration.Value; }
    public static float BackgroundChangeDuration { get => Instance._backgroundChangeDuration.Value; }
    public static float BattleGridAnimator_fadeDuration { get => Instance._BattleGridAnimator_fadeDuration.Value; }
    public static float BattleGridAnimator_staggerDelay { get => Instance._BattleGridAnimator_staggerDelay.Value; }
    public static float DynamicDuelCamera_smoothTime { get => Instance._DynamicDuelCamera_smoothTime.Value; }
    public static bool AutoBuyUpgradesInShop { get => Instance._autoBuyUpgradesInShop.Value; }
    public static bool Debug_ForceShowColordebug { get => Instance._Debug_ForceShowColordebug.Value; }

    public static bool EnableScenarioMode { get => Instance._enableScenarioMode; }
    public static ScenarioSequenceSO ActiveScenarioSequence { get => Instance._activeScenarioSequence; }

    public static Settings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<Settings>();
            }
            return _instance;
        }
    }
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        Application.targetFrameRate = 60;
        ColoredDebug.DeleteLogFile();

        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        StoreOriginalValues();
        if (_speedMultiplierON)
        {
            SetSpeedMultiplier(_speedMultiplier);
        }
    }
    #endregion

    #region Публичные методы
    public static void SetSimulationMode(bool isSimulating, float speedMultiplier = 100f)
    {
        if (Instance == null) return;
        Instance.SetSkipAnimation(isSimulating);
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = Mathf.Clamp(multiplier, 1f, 16f);
        Time.timeScale = _speedMultiplier;
    }

    public void SetSkipAnimation(bool isTrue)
    {
        if (isTrue)
        {
            foreach (var entry in Instance._originalFloatValues)
            {
                var floatRef = entry.Key;

                if (floatRef == Instance._ProjectileSpeed)
                {
                    // Для СКОРОСТИ - устанавливаем огромное значение
                    floatRef.Value = entry.Value * 999999;
                }
                else
                {
                    // Для ДЛИТЕЛЬНОСТЕЙ/ЗАДЕРЖЕК - устанавливаем очень маленькое, но не нулевое значение.
                    // Это предотвращает зависание игры из-за бесконечных циклов в одном кадре.
                    floatRef.Value = 0.001f;
                }
            }
        }
        else
        {
            // Восстанавливаем оригинальные значения
            Time.timeScale = 1f;
            foreach (var entry in Instance._originalFloatValues)
            {
                entry.Key.Value = entry.Value;
            }
        }
    }
    #endregion

    #region Личные методы
    private void StoreOriginalValues()
    {
        _originalFloatValues.Clear();
        AddOriginalValue(_MovementDuringBattleSpeed);
        AddOriginalValue(_hitAttackAnimationSpeed);
        AddOriginalValue(_IntroWait);
        AddOriginalValue(_FadeDuration);
        AddOriginalValue(_WaitForNewStage);
        AddOriginalValue(_MovementDurationBetweenStages);
        AddOriginalValue(_ProjectileSpeed);
        AddOriginalValue(_WaitAfterUnitAction_Moved);
        AddOriginalValue(_WaitAfterUnitAction_RangedAttackFailed);
        AddOriginalValue(_WaitAfterUnitAction_RangedAttackSucess);
        AddOriginalValue(_WaitAfterUnitAction_Melee);
        AddOriginalValue(_WaitAfterUnitAction_RangedAttackFired);
        AddOriginalValue(_MovementDuringMeleeAttackSpeed);
        AddOriginalValue(_WaitAfterUnitAction_Heal);
        AddOriginalValue(_QuickEnemyMoveWaitTime);
        AddOriginalValue(_MovementSpeedDuringSpawn);
        AddOriginalValue(_FloatingTextDelayedTime);
        AddOriginalValue(_StageStartDelay);
        AddOriginalValue(_shop_backgroundFadeDuration);
        AddOriginalValue(_shop_panelAnimationDuration);
        AddOriginalValue(_WaitAfterAllEnemySpawned);
        AddOriginalValue(_backlashEffectDuration);
        AddOriginalValue(_backgroundChangeDuration);
        AddOriginalValue(_BattleGridAnimator_fadeDuration);
        AddOriginalValue(_BattleGridAnimator_staggerDelay);
        AddOriginalValue(_DynamicDuelCamera_smoothTime);
    }

    private void AddOriginalValue(FloatReference floatRef)
    {
        if (floatRef != null && !_originalFloatValues.ContainsKey(floatRef))
        {
            _originalFloatValues.Add(floatRef, floatRef.Value);
        }
    }
    #endregion
}