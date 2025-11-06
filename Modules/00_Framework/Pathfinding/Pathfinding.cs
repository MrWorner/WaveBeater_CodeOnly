// НАЗНАЧЕНИЕ: Предоставляет статические методы для поиска пути на BattleGrid с использованием алгоритма A*.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleGrid, BattleUnit, BattleCell.
// ПРИМЕЧАНИЕ: Алгоритм учитывает размер юнита и проходимость клеток, включая временное игнорирование союзников во время поиска.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Pathfinding
{
    #region Поля
    private static bool _ColoredDebug => false;
    #endregion Поля

    #region Внутренние классы
    /// <summary>
    /// Представляет узел в графе поиска пути A*.
    /// </summary>
    private class Node
    {
        public Vector2Int Position;
        public Node Parent;
        public int GCost; // Стоимость пути от старта до этого узла
        public int HCost; // Эвристическая стоимость пути от этого узла до цели
        public int FCost => GCost + HCost; // Общая стоимость узла

        public Node(Vector2Int position)
        {
            Position = position;
        }

        public override string ToString()
        {
            return $"Node({Position}, F={FCost}, G={GCost}, H={HCost})";
        }
    }
    #endregion Внутренние классы

    #region Публичные методы
    /// <summary>
    /// Находит кратчайший путь от стартовой до целевой позиции для юнита указанного размера, используя алгоритм A*.
    /// </summary>
    /// <param name="startPos">Якорная позиция начала пути.</param>
    /// <param name="targetPos">Якорная позиция цели пути.</param>
    /// <param name="unitSize">Размер юнита (ширина, высота).</param>
    /// <param name="movingUnitInstance"> (Необязательно) Экземпляр BattleUnit, который ищет путь. Используется для определения союзников.</param>
    /// <returns>Список позиций (Vector2Int), представляющий путь (не включая стартовую). Пустой список, если путь не найден.</returns>
    public static List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int targetPos, Vector2Int unitSize, BattleUnit movingUnitInstance = null)
    {
        // --- Получаем юнита ДО начала основного цикла для логирования ---
        BattleUnit movingUnit = movingUnitInstance ?? GetUnitAtPosition(startPos);
        string movingUnitName = movingUnit != null ? movingUnit.name : $"NULL_at_{startPos}";

        ColoredDebug.CLog(null, $"<color=#FFA07A>Pathfinding ({movingUnitName}):</color> Request path from <color=yellow>{startPos}</color> to <color=yellow>{targetPos}</color> for unit size <color=cyan>{unitSize}</color>.", _ColoredDebug);
        var grid = BattleGrid.Instance;
        if (grid == null || grid.Grid == null)
        {
            Debug.LogError($"[Pathfinding ({movingUnitName})] BattleGrid not initialized!");
            return new List<Vector2Int>();
        }

        if (movingUnit == null)
        {
            ColoredDebug.CLog(null, $"<color=red>Pathfinding:</color> CRITICAL! movingUnit is NULL for start position {startPos}. Pathfinding might fail to identify allies correctly.", true);
        }

        var openList = new List<Node>();
        var closedList = new HashSet<Vector2Int>();

        Node startNode = new Node(startPos);
        startNode.GCost = 0;
        startNode.HCost = GetDistance(startPos, targetPos);
        openList.Add(startNode);

        int iterations = 0;
        const int maxIterations = 5000;

        while (openList.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            Node currentNode = openList.OrderBy(n => n.FCost).ThenBy(n => n.HCost).First();
            openList.Remove(currentNode);
            closedList.Add(currentNode.Position);

            if (currentNode.Position == targetPos)
            {
                ColoredDebug.CLog(null, $"<color=lime>Pathfinding ({movingUnitName}):</color> Path found in <color=yellow>{iterations}</color> iterations.", _ColoredDebug);
                return RetracePath(startNode, currentNode);
            }

            foreach (var neighbourPos in GetNeighbours(currentNode.Position))
            {
                if (closedList.Contains(neighbourPos)) continue;
                if (!IsAreaWalkableForUnit(neighbourPos, unitSize, grid, movingUnit, startPos, targetPos, movingUnitName)) continue;

                int moveCost = GetDistance(currentNode.Position, neighbourPos);
                int gCostNeigh = currentNode.GCost + moveCost;
                int hCostNeigh = GetDistance(neighbourPos, targetPos);

                Node existingNode = openList.FirstOrDefault(n => n.Position == neighbourPos);
                if (existingNode == null)
                {
                    Node neighbourNode = new Node(neighbourPos)
                    {
                        GCost = gCostNeigh,
                        HCost = hCostNeigh,
                        Parent = currentNode
                    };
                    openList.Add(neighbourNode);
                }
                else if (gCostNeigh < existingNode.GCost)
                {
                    existingNode.GCost = gCostNeigh;
                    existingNode.Parent = currentNode;
                }
            }
        }

        if (openList.Count == 0 && iterations < maxIterations)
        {
            ColoredDebug.CLog(null, $"<color=orange>Pathfinding ({movingUnitName}):</color> Direct path FAILED. Open list empty after {iterations} iterations. Searching nearest...", _ColoredDebug);
        }
        else if (iterations >= maxIterations)
        {
            ColoredDebug.CLog(null, $"<color=red>Pathfinding ({movingUnitName}):</color> Direct path FAILED. Max iterations ({maxIterations}) reached. Searching nearest...", true);
        }

        return FindPathToNearestWalkable(startPos, targetPos, unitSize, movingUnit, movingUnitName);
    }

    /// <summary>
    /// Рассчитывает Манхэттенское расстояние (стоимость перемещения по сетке без диагоналей), умноженное на 10.
    /// </summary>
    /// <param name="posA">Первая позиция.</param>
    /// <param name="posB">Вторая позиция.</param>
    /// <returns>Стоимость H эвристики.</returns>
    public static int GetDistance(Vector2Int posA, Vector2Int posB)
    {
        int dstX = Mathf.Abs(posA.x - posB.x);
        int dstY = Mathf.Abs(posA.y - posB.y);
        return 10 * (dstX + dstY);
    }

    // --- ИЗМЕНЕНИЕ: Метод сделан public static ---
    /// <summary>
    /// Проверяет, является ли вся область, занимаемая юнитом (если его якорь в anchorPos), проходимой.
    /// </summary>
    /// <param name="anchorPos">Якорная позиция проверяемой области.</param>
    /// <param name="unitSize">Размер юнита.</param>
    /// <param name="grid">Ссылка на сетку.</param>
    /// <param name="movingUnit">Юнит, который пытается найти путь.</param>
    /// <param name="startPos">Стартовая позиция пути (для проверки).</param>
    /// <param name="targetPos">Целевая позиция пути (для проверки).</param>
    /// <param name="movingUnitName">Имя юнита для логирования.</param>
    /// <returns>True, если вся область проходима.</returns>
    public static bool IsAreaWalkableForUnit(Vector2Int anchorPos, Vector2Int unitSize, BattleGrid grid, BattleUnit movingUnit, Vector2Int startPos, Vector2Int targetPos, string movingUnitName)
    {
        for (int x = 0; x < unitSize.x; x++)
        {
            for (int y = 0; y < unitSize.y; y++)
            {
                Vector2Int cellPos = anchorPos + new Vector2Int(x, y);
                if (!IsSingleCellWalkableForUnit(cellPos, grid, movingUnit, startPos, targetPos, movingUnitName))
                {
                    return false;
                }
            }
        }
        return true;
    }

    // --- ИЗМЕНЕНИЕ: Метод сделан public static ---
    /// <summary>
    /// Проверяет проходимость ОДНОЙ клетки сетки для ДАННОГО юнита (movingUnit).
    /// </summary>
    /// <param name="pos">Проверяемая позиция клетки.</param>
    /// <param name="grid">Ссылка на сетку.</param>
    /// <param name="movingUnit">Юнит, который пытается найти путь (может быть null!).</param>
    /// <param name="startPos">Стартовая позиция пути.</param>
    /// <param name="targetPos">Целевая позиция пути.</param>
    /// <param name="movingUnitName">Имя юнита для логирования.</param>
    /// <returns>True, если клетка проходима.</returns>
    public static bool IsSingleCellWalkableForUnit(Vector2Int pos, BattleGrid grid, BattleUnit movingUnit, Vector2Int startPos, Vector2Int targetPos, string movingUnitName)
    {
        // 1. Базовые проверки: границы, существование клетки, проходимость
        if (pos.x < 0 || pos.x >= grid.Width || pos.y < 0 || pos.y >= grid.Height)
        {
            // ColoredDebug.CLog(null, $"<color=grey>Pathfinding ({movingUnitName}):</color> Cell {pos} unwalkable (Out of bounds).", _ColoredDebug);
            return false;
        }
        BattleCell cell = grid.GetCell(pos);
        if (cell == null || !cell.IsPassable)
        {
            string reason = cell == null ? "NULL" : (!cell.IsPassable ? "Not Passable (Hole/Deactivated)" : "Unknown");
            // ColoredDebug.CLog(null, $"<color=grey>Pathfinding ({movingUnitName}):</color> Cell {pos} unwalkable ({reason}).", _ColoredDebug);
            return false;
        }

        // 2. Проверка, является ли клетка одной из стартовых клеток двигающегося юнита
        bool isStartingCell = false;
        if (movingUnit != null && movingUnit.Movement != null && movingUnit.Movement.OccupiedCells != null)
        {
            isStartingCell = movingUnit.Movement.OccupiedCells.Any(occupiedCell => occupiedCell != null && occupiedCell.Position == pos);
        }

        if (isStartingCell)
        {
            // ColoredDebug.CLog(null, $"<color=cyan>Pathfinding ({movingUnitName}):</color> Cell {pos} walkable (Starting cell).", _ColoredDebug);
            return true;
        }

        // 3. Проверка занятости клетки (если это НЕ стартовая клетка)
        if (cell.IsOccupied())
        {
            string occupantName = (cell.Occupant as Component)?.gameObject.name ?? cell.Occupant?.GetType().Name ?? "UNKNOWN";

            // 3a. Является ли целевой клеткой? Разрешаем двигаться "в" цель.
            if (pos == targetPos)
            {
                // ColoredDebug.CLog(null, $"<color=cyan>Pathfinding ({movingUnitName}):</color> Cell {pos} walkable (Target cell, occupied by {occupantName}).", _ColoredDebug);
                return true;
            }

            // 3b. Занята ли союзником?
            if (cell.Occupant is BattleUnit occupantUnit)
            {
                if (movingUnit != null)
                {
                    if (occupantUnit.FactionType == movingUnit.FactionType)
                    {
                        // Разрешаем проходить СКВОзь союзников во время поиска пути
                        // ColoredDebug.CLog(null, $"<color=cyan>Pathfinding ({movingUnitName}):</color> Cell {pos} walkable (Occupied by ally {occupantName}).", _ColoredDebug);
                        return true;
                    }
                    else // Занято врагом (не целью)
                    {
                        // ColoredDebug.CLog(null, $"<color=grey>Pathfinding ({movingUnitName}):</color> Cell {pos} unwalkable (Occupied by enemy {occupantName}).", _ColoredDebug);
                        return false;
                    }
                }
                else // movingUnit == NULL (!! Проблема !!)
                {
                    ColoredDebug.CLog(null, $"<color=red>Pathfinding ({movingUnitName}):</color> Cell {pos} UNWALKABLE! Occupied by Unit {occupantName}, but cannot check faction because movingUnit is NULL.", true);
                    return false;
                }
            }

            // 3c. Занята не-юнитом (пропом и т.д.)
            // ColoredDebug.CLog(null, $"<color=grey>Pathfinding ({movingUnitName}):</color> Cell {pos} unwalkable (Occupied by non-unit {occupantName}).", _ColoredDebug);
            return false;
        }

        // 4. Клетка пуста и проходима
        // ColoredDebug.CLog(null, $"<color=green>Pathfinding ({movingUnitName}):</color> Cell {pos} walkable (Empty and passable).", _ColoredDebug);
        return true;
    }

    #endregion Публичные методы

    #region Личные методы
    /// <summary>
    /// Находит путь к ближайшей проходимой для юнита якорной позиции рядом с недостижимой целью.
    /// </summary>
    /// <param name="startPos">Стартовая позиция.</param>
    /// <param name="targetPos">Изначальная (недостижимая) целевая позиция.</param>
    /// <param name="unitSize">Размер юнита.</param>
    /// <param name="movingUnit">Юнит, ищущий путь.</param>
    /// <param name="movingUnitName">Имя юнита для логов.</param>
    /// <returns>Путь к ближайшей доступной точке или пустой список.</returns>
    private static List<Vector2Int> FindPathToNearestWalkable(Vector2Int startPos, Vector2Int targetPos, Vector2Int unitSize, BattleUnit movingUnit, string movingUnitName)
    {
        ColoredDebug.CLog(null, $"<color=yellow>Pathfinding ({movingUnitName}):</color> Finding path to nearest walkable from {startPos} towards {targetPos}.", _ColoredDebug);
        var grid = BattleGrid.Instance;
        if (grid == null || grid.Grid == null)
        {
            Debug.LogError($"[Pathfinding ({movingUnitName})] Grid is null during FindPathToNearestWalkable!");
            return new List<Vector2Int>();
        }

        List<Vector2Int> candidates = new List<Vector2Int>();
        bool foundCandidatesInRadius = false;
        const int maxSearchRadius = 8;

        for (int radius = 1; radius <= maxSearchRadius; radius++)
        {
            candidates.Clear();

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) < radius && Mathf.Abs(y) < radius) continue;
                    Vector2Int checkPos = targetPos + new Vector2Int(x, y);

                    if (IsAreaWalkableForUnit(checkPos, unitSize, grid, movingUnit, startPos, targetPos, movingUnitName))
                    {
                        candidates.Add(checkPos);
                    }
                }
            }

            if (candidates.Count > 0)
            {
                ColoredDebug.CLog(null, $"<color=cyan>Pathfinding ({movingUnitName}):</color> Found {candidates.Count} candidate positions at radius {radius}.", _ColoredDebug);
                foundCandidatesInRadius = true;

                foreach (var candidate in candidates.OrderBy(c => GetDistance(c, targetPos)))
                {
                    ColoredDebug.CLog(null, $"<color=grey>Pathfinding ({movingUnitName}):</color> Attempting direct path to candidate {candidate} (Dist to original target: {GetDistance(candidate, targetPos)}).", _ColoredDebug);
                    var path = FindDirectPath(startPos, candidate, unitSize, movingUnit, movingUnitName);
                    if (path.Count > 0)
                    {
                        ColoredDebug.CLog(null, $"<color=lime>Pathfinding ({movingUnitName}):</color> Found alternative path to position <color=yellow>{candidate}</color>", _ColoredDebug);
                        return path;
                    }
                    else
                    {
                        ColoredDebug.CLog(null, $"<color=grey>Pathfinding ({movingUnitName}):</color> Direct path to candidate {candidate} failed.", _ColoredDebug);
                    }
                }
                break; // Прекращаем поиск, если кандидаты в радиусе не дали путь
            }
        }

        if (!foundCandidatesInRadius)
        {
            ColoredDebug.CLog(null, $"<color=orange>Pathfinding ({movingUnitName}):</color> No walkable candidate positions found within radius {maxSearchRadius} of {targetPos}.", _ColoredDebug);
        }
        ColoredDebug.CLog(null, $"<color=red>Pathfinding ({movingUnitName}):</color> Failed to find path even to the nearest walkable point.", _ColoredDebug);
        return new List<Vector2Int>();
    }

    /// <summary>
    /// Упрощенный поиск прямого пути A* (используется для поиска пути к кандидатам в FindPathToNearestWalkable).
    /// </summary>
    /// <param name="startPos">Стартовая позиция.</param>
    /// <param name="targetPos">Целевая позиция (кандидат).</param>
    /// <param name="unitSize">Размер юнита.</param>
    /// <param name="movingUnit">Юнит, ищущий путь.</param>
    /// <param name="movingUnitName">Имя юнита для логов.</param>
    /// <returns>Найденный путь или пустой список.</returns>
    private static List<Vector2Int> FindDirectPath(Vector2Int startPos, Vector2Int targetPos, Vector2Int unitSize, BattleUnit movingUnit, string movingUnitName)
    {
        var grid = BattleGrid.Instance;
        if (grid == null || grid.Grid == null)
        {
            Debug.LogError($"[Pathfinding ({movingUnitName})] Grid is null during FindDirectPath!");
            return new List<Vector2Int>();
        }

        var openList = new List<Node>();
        var closedList = new HashSet<Vector2Int>();

        Node startNode = new Node(startPos);
        startNode.GCost = 0;
        startNode.HCost = GetDistance(startPos, targetPos);
        openList.Add(startNode);
        int iterations = 0;
        const int maxSubPathIterations = 2000;

        while (openList.Count > 0 && iterations < maxSubPathIterations)
        {
            iterations++;
            Node currentNode = openList.OrderBy(n => n.FCost).ThenBy(n => n.HCost).First();
            openList.Remove(currentNode);
            closedList.Add(currentNode.Position);
            if (currentNode.Position == targetPos)
            {
                ColoredDebug.CLog(null, $"<color=grey>Pathfinding ({movingUnitName}):</color> Direct sub-path from {startPos} to {targetPos} SUCCEEDED in {iterations} iterations.", _ColoredDebug);
                return RetracePath(startNode, currentNode);
            }

            foreach (var neighbourPos in GetNeighbours(currentNode.Position))
            {
                if (closedList.Contains(neighbourPos)) continue;
                if (!IsAreaWalkableForUnit(neighbourPos, unitSize, grid, movingUnit, startPos, targetPos, movingUnitName)) continue;
                int moveCost = GetDistance(currentNode.Position, neighbourPos);
                int gCostNeigh = currentNode.GCost + moveCost;
                int hCostNeigh = GetDistance(neighbourPos, targetPos);
                Node existingNode = openList.FirstOrDefault(n => n.Position == neighbourPos);
                if (existingNode == null)
                {
                    Node neighbourNode = new Node(neighbourPos)
                    {
                        GCost = gCostNeigh,
                        HCost = hCostNeigh,
                        Parent = currentNode
                    };
                    openList.Add(neighbourNode);
                }
                else if (gCostNeigh < existingNode.GCost)
                {
                    existingNode.GCost = gCostNeigh;
                    existingNode.Parent = currentNode;
                }
            }
        }

        ColoredDebug.CLog(null, $"<color=grey>Pathfinding ({movingUnitName}):</color> Direct sub-path from {startPos} to {targetPos} FAILED after {iterations} iterations.", _ColoredDebug);
        return new List<Vector2Int>();
    }

    /// <summary>
    /// Находит BattleUnit на указанной якорной позиции.
    /// </summary>
    /// <param name="position">Якорная позиция.</param>
    /// <returns>Найденный BattleUnit или null.</returns>
    private static BattleUnit GetUnitAtPosition(Vector2Int position)
    {
        var grid = BattleGrid.Instance;
        if (grid == null)
        {
            Debug.LogError("[Pathfinding] GetUnitAtPosition failed: BattleGrid.Instance is null.");
            return null;
        }
        var cell = grid.GetCell(position);
        if (cell == null)
        {
            // ColoredDebug.CLog(null, $"[Pathfinding] GetUnitAtPosition: Cell at {position} not found in grid.", _ColoredDebug);
            return null;
        }
        return cell.Occupant as BattleUnit;
    }

    /// <summary>
    /// Восстанавливает путь от конечного узла к начальному, следуя по родительским ссылкам.
    /// </summary>
    /// <param name="startNode">Начальный узел.</param>
    /// <param name="endNode">Конечный узел.</param>
    /// <returns>Список позиций пути от старта (исключая) до цели.</returns>
    private static List<Vector2Int> RetracePath(Node startNode, Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node currentNode = endNode;
        int safetyCounter = 0;

        while (currentNode != startNode && safetyCounter < 1000)
        {
            path.Add(currentNode.Position);
            if (currentNode.Parent == null)
            {
                Debug.LogError($"[Pathfinding] RetracePath error: Node {currentNode.Position} has no parent before reaching start node {startNode.Position}!");
                break;
            }
            currentNode = currentNode.Parent;
            safetyCounter++;
        }
        if (safetyCounter >= 1000) Debug.LogError($"[Pathfinding] RetracePath potentially infinite loop detected!");

        path.Reverse();
        return path;
    }

    /// <summary>
    /// Возвращает список координат соседних клеток (вверх, вниз, влево, вправо).
    /// </summary>
    /// <param name="position">Центральная позиция.</param>
    /// <returns>Список соседних позиций.</returns>
    private static List<Vector2Int> GetNeighbours(Vector2Int position)
    {
        return new List<Vector2Int>
        {
            position + Vector2Int.up,
            position + Vector2Int.down,
            position + Vector2Int.left,
            position + Vector2Int.right
        };
    }
    #endregion Личные методы
}