// НАЗНАЧЕНИЕ: Основной компонент-контейнер для всех боевых юнитов на сцене. Отвечает за агрегацию всех подсистем (здоровье, статы, движение и т.д.) и управление их жизненным циклом.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: BattleUnitStats, BattleUnitHealth, BattleUnitMovement, AIBrain, BattleCell.
// ПРИМЕЧАНИЕ: Является центральной точкой доступа ко всем компонентам юнита. Управляет статическими списками для легкого доступа к герою и врагам.
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class BattleUnit : MonoBehaviour
{
    public enum Faction { NotSet, Hero, Friendly, Enemy }
    public enum Direction { Left, Right }
    public enum UnitLevel
    {
        [LabelText("0 - Зеленый (Обычный)")] Green_00 = 0,
        [LabelText("1 - Голубой (Базовый)")] Blue_01 = 1,
        [LabelText("2 - Желтый (Усиленный)")] Yellow_02 = 2,
        [LabelText("3 - Оранжевый (Ветеран)")] Orange_03 = 3,
        [LabelText("4 - Красный (Опасный)")] Red_04 = 4,
        [LabelText("5 - Розовый (Стремительный)")] Pink_05 = 5,
        [LabelText("6 - Бирюзовый (Дух стихий)")] Turquoise_06 = 6,
        [LabelText("7 - Синий (Элитный)")] DarkBlue_07 = 7,
        [LabelText("8 - Фиолетовый (Мощный)")] Violet_08 = 8,
        [LabelText("9 - Бордовый (Яростный)")] Maroon_09 = 9,
        [LabelText("10 - Индиго (Мистик)")] Indigo_10 = 10,
        [LabelText("11 - Серый (Каменный)")] Grey_11 = 11,
        [LabelText("12 - Коричневый (Стойкий)")] Brown_12 = 12,
        [LabelText("13 - Серебряный (Чемпион)")] Silver_13 = 13,
        [LabelText("14 - Золотой (Легендарный)")] Gold_14 = 14,
        [LabelText("15 - Белый (Призрачный)")] White_15 = 15,
        [LabelText("16 - Черный (Проклятый)")] Black_16 = 16
    }
    public enum UnitType_DEPRECATED { NotSet, Melee, Ranged, Mixed }

    /// <summary>
    /// Событие, вызываемое после того, как юнит полностью завершил свой ход.
    /// </summary>
    public event UnityAction OnTurnCompleted;
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _mainBodyPart;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Transform _damagePoint;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitState _unitState;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitStats _unitStats;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitHealth _unitHealth;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitAbilities _unitAbilities;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitMovement _unitMovement;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitArsenal _unitArsenal;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitAnimator _unitAnimator;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitUI _unitUI;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitActions _unitActions;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitTargetingSystem _targetingSystem;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private AIBrain _aiBrain;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private Faction _faction;
    [BoxGroup("SETTINGS"), SerializeField] private UnitType_DEPRECATED _currentUnitType;
    [BoxGroup("SETTINGS"), SerializeField, OnValueChanged("OnUnitLevelChangedInInspector")] private UnitLevel _unitLevel;
    [BoxGroup("SETTINGS"), SerializeField] private Direction _watchingDirection = Direction.Left;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private bool _isAlive = true;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    /// <summary>
    /// Статическая ссылка на главного героя.
    /// </summary>
    public static BattleUnit Hero { get; private set; }
    /// <summary>
    /// Статический список всех вражеских юнитов на сцене.
    /// </summary>
    public static List<BattleUnit> Enemies { get; private set; } = new List<BattleUnit>();
    /// <summary>
    /// Фракция, к которой принадлежит юнит.
    /// </summary>
    public Faction FactionType { get => _faction; }
    /// <summary>
    /// Текущий уровень юнита.
    /// </summary>
    public UnitLevel Level { get => _unitLevel; }
    /// <summary>
    /// (Устаревший) Тип юнита.
    /// </summary>
    public UnitType_DEPRECATED CurrentUnitType { get => _currentUnitType; }
    /// <summary>
    /// "Якорная" позиция юнита на сетке (обычно левая нижняя).
    /// </summary>
    public Vector2Int CurrentPosition { get => _unitMovement.CurrentPosition; }
    /// <summary>
    /// Жив ли юнит.
    /// </summary>
    public bool IsAlive { get => _isAlive; }
    /// <summary>
    /// Занят ли юнит в данный момент (двигается или атакует).
    /// </summary>
    public bool IsBusy => Movement.IsMoving || Actions.IsAttacking;
    /// <summary>
    /// Направление, в которое смотрит юнит.
    /// </summary>
    public Direction WatchingDirection { get => _watchingDirection; set => _watchingDirection = value; }
    /// <summary>
    /// Точка, из которой наносится урон или применяются способности.
    /// </summary>
    public Transform DamagePoint { get => _damagePoint; }
    /// <summary>
    /// Основной GameObject, представляющий тело юнита.
    /// </summary>
    public GameObject MainBodyPart { get => _mainBodyPart; }
    public BattleUnitState State { get => _unitState; }
    public BattleUnitStats Stats { get => _unitStats; }
    public BattleUnitHealth Health { get => _unitHealth; }
    public BattleUnitAbilities Abilities { get => _unitAbilities; }
    public BattleUnitMovement Movement { get => _unitMovement; }
    public BattleUnitAnimator Animator { get => _unitAnimator; }
    public BattleUnitUI UI { get => _unitUI; }
    public BattleUnitArsenal Arsenal { get => _unitArsenal; }
    public BattleUnitActions Actions { get => _unitActions; }
    public AIBrain Brain { get => _aiBrain; }
    public BattleUnitTargetingSystem TargetingSystem { get => _targetingSystem; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (!_unitState) DebugUtils.LogMissingReference(this, nameof(_unitState));
        if (!_unitStats) DebugUtils.LogMissingReference(this, nameof(_unitStats));
        if (!_unitHealth) DebugUtils.LogMissingReference(this, nameof(_unitHealth));
        if (!_unitAbilities) DebugUtils.LogMissingReference(this, nameof(_unitAbilities));
        if (!_unitMovement) DebugUtils.LogMissingReference(this, nameof(_unitMovement));
        if (!_unitArsenal) DebugUtils.LogMissingReference(this, nameof(_unitArsenal));
        if (!_unitAnimator) DebugUtils.LogMissingReference(this, nameof(_unitAnimator));
        if (!_unitUI) DebugUtils.LogMissingReference(this, nameof(_unitUI));
        if (!_unitActions) DebugUtils.LogMissingReference(this, nameof(_unitActions));
        if (!_aiBrain) DebugUtils.LogMissingReference(this, nameof(_aiBrain));
        if (!_mainBodyPart) DebugUtils.LogMissingReference(this, nameof(_mainBodyPart));
        if (!_damagePoint) DebugUtils.LogMissingReference(this, nameof(_damagePoint));
        if (!_targetingSystem) DebugUtils.LogMissingReference(this, nameof(_targetingSystem));

        if (_faction == Faction.Hero)
        {
            if (Hero == null) { Hero = this; } else { DebugUtils.LogInstanceAlreadyExists(this, Hero); }
        }
        else if (_faction == Faction.Enemy)
        {
            if (!Enemies.Contains(this)) { Enemies.Add(this); }
        }
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnit ({0}):</color> Awake. Фракция: <color=yellow>{1}</color>.", _ColoredDebug, name, _faction);
    }

    private void OnEnable()
    {
        _unitHealth.OnDeath += HandleDeath;
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnit ({0}):</color> OnEnable. Подписался на событие OnDeath.", _ColoredDebug, name);
    }

    private void OnDisable()
    {
        _unitHealth.OnDeath -= HandleDeath;
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnit ({0}):</color> OnDisable. Отписался от события OnDeath.", _ColoredDebug, name);
    }

    private void OnDestroy()
    {
        if (_faction == Faction.Enemy)
        {
            if (Enemies.Contains(this))
            {
                Enemies.Remove(this);
                ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnit ({0}):</color> OnDestroy. Удален из списка врагов.", _ColoredDebug, name);
            }
        }
        if (Hero == this)
        {
            Hero = null;
            ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnit ({0}):</color> OnDestroy. Ссылка на героя очищена.", _ColoredDebug, name);
        }
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Инициализирует юнит в указанной ячейке.
    /// </summary>
    /// <param name="cell">"Якорная" ячейка для размещения юнита.</param>
    public void Initialize(BattleCell cell)
    {
        _unitStats.Initialize(_unitLevel);
        _unitHealth.Initialize(_unitStats.MaxHealth);
        _unitAbilities.Initialize();

        _unitUI.Initialize(this);

        _unitAnimator.Initialize(this);
        _unitMovement.Initialize(cell);
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnit:</color> Инициализирован на позиции <color=yellow>{0}</color>. Тип: <color=orange>{1}</color>, Уровень: <color=lime>{2}</color>.", _ColoredDebug, cell.Position, _currentUnitType, _unitLevel);
    }

    /// <summary>
    /// Начинает ход юнита.
    /// </summary>
    /// <param name="onTurnCompletedCallback">Действие, которое будет вызвано по завершении хода.</param>
    public void TakeTurn(UnityAction onTurnCompletedCallback)
    {
        OnTurnCompleted = onTurnCompletedCallback;
        if (State != null && State.HasFlag("DISABLED"))
        {
            ColoredDebug.CLog(gameObject, "<color=grey>BattleUnit ({0}):</color> Юнит отключен. Пропускаю ход.", _ColoredDebug, name);
            OnTurnCompleted?.Invoke();
            return;
        }

        if (_aiBrain != null)
        {
            _aiBrain.ExecuteTurn(OnTurnCompleted);
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=orange>BattleUnit ({0}):</color> AIBrain не найден. Ход пропущен.", _ColoredDebug, name);
            OnTurnCompleted?.Invoke();
        }
    }

    /// <summary>
    /// Изменяет уровень юнита и пересчитывает его характеристики.
    /// </summary>
    /// <param name="newLevel">Новый уровень для установки.</param>
    public void ChangeUnitLevel(UnitLevel newLevel)
    {
        if (newLevel == _unitLevel) return;
        _unitLevel = newLevel;
        UpdateLevelStats();
    }
    #endregion Публичные методы

    #region Личные методы
    /// <summary>
    /// Обработчик события смерти юнита.
    /// </summary>
    private void HandleDeath()
    {
        ColoredDebug.CLog(gameObject, "<color=red><b>BattleUnit ({0}):</b></color> <color=red><b>УНИЧТОЖЕН</b></color>.", _ColoredDebug, name);
        _isAlive = false;

        if (_faction == Faction.Enemy && EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy(this);
        }

        SoundManager.Instance.PlayOneShot(SoundType.Explosion);
        GameObject explosionEffect = ObjectPoolExplosion.Instance.GetObject();
        explosionEffect.transform.position = transform.position;
        if (_mainBodyPart != null)
        {
            _mainBodyPart.SetActive(false);
        }

        // 1. Создаем копию списка ячеек, которые нужно повредить
        var cellsToDamage = new List<BattleCell>(Movement.OccupiedCells);

        // 2. Вызываем метод очистки, который сбросит цвет и освободит ячейки
        Movement.ClearOccupation();

        // 3. Наносим урон ячейкам, которые были заняты
        foreach (var cell in cellsToDamage)
        {
            if (cell != null)
            {
                cell.TakeDamage();
            }
        }

        int finalBounty = (int)(_unitStats.Bounty * Settings.BountyMultiplier);
        if (finalBounty <= 0) finalBounty = 1;

        CurrencyManager.Instance.AddCurrency(finalBounty);
        _unitUI.ShowBountyText(finalBounty);
        Destroy(gameObject, 0.1f);
    }

    /// <summary>
    /// Обновляет характеристики юнита на основе его текущего уровня.
    /// </summary>
    private void UpdateLevelStats()
    {
        if (_unitStats == null || !_isAlive) return;
        float healthRatio = _unitStats.MaxHealth > 0 ? (float)_unitHealth.CurrentHealth / _unitStats.MaxHealth : 0;

        _unitStats.ApplyLevelBonus(_unitLevel);
        int newHealth = Mathf.RoundToInt(_unitStats.MaxHealth * healthRatio);
        if (newHealth <= 0 && healthRatio > 0) newHealth = 1;

        _unitHealth.SetHealth(newHealth, _unitStats.MaxHealth);
        _unitUI.UpdateHealthDisplay(_unitHealth.CurrentHealth, _unitStats.MaxHealth);
        _unitAnimator.SetUnitColorByLevel(_unitLevel);
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnit ({0}):</color> Уровень изменен на <color=yellow>{1}</color>. Новое здоровье: <color=lime>{2}/{3}</color>", _ColoredDebug, name, _unitLevel, _unitHealth.CurrentHealth, _unitStats.MaxHealth);
    }

    /// <summary>
    /// Метод, вызываемый Odin Inspector при изменении уровня юнита в редакторе.
    /// </summary>
    private void OnUnitLevelChangedInInspector()
    {
        UpdateLevelStats();
    }
    #endregion Личные методы
}
