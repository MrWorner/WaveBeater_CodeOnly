// НАЗНАЧЕНИЕ: Управляет перемещением юнита по боевой сетке, включая поиск пути, занятие/освобождение ячеек и анимацию движения.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnit, BattleGrid, BattleGridUtils, Pathfinding, DOTween.
// ПРИМЕЧАНИЕ: Содержит логику для движения юнитов, занимающих несколько клеток, и использует A* для поиска оптимального хода к цели, отдавая приоритет горизонтальному движению.
using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
public class BattleUnitMovement : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnit _battleUnit;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), Tooltip("На каком расстоянии (в клетках) юнит начинает выравниваться по оси Y с целью, а не просто идти вперед."), SerializeField] private int _verticalAlignDistance = 3;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private Vector2Int _anchorPosition;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private List<BattleCell> _occupiedCells = new List<BattleCell>();
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private BattleCell[,] _grid;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    /// <summary> Якорная позиция юнита на сетке.
    /// </summary>
    public Vector2Int CurrentPosition => _anchorPosition;
    /// <summary> Список ячеек, занимаемых юнитом.
    /// </summary>
    public IReadOnlyList<BattleCell> OccupiedCells => _occupiedCells;
    /// <summary> Якорная ячейка юнита.
    /// </summary>
    public BattleCell CurrentCell => _occupiedCells.FirstOrDefault();
    /// <summary> Двигается ли юнит в данный момент.
    /// </summary>
    public bool IsMoving
    {
        get;
        private set;
    }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_battleUnit == null) DebugUtils.LogMissingReference(this, nameof(_battleUnit));
        if (BattleGrid.Instance != null)
        {
            _grid = BattleGrid.Instance.Grid;
        }
        else
        {
            Debug.LogError($"[BattleUnitMovement] Не удалось найти BattleGrid.Instance в Awake на объекте {gameObject.name}!");
        }
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Инициализирует компонент движения, размещая юнит на начальной ячейке.
    /// </summary>
    /// <param name="anchorCell">Якорная ячейка для размещения.</param>
    public void Initialize(BattleCell anchorCell)
    {
        if (_grid == null && BattleGrid.Instance != null)
        {
            _grid = BattleGrid.Instance.Grid;
        }
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitMovement ({0}):</color> Инициализация с якорной ячейкой <color=yellow>{1}</color>.", _ColoredDebug, _battleUnit.name, anchorCell.Position);
        SetPositionAndOccupy(anchorCell, Settings.MovementSpeedDuringSpawn);
    }

    /// <summary>
    /// Выполняет захват новой клетки и анимацию.
    /// Не очищает старую клетку! Вызывается из TurnManager.
    /// </summary>
    /// <param name="newAnchorPos">Новая якорная позиция.</param>
    /// <param name="onCompleted">Callback по завершении движения.</param>
    public void ExecuteMove(Vector2Int newAnchorPos, UnityAction onCompleted)
    {
        if (_grid == null) _grid = BattleGrid.Instance.Grid;
        ColoredDebug.CLog(gameObject, "<color=lime>🧭 ЗАХВАТ КЛЕТКИ (ExecuteMove) ({0}):</color> Новая позиция: {1}",
             _ColoredDebug, _battleUnit.name, newAnchorPos);
        BattleCell newAnchorCell = _grid[newAnchorPos.x, newAnchorPos.y];
        SetPositionAndOccupy(newAnchorCell, Settings.MovementDuringBattleSpeed, onCompleted);
        SoundManager.Instance.PlayOneShot(SoundType.RoboticMove);
    }

    /// <summary>
    /// Принудительно повторно занимает клетки на текущей позиции.
    /// Используется для синхронизации состояния.
    /// </summary>
    public void ReoccupyCurrentCells()
    {
        if (BattleGrid.Instance != null)
        {
            _grid = BattleGrid.Instance.Grid;
        }

        if (BattleGrid.Instance == null || _grid == null)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>BattleUnitMovement ({0}):</color> Не могу повторно занять ячейки, BattleGrid недоступен.", _ColoredDebug, _battleUnit.name);
            return;
        }
        BattleCell currentAnchorCell = _grid[_anchorPosition.x, _anchorPosition.y];
        if (currentAnchorCell != null)
        {
            ColoredDebug.CLog(gameObject, "<color=yellow>--- BattleUnitMovement ({0}) ---</color> Принудительно повторно занимаю ячейки от якоря <color=lime>{1}</color>.", _ColoredDebug, _battleUnit.name, currentAnchorCell.Position);
            ClearOccupation();
            SetPositionAndOccupy(currentAnchorCell, 0f); // 0f - без анимации движения
        }
        else
        {
            Debug.LogError($"[BattleUnitMovement] ({_battleUnit.name}) Не удалось найти ячейку под юнитом для повторного занятия по координатам {_anchorPosition}.");
        }
    }

    /// <summary>
    /// Находит лучшую следующую позицию для движения к цели (герою), отдавая приоритет горизонтальному движению.
    /// </summary>
    /// <param name="unitForPathfinding">Ссылка на BattleUnit, выполняющего поиск (для Pathfinding).</param>
    /// <returns>Координаты следующего шага или текущую позицию, если движение невозможно или не требуется.</returns>
    public Vector2Int FindBestMove(BattleUnit unitForPathfinding)
    {
        var hero = BattleUnit.Hero;
        if (hero == null)
        {
            ColoredDebug.CLog(gameObject, "<color=red>BattleUnitMovement ({0}):</color> Герой не найден при поиске хода!", _ColoredDebug, _battleUnit.name);
            return _anchorPosition;
        }

        ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitMovement ({0}):</color> Поиск лучшего хода. Текущая: <color=yellow>{1}</color>, Цель (Герой): <color=yellow>{2}</color>", _ColoredDebug, _battleUnit.name, _anchorPosition, hero.CurrentPosition);
        // Проверяем, может ли юнит атаковать с текущей позиции (ближний бой)
        var meleeMode = _battleUnit.Arsenal?.GetAllAttackModes().FirstOrDefault(m => m.isMelee);
        if (meleeMode != null)
        {
            int distanceToHero = BattleGridUtils.GetDistance(_battleUnit, hero);
            ColoredDebug.CLog(gameObject, "<color=yellow>BattleUnitMovement ({0}):</color> Дистанция до героя: {1}, Дальность ближнего боя: {2}", _ColoredDebug, _battleUnit.name, distanceToHero, meleeMode.range);
            if (distanceToHero <= meleeMode.range && _battleUnit.Arsenal.CanUseMode(meleeMode))
            {
                ColoredDebug.CLog(gameObject, "<color=#90EE90>BattleUnitMovement ({0}):</color> Цель в зоне атаки ближнего боя. Остаюсь на месте.", _ColoredDebug, _battleUnit.name);
                return _anchorPosition;
            }
        }

        // Находим ближайшую точку на цели для A*
        Vector2Int targetPos = GetClosestPointOnTarget(hero);
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitMovement ({0}):</color> Целевая точка для A*: <color=lime>{1}</color>", _ColoredDebug, _battleUnit.name, targetPos);
        // Получаем путь от A*
        List<Vector2Int> astarPath = Pathfinding.FindPath(_anchorPosition, targetPos, _battleUnit.Stats.UnitSize, unitForPathfinding);
        if (astarPath.Count == 0)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>BattleUnitMovement ({0}):</color> A* не нашел путь к {1}. Остаюсь на месте.", _ColoredDebug, _battleUnit.name, targetPos);
            return _anchorPosition; // Путь не найден, стоим на месте
        }

        Vector2Int astarNextStep = astarPath[0];
        ColoredDebug.CLog(gameObject, "<color=grey>BattleUnitMovement ({0}):</color> A* предлагает шаг: <color=yellow>{1}</color>.", _ColoredDebug, _battleUnit.name, astarNextStep);
        // --- Логика приоритета горизонтального движения ---
        int deltaX = targetPos.x - _anchorPosition.x;
        int deltaY = targetPos.y - _anchorPosition.y;

        // Если нужно двигаться по горизонтали
        if (deltaX != 0)
        {
            int preferredHorizontalDir = System.Math.Sign(deltaX);
            Vector2Int horizontalStepPos = _anchorPosition + new Vector2Int(preferredHorizontalDir, 0);

            ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitMovement ({0}):</color> Проверка приоритетного горизонтального шага: <color=yellow>{1}</color>", _ColoredDebug, _battleUnit.name, horizontalStepPos);
            // Проверяем, проходим ли горизонтальный шаг (используя логику Pathfinding)
            // Передаем unitForPathfinding для корректной проверки союзников
            // Используем BattleGrid.Instance напрямую, т.к.
            _grid = BattleGrid.Instance.Grid; // Убедимся, что сетка актуальна
            // Проверяем проходимость всей области, которую займет юнит
            bool isHorizontalAreaWalkable = Pathfinding.IsAreaWalkableForUnit(horizontalStepPos, _battleUnit.Stats.UnitSize, BattleGrid.Instance, unitForPathfinding, _anchorPosition, targetPos, _battleUnit.name);
            if (isHorizontalAreaWalkable)
            {
                ColoredDebug.CLog(gameObject, "<color=#90EE90>BattleUnitMovement ({0}):</color> Приоритет отдан горизонтальному шагу: <color=lime>{1}</color>.", _ColoredDebug, _battleUnit.name, horizontalStepPos);
                return horizontalStepPos;
            }
            else
            {
                ColoredDebug.CLog(gameObject, "<color=orange>BattleUnitMovement ({0}):</color> Горизонтальный шаг {1} заблокирован или недоступен.", _ColoredDebug, _battleUnit.name, horizontalStepPos);
            }
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=grey>BattleUnitMovement ({0}):</color> Горизонтальное выравнивание достигнуто (deltaX = 0).", _ColoredDebug, _battleUnit.name);
        }

        // --- ИЗМЕНЕНИЕ ЛОГИКИ ---
        // Если горизонтальный шаг невозможен, проверяем шаг от A*.
        // НО: Запрещаем движение назад (увеличение X для врагов).
        if (astarNextStep.x > _anchorPosition.x)
        {
            ColoredDebug.CLog(gameObject, "<color=red>BattleUnitMovement ({0}):</color> A* предложил ход назад ({1}), но это запрещено. Остаюсь на месте.", _ColoredDebug, _battleUnit.name, astarNextStep);
            return _anchorPosition;
        }
        else
        {
            // Разрешаем ход (он либо вперед по X, либо вертикальный)
            ColoredDebug.CLog(gameObject, "<color=#90EE90>BattleUnitMovement ({0}):</color> Горизонтальный шаг заблокирован. Использую шаг A* (вперед/вертикально): <color=lime>{1}</color>.", _ColoredDebug, _battleUnit.name, astarNextStep);
            return astarNextStep;
        }
    }


    /// <summary>
    /// Ищет разрушаемое препятствие на пути к герою (упрощенная версия).
    /// </summary>
    /// <returns>GameObject препятствия или null.</returns>
    public GameObject FindDestructibleObstacleOnPath()
    {
        var hero = BattleUnit.Hero;
        if (hero == null)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>BattleUnitMovement ({0}):</color> Не могу найти препятствие, герой не найден.", _ColoredDebug, _battleUnit.name);
            return null;
        }

        // 1. Определяем направление к цели
        Vector2Int moveDirection = Vector2Int.zero;
        // Используем GetClosestPointOnTarget, чтобы правильно определить deltaX для юнитов > 1x1
        Vector2Int closestHeroCell = GetClosestPointOnTarget(hero);
        int deltaX = closestHeroCell.x - _anchorPosition.x;

        if (deltaX != 0)
        {
            // Приоритет всегда у горизонтального движения
            moveDirection = new Vector2Int(System.Math.Sign(deltaX), 0);
        }
        else // Если по X уже выровнены, проверяем Y
        {
            int deltaY = closestHeroCell.y - _anchorPosition.y;
            if (deltaY != 0)
            {
                moveDirection = new Vector2Int(0, System.Math.Sign(deltaY));
            }
            else
            {
                return null; // Стоим вплотную к цели, нечего ломать
            }
        }

        // 2. Проверяем область ПРЯМО по направлению движения
        // (Мы проверяем область размером с самого юнита, чтобы корректно работать с большими юнитами)
        Vector2Int areaToAttackPos = _anchorPosition + moveDirection;
        GameObject blocker = CheckAreaForSingleDestructible(areaToAttackPos, _battleUnit.Stats.UnitSize);

        if (blocker != null)
        {
            ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitMovement ({0}):</color> Найден разрушаемый блок ПРЯМО на пути: <color=yellow>{1}</color> на якорной позиции <color=lime>{2}</color>.", _ColoredDebug, _battleUnit.name, blocker.name, areaToAttackPos);
            return blocker;
        }

        ColoredDebug.CLog(gameObject, "<color=grey>BattleUnitMovement ({0}):</color> Прямо на пути ({1}) разрушаемых препятствий не найдено.", _ColoredDebug, _battleUnit.name, areaToAttackPos);
        return null;
    }

    /// <summary>
    /// Освобождает все ячейки, занимаемые юнитом.
    /// Вызывается из TurnManager в Фазе 3.A.
    /// </summary>
    public void ClearOccupation()
    {
        if (_occupiedCells.Count == 0)
        {
            //ColoredDebug.CLog(gameObject, "<color=orange>BattleUnitMovement ({0}):</color> Попытка очистки, но ячеек нет. Пропускаю.", _ColoredDebug, _battleUnit.name);
            return;
        }

        ColoredDebug.CLog(gameObject, "<color=orange>BattleUnitMovement ({0}):</color> Очищаю <color=yellow>{1}</color> занятых ячеек.", _ColoredDebug, _battleUnit.name, _occupiedCells.Count);
        foreach (var cell in _occupiedCells)
        {
            if (cell != null)
            {
                // ЛОГ: Проверяем, действительно ли мы освобождаем клетку от СЕБЯ
                if (cell.Occupant == (object)_battleUnit)
                {

                    cell.SetOccupant(null);
                    ColoredDebug.CLog(gameObject, "<color=grey>BattleUnitMovement ({0}):</color> Ячейка <color=yellow>{1}</color> освобождена.", _ColoredDebug, _battleUnit.name, cell.Position);
                }
                else
                {
                    // Этот лог может выявить баги, если мы пытаемся очистить клетку, занятую кем-то другим
                    string occupantName = (cell.Occupant as Component)?.gameObject.name
                                             ?? cell.Occupant?.GetType().Name ?? "NULL";
                    ColoredDebug.CLog(gameObject, "<color=red>BattleUnitMovement ({0}):</color> ПЫТАЛСЯ ОЧИСТИТЬ ячейку {1}, но она занята <color=yellow>{2}</color>! Очистка отменена для этой ячейки.", true, _battleUnit.name, cell.Position, occupantName);
                }
            }
            else
            {
                ColoredDebug.CLog(gameObject, "<color=orange>BattleUnitMovement ({0}):</color> Пропущена null ячейка при очистке.", _ColoredDebug, _battleUnit.name);
            }
        }
        _occupiedCells.Clear();
    }
    #endregion Публичные методы

    #region Личные методы
    /// <summary>
    /// Находит ближайшую к юниту точку на "теле" цели.
    /// </summary>
    /// <param name="target">Цель.</param>
    /// <returns>Координаты ближайшей клетки на цели.</returns>
    private Vector2Int GetClosestPointOnTarget(BattleUnit target)
    {
        if (target.Movement == null || target.Movement.OccupiedCells == null || target.Movement.OccupiedCells.Count == 0)
        {
            ColoredDebug.CLog(gameObject, "<color=yellow>BattleUnitMovement ({0}):</color> Не могу получить занятые клетки цели '{1}', использую CurrentPosition {2}", _ColoredDebug, _battleUnit.name, target.name, target.CurrentPosition);
            return target.CurrentPosition;
        }

        Vector2Int bestTargetPoint = target.CurrentPosition;
        int minDistance = int.MaxValue;
        // Перебираем все клетки, занимаемые целью
        foreach (var targetCell in target.Movement.OccupiedCells)
        {
            if (targetCell == null) continue; // Проверка на null

            // Используем BattleGridUtils.GetDistance для расчета расстояния между областями (юнитом и клеткой цели)
            int dist = BattleGridUtils.GetDistance(_anchorPosition, _battleUnit.Stats.UnitSize, targetCell.Position, Vector2Int.one);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestTargetPoint = targetCell.Position;
            }
        }

        ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitMovement ({0}):</color> Ближайшая точка на цели '{1}': <color=lime>{2}</color> (дистанция: {3})", _ColoredDebug, _battleUnit.name, target.name, bestTargetPoint, minDistance);
        return bestTargetPoint;
    }

    /// <summary>
    /// Проверяет, содержит ли указанная область только ОДИН разрушаемый объект.
    /// </summary>
    /// <param name="anchorPos">Якорная позиция проверяемой области.</param>
    /// <param name="size">Размер проверяемой области.</param>
    /// <returns>GameObject разрушаемого объекта или null.</returns>
    private GameObject CheckAreaForSingleDestructible(Vector2Int anchorPos, Vector2Int size)
    {
        HashSet<GameObject> occupants = new HashSet<GameObject>();
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int checkPos = anchorPos + new Vector2Int(x, y);
                if (!IsInsideGrid(checkPos))
                {
                    //ColoredDebug.CLog(gameObject, "<color=orange>CheckAreaForSingleDestructible ({0}):</color> Позиция {1} вне сетки.", _ColoredDebug, _battleUnit.name, checkPos);
                    return null; // Область выходит за пределы - точно не подходит
                }
                BattleCell cell = _grid[checkPos.x, checkPos.y];
                if (cell == null)
                {
                    //ColoredDebug.CLog(gameObject, "<color=orange>CheckAreaForSingleDestructible ({0}):</color> Ячейка {1} не найдена (null).", _ColoredDebug, _battleUnit.name, checkPos);
                    return null; // Отсутствующая клетка - не подходит
                }
                if (cell.Occupant != null)
                {
                    if (cell.Occupant is GameObject propObj)

                    {
                        occupants.Add(propObj);
                    }
                    else // Если занято чем-то другим (например, юнитом)
                    {
                        //ColoredDebug.CLog(gameObject, "<color=grey>CheckAreaForSingleDestructible ({0}):</color> Ячейка {1} занята не-пропом ({2}).", _ColoredDebug, _battleUnit.name, checkPos, cell.Occupant.GetType().Name);
                        return null; // Занято не тем - не подходит
                    }
                }
            }
        }

        if (occupants.Count == 1)
        {
            GameObject singleOccupant = occupants.First();
            if (singleOccupant.TryGetComponent<Prop>(out var propComponent))
            {
                if (propComponent.PropSO != null && propComponent.PropSO.IsDestructible)
                {
                    //ColoredDebug.CLog(gameObject, "<color=lime>CheckAreaForSingleDestructible ({0}):</color> Найден единственный разрушаемый проп: {1}.", _ColoredDebug, _battleUnit.name, singleOccupant.name);
                    return singleOccupant;
                }
                //else { ColoredDebug.CLog(gameObject, "<color=grey>CheckAreaForSingleDestructible ({0}):</color> Единственный объект {1} не является разрушаемым.", _ColoredDebug, _battleUnit.name, singleOccupant.name); }
            }
            //else { ColoredDebug.CLog(gameObject, "<color=grey>CheckAreaForSingleDestructible ({0}):</color> Единственный объект {1} не имеет компонента Prop.", _ColoredDebug, _battleUnit.name, singleOccupant.name); }
        }
        //else if (occupants.Count > 1) { ColoredDebug.CLog(gameObject, "<color=grey>CheckAreaForSingleDestructible ({0}):</color> Найдено несколько объектов ({1}).", _ColoredDebug, _battleUnit.name, occupants.Count); }
        //else { ColoredDebug.CLog(gameObject, "<color=grey>CheckAreaForSingleDestructible ({0}):</color> В области нет объектов.", _ColoredDebug, _battleUnit.name); }

        return null; // Не найдено или найдено больше одного/неразрушаемый
    }

    /// <summary>
    /// Устанавливает новую якорную позицию, занимает соответствующие клетки и анимирует перемещение юнита к центру занятой области.
    /// </summary>
    /// <param name="anchorCell">Новая якорная клетка.</param>
    /// <param name="moveDuration">Длительность анимации (0 для мгновенного перемещения).</param>
    /// <param name="onCompleted">Callback по завершении анимации.</param>
    private void SetPositionAndOccupy(BattleCell anchorCell, float moveDuration, UnityAction onCompleted = null)
    {
        if (anchorCell == null)
        {
            Debug.LogError($"[BattleUnitMovement] ({_battleUnit.name}) ОШИБКА! anchorCell равен null при попытке SetPositionAndOccupy.");
            IsMoving = false; // Сбрасываем флаг, если ошибка
            onCompleted?.Invoke();
            return;
        }

        _anchorPosition = anchorCell.Position;
        _occupiedCells.Clear();

        Vector3 centerPosition = Vector3.zero;
        Vector2Int size = _battleUnit.Stats.UnitSize;

        ColoredDebug.CLog(gameObject, "<color=lime>BattleUnitMovement ({0}):</color> Занимаю ячейки от якоря <color=yellow>{1}</color>. Размер: <color=cyan>{2}</color>.", _ColoredDebug, _battleUnit.name, _anchorPosition, size);
        /* // Отладочный лог, может быть слишком частым
        if (anchorCell.IsOccupied())
        {
            string occupantName = (anchorCell.Occupant as Component)?.gameObject.name ??
anchorCell.Occupant?.GetType().Name ?? "UNKNOWN";
            ColoredDebug.CLog(gameObject, "<color=orange>BattleUnitMovement ({0}):</color> Якорная клетка {1} УЖЕ ЗАНЯТА <color=yellow>{2}</color> перед захватом!", _ColoredDebug, _battleUnit.name, _anchorPosition, occupantName);
        }*/

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int cellPos = _anchorPosition + new Vector2Int(x, y);
                if (IsInsideGrid(cellPos))
                {
                    BattleCell cellToOccupy = _grid[cellPos.x, cellPos.y];
                    if (cellToOccupy == null)
                    {
                        ColoredDebug.CLog(gameObject, "<color=orange>BattleUnitMovement ({0}):</color> Попытка занять null ячейку {1}.", _ColoredDebug, _battleUnit.name, cellPos);
                        continue;
                    }

                    // Проверяем, не занята ли уже клетка
                    if (cellToOccupy.IsOccupied() && cellToOccupy.Occupant != (object)_battleUnit)
                    {
                        string occupantName = (cellToOccupy.Occupant
as Component)?.gameObject.name ?? cellToOccupy.Occupant?.GetType().Name ?? "UNKNOWN";
                        ColoredDebug.CLog(gameObject, "<color=red>BattleUnitMovement ({0}):</color> КЛЕТКА {1} УЖЕ ЗАНЯТА <color=yellow>{2}</color>! Все равно занимаю.", true, _battleUnit.name, cellPos, occupantName);
                    }

                    cellToOccupy.SetOccupant(_battleUnit);
                    _occupiedCells.Add(cellToOccupy);
                    centerPosition += cellToOccupy.WorldPosition;
                    //ColoredDebug.CLog(gameObject, "<color=grey>BattleUnitMovement ({0}):</color> Ячейка {1} занята.", _ColoredDebug, _battleUnit.name, cellPos);
                }
                else
                {
                    Debug.LogError($"[BattleUnitMovement] ({_battleUnit.name}) ОШИБКА! Попытка занять ячейку {cellPos} вне границ сетки!");
                }
            }
        }

        if (_occupiedCells.Count > 0)
        {
            centerPosition /= _occupiedCells.Count;
            //ColoredDebug.CLog(gameObject, "<color=grey>BattleUnitMovement ({0}):</color> Рассчитана центральная позиция: {1}.", _ColoredDebug, _battleUnit.name, centerPosition);
        }
        else
        {
            centerPosition = anchorCell.WorldPosition; // Fallback
            ColoredDebug.CLog(gameObject, "<color=orange>BattleUnitMovement ({0}):</color> Не удалось занять ни одной ячейки! Используется позиция якорной ячейки {1}.", _ColoredDebug, _battleUnit.name, centerPosition);
        }

        if (moveDuration > 0)
        {
            IsMoving = true; // Устанавливаем флаг перед началом анимации
            ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitMovement ({0}):</color> Запуск анимации перемещения к {1} за {2} сек.", _ColoredDebug, _battleUnit.name, centerPosition, moveDuration);
            _battleUnit.transform.DOMove(centerPosition, moveDuration).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                IsMoving = false; // Сбрасываем флаг по завершении
                ColoredDebug.CLog(gameObject, "<color=green>BattleUnitMovement ({0}):</color> Анимация перемещения завершена.", _ColoredDebug, _battleUnit.name);
                onCompleted?.Invoke();
            });
        }
        else // Мгновенное перемещение
        {
            _battleUnit.transform.position = centerPosition;
            ColoredDebug.CLog(gameObject, "<color=green>BattleUnitMovement ({0}):</color> Мгновенное перемещение выполнено в {1}.", _ColoredDebug, _battleUnit.name, centerPosition);
            IsMoving = false; // Сбрасываем флаг
            onCompleted?.Invoke();
        }
    }

    /// <summary>
    /// Проверяет, находится ли позиция в пределах сетки.
    /// </summary>
    /// <param name="p">Позиция для проверки.</param>
    /// <returns>True, если позиция внутри сетки.</returns>
    private bool IsInsideGrid(Vector2Int p)
    {
        return _grid != null && p.x >= 0 && p.x < _grid.GetLength(0) && p.y >= 0 && p.y < _grid.GetLength(1);
    }
    #endregion Личные методы
}