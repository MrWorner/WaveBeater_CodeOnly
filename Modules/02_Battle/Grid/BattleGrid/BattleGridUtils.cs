// НАЗНАЧЕНИЕ: Предоставляет статические методы-утилиты для работы с BattleGrid и BattleUnit, особенно для расчетов расстояний и поиска позиций на сетке.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnit, BattleCell, BattleGrid.
// ПРИМЕЧАНИЕ: Этот класс не имеет состояния (stateless) и содержит только чистые функции для вычислений.
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public static class BattleGridUtils
{
    /// <summary>
    /// Рассчитывает минимальное расстояние (в клетках) между двумя юнитами, учитывая их размеры.
    /// Использует метрику "Шахматного расстояния" (расстояние Чебышева).
    /// </summary>
    /// <returns>Минимальное количество ходов (по прямой или диагонали) между границами юнитов.</returns>
    public static int GetDistance(BattleUnit unitA, BattleUnit unitB)
    {
        if (unitA == null || unitB == null) return int.MaxValue;
        return GetDistance(unitA.CurrentPosition, unitA.Stats.UnitSize, unitB.CurrentPosition, unitB.Stats.UnitSize);
    }

    /// <summary>
    /// Рассчитывает расстояние от юнита до области, заданной позицией и размером (например, пропа).
    /// </summary>
    public static int GetDistance(BattleUnit unit, Vector2Int areaPos, Vector2Int areaSize)
    {
        if (unit == null) return int.MaxValue;
        return GetDistance(unit.CurrentPosition, unit.Stats.UnitSize, areaPos, areaSize);
    }

    /// <summary>
    /// Базовый метод для расчета "Шахматного расстояния" между двумя прямоугольными областями на сетке.
    /// </summary>
    /// <returns>Расстояние в клетках (0, если области пересекаются).</returns>
    public static int GetDistance(Vector2Int posA, Vector2Int sizeA, Vector2Int posB, Vector2Int sizeB)
    {
        int unitA_minX = posA.x;
        int unitA_maxX = posA.x + sizeA.x - 1;
        int unitA_minY = posA.y;
        int unitA_maxY = posA.y + sizeA.y - 1;
        int unitB_minX = posB.x;
        int unitB_maxX = posB.x + sizeB.x - 1;
        int unitB_minY = posB.y;
        int unitB_maxY = posB.y + sizeB.y - 1;

        int x_gap = 0;
        if (unitA_maxX < unitB_minX) x_gap = unitB_minX - unitA_maxX;
        else if (unitB_maxX < unitA_minX) x_gap = unitA_minX - unitB_maxX;

        int y_gap = 0;
        if (unitA_maxY < unitB_minY) y_gap = unitB_minY - unitA_maxY;
        else if (unitB_maxY < unitA_minY) y_gap = unitA_minY - unitB_maxY;

        // Расстояние Чебышева - это максимум из зазоров по осям. Вычитаем 1, чтобы получить количество клеток *между* объектами.
        // Но так как нам нужно расстояние для AI (1 = сосед), мы не вычитаем 1, а просто берем максимум.
        return Mathf.Max(x_gap, y_gap);
    }

    /// <summary>
    /// Проверяет, находятся ли два юнита в соседних клетках (включая диагонали).
    /// </summary>
    /// <returns>True, если расстояние между ними равно 1.</returns>
    public static bool AreUnitsAdjacent(BattleUnit unitA, BattleUnit unitB)
    {
        // Юниты являются соседними, если расстояние между ними равно 1.
        return GetDistance(unitA, unitB) == 1;
    }

    /// <summary>
    /// Находит ближайшую доступную якорную клетку для размещения объекта заданного размера.
    /// Поиск начинается с preferedAnchor и расходится по спирали.
    /// </summary>
    /// <param name="preferedAnchor">Предпочтительная якорная клетка.</param>
    /// <param name="size">Размер размещаемого объекта.</param>
    /// <returns>Найденная BattleCell или null, если свободного места нет.</returns>
    public static BattleCell FindNearestAvailableAnchor(BattleCell preferedAnchor, Vector2Int size)
    {
        BattleGrid grid = BattleGrid.Instance;
        if (grid == null || preferedAnchor == null) return null;

        if (IsAreaAvailable(preferedAnchor.Position, size, grid))
        {
            return preferedAnchor;
        }

        int searchRadius = 1;
        int maxRadius = Mathf.Max(grid.Width, grid.Height);
        while (searchRadius < maxRadius)
        {
            // Используем LINQ для более чистого поиска по спирали
            var cellsInRadius = GetCellsInSpiralRing(preferedAnchor.Position, searchRadius, grid);

            foreach (var cellPos in cellsInRadius)
            {
                if (IsAreaAvailable(cellPos, size, grid))
                {
                    // Возвращаем клетку из сетки по найденной позиции
                    return grid.GetCell(cellPos);
                }
            }
            searchRadius++;
        }
        return null;
    }

    /// <summary>
    /// Вспомогательный метод для проверки, свободна ли и проходима ли область заданного размера.
    /// </summary>
    private static bool IsAreaAvailable(Vector2Int anchorPos, Vector2Int size, BattleGrid grid)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int checkPos = anchorPos + new Vector2Int(x, y);
                if (checkPos.x < 0 || checkPos.x >= grid.Width || checkPos.y < 0 || checkPos.y >= grid.Height)
                {
                    return false; // Выход за границы
                }
                BattleCell cell = grid.Grid[checkPos.x, checkPos.y];
                if (cell == null || !cell.IsEmpty()) // Проверяем IsEmpty, которая включает IsPassable и !IsOccupied
                {
                    return false; // Клетка null, непроходима или занята
                }
            }
        }
        return true; // Вся область доступна
    }

    /// <summary>
    /// Возвращает список координат клеток, образующих кольцо заданного радиуса вокруг центральной точки.
    /// </summary>
    private static List<Vector2Int> GetCellsInSpiralRing(Vector2Int center, int radius, BattleGrid grid)
    {
        List<Vector2Int> ringCells = new List<Vector2Int>();
        for (int i = -radius; i <= radius; i++)
        {
            for (int j = -radius; j <= radius; j++)
            {
                // Добавляем только клетки на границе квадрата радиуса
                if (Mathf.Abs(i) == radius || Mathf.Abs(j) == radius)
                {
                    Vector2Int pos = center + new Vector2Int(i, j);
                    // Проверяем, что позиция внутри сетки
                    if (pos.x >= 0 && pos.x < grid.Width && pos.y >= 0 && pos.y < grid.Height)
                    {
                        ringCells.Add(pos);
                    }
                }
            }
        }
        // Сортируем по удаленности от центра (не обязательно, но может помочь найти "ближайшую" в кольце)
        return ringCells.OrderBy(p => Vector2Int.Distance(center, p)).ToList();
    }

    /// <summary>
    /// Рассчитывает среднюю мировую позицию всех клеток в заданной области.
    /// </summary>
    /// <param name="anchorPos">Якорная позиция области.</param>
    /// <param name="size">Размер области.</param>
    /// <returns>Средняя мировая позиция или позиция якорной клетки, если не удалось найти ни одной клетки.</returns>
    public static Vector3 GetAreaCenterInWorldSpace(Vector2Int anchorPos, Vector2Int size)
    {
        Vector3 center = Vector3.zero;
        int count = 0;
        BattleGrid grid = BattleGrid.Instance;
        if (grid == null) return Vector3.zero; // Safety check

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int cellPos = anchorPos + new Vector2Int(x, y);
                // Проверка на выход за границы
                if (cellPos.x >= 0 && cellPos.x < grid.Width && cellPos.y >= 0 && cellPos.y < grid.Height)
                {
                    BattleCell cell = grid.Grid[cellPos.x, cellPos.y];
                    if (cell != null)
                    {
                        center += cell.WorldPosition;
                        count++;
                    }
                }
            }
        }
        // Если нашли хотя бы одну клетку, возвращаем среднее, иначе позицию якоря (или 0,0,0 если якоря нет)
        return count > 0 ? center / count : (grid.GetCell(anchorPos)?.WorldPosition ?? Vector3.zero);
    }

}