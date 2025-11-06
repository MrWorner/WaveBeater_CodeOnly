// НАЗНАЧЕНИЕ: Отвечает за размещение врагов на боевой сетке, находя для них подходящее место и запуская их полную инициализацию.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleGrid, BattleUnit.
// ПРИМЕЧАНИЕ: Логика поиска места адаптирована для юнитов, занимающих несколько клеток.
using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Events;

public class EnemyPlacementService : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleGrid _battleGrid;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Методы UNITY
    private void Awake()
    {
        if (_battleGrid == null) DebugUtils.LogMissingReference(this, nameof(_battleGrid));
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Размещает врага на поле, находя для него подходящее место и полностью инициализируя его.
    /// </summary>
    /// <param name="enemy">Экземпляр врага для размещения.</param>
    public void PlaceEnemy(BattleUnit enemy)
    {
        if (_battleGrid == null)
        {
            ColoredDebug.CLog(gameObject, "<color=red>EnemyPlacementService:</color> Ссылка на <color=yellow>BattleGrid</color> не назначена!", true);
            return;
        }

        BattleCell targetAnchorCell = FindFarthestAvailableAnchorCell(enemy.Stats.UnitSize);

        if (targetAnchorCell != null)
        {
            enemy.Initialize(targetAnchorCell);
            ColoredDebug.CLog(gameObject, "<color=lime>EnemyPlacementService:</color> Враг <color=orange>{0}</color> (размер {1}) размещен и инициализирован на якорной позиции <color=yellow>{2}</color>.", _ColoredDebug, enemy.name, enemy.Stats.UnitSize, targetAnchorCell.Position);
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=red>EnemyPlacementService:</color> Не найдено свободных клеток для размещения врага <color=red>{0}</color> (размер {1}). Уничтожаю.", true, enemy.name, enemy.Stats.UnitSize);
            Destroy(enemy.gameObject);
        }
    }
    #endregion Публичные методы

    #region Личные методы
    private BattleCell FindFarthestAvailableAnchorCell(Vector2Int size)
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>EnemyPlacementService:</color> Поиск самой дальней доступной области размером <color=yellow>{0}</color>.", _ColoredDebug, size);
        // Идем справа налево, сверху вниз, чтобы найти самую "дальнюю" точку
        for (int x = _battleGrid.Width - size.x; x >= 0; x--)
        {
            for (int y = 0; y < _battleGrid.Height - (size.y - 1); y++)
            {
                if (IsAreaEmpty(new Vector2Int(x, y), size))
                {
                    ColoredDebug.CLog(gameObject, "<color=lime>EnemyPlacementService:</color> Найдена подходящая якорная клетка: <color=yellow>{0}</color>.", _ColoredDebug, new Vector2Int(x, y));
                    return _battleGrid.Grid[x, y];
                }
            }
        }

        ColoredDebug.CLog(gameObject, "<color=red>EnemyPlacementService:</color> Не найдено свободных областей на поле для юнита размером {0}.", _ColoredDebug, size);
        return null;
    }

    private bool IsAreaEmpty(Vector2Int anchorPos, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int checkPos = anchorPos + new Vector2Int(x, y);

                if (checkPos.x >= _battleGrid.Width || checkPos.y >= _battleGrid.Height || checkPos.x < 0 || checkPos.y < 0)
                {
                    return false;
                }

                BattleCell cell = _battleGrid.Grid[checkPos.x, checkPos.y];

                if (cell == null || !cell.IsEmpty())
                {
                    return false;
                }
            }
        }
        return true;
    }
    #endregion Личные методы
}