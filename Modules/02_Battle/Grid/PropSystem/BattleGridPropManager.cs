using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleGridPropManager : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleGrid _battleGrid;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), Tooltip("Профиль, используемый по умолчанию, если не указан другой."), SerializeField] private PropPlacementProfileSO _defaultPlacementProfile;
    [BoxGroup("SETTINGS"), Tooltip("Родительский объект для хранения неактивных пропов в пуле."), SerializeField] private Transform _poolParent;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    [BoxGroup("DEBUG"), ShowInInspector, ReadOnly] private Dictionary<PropSO, List<GameObject>> _propPool = new Dictionary<PropSO, List<GameObject>>();
    [BoxGroup("DEBUG"), ShowInInspector, ReadOnly] private List<GameObject> _activeProps = new List<GameObject>();
    [BoxGroup("DEBUG"), ShowInInspector, ReadOnly] private List<GameObject> _propsOnCooldown = new List<GameObject>();
    #endregion Поля

    #region Свойства
    private static BattleGridPropManager _instance;
    /// <summary>
    /// Предоставляет глобальный доступ к экземпляру BattleGridPropManager.
    /// </summary>
    public static BattleGridPropManager Instance
    {
        get
        {
            if (_instance == null) _instance = FindFirstObjectByType<BattleGridPropManager>();
            return _instance;
        }
    }

    /// <summary>
    /// Возвращает список всех активных пропов на сцене.
    /// </summary>
    public IReadOnlyList<GameObject> ActiveProps => _activeProps;
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null) { DebugUtils.LogInstanceAlreadyExists(this, _instance); } else { _instance = this; }
        if (_battleGrid == null) DebugUtils.LogMissingReference(this, nameof(_battleGrid));
        if (_poolParent == null) _poolParent = transform;
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Уничтожает указанный проп (вызывается из PropHealth.Die), освобождая занимаемые им клетки и возвращая его в пул.
    /// </summary>
    /// <param name="propInstance">Игровой объект пропа для уничтожения.</param>
    public void DestroyProp(GameObject propInstance)
    {
        if (propInstance == null) return;

        // Check if it's currently active before proceeding
        bool wasActive = _activeProps.Contains(propInstance);

        ColoredDebug.CLog(gameObject, "<color=orange>BattleGridPropManager:</color> Уничтожение (через Die) пропа <color=yellow>{0}</color>.", _ColoredDebug, propInstance.name);

        // Освобождаем ячейки
        var cellsToClear = new List<BattleCell>();
        foreach (var cell in _battleGrid.AllCells)
        {
            if (cell != null && cell.Occupant == (object)propInstance)
            {
                cellsToClear.Add(cell);
            }
        }

        foreach (var cell in cellsToClear)
        {
            cell.SetOccupant(null);
        }

        // Эффект взрыва и звук (могут быть вызваны даже если объект уже не в _activeProps)
        ObjectPoolExplosion.Instance.GetObject().transform.position = propInstance.transform.position;
        SoundManager.Instance.PlayOneShot(SoundType.Explosion);

        // Убираем из активных (если был там) и возвращаем в пул
        if (wasActive)
        {
            _activeProps.Remove(propInstance);
        }
        ReturnPropToPool(propInstance);
    }

    /// <summary>
    /// Размещает указанный проп в заданной якорной позиции.
    /// </summary>
    /// <param name="propSO">ScriptableObject пропа для размещения.</param>
    /// <param name="anchorPosition">Координаты якорной клетки на сетке.</param>
    public void PlacePropAt(PropSO propSO, Vector2Int anchorPosition)
    {
        if (propSO == null)
        {
            Debug.LogError("[BattleGridPropManager] Попытка разместить пустой (null) PropSO.");
            return;
        }

        GameObject propInstance = GetPropFromPool(propSO);

        if (propInstance.TryGetComponent<PropHealth>(out var propHealth))
        {
            propHealth.Initialize();
        }

        if (propInstance.TryGetComponent<Prop>(out var propComponent))
        {
            propComponent.SetAnchorPosition(anchorPosition);
        }

        Vector3 finalWorldCenterPosition = GetVisualAreaCenterInWorldSpace(anchorPosition, propSO.PropSize);
        propInstance.transform.position = finalWorldCenterPosition;
        BridgeCellElement anchorCellElement = BridgeManager.Instance.ActiveBridge.GetPhysicalCell(anchorPosition);
        if (anchorCellElement != null)
        {
            propInstance.transform.SetParent(anchorCellElement.transform, true);
        }
        else
        {
            Debug.LogError($"[BattleGridPropManager] Не удалось найти физическую ячейку моста в позиции {anchorPosition} для размещения пропа {propSO.name}!");
            propInstance.transform.SetParent(BridgeManager.Instance.ActiveBridge.transform, true);
        }

        propInstance.SetActive(true);
        _activeProps.Add(propInstance);
        if (!propSO.IsPassable)
        {
            OccupyCells(anchorPosition, propSO.PropSize, propInstance);
        }

        ColoredDebug.CLog(gameObject, "<color=cyan>BattleGridPropManager:</color> Размещен проп <color=yellow>{0}</color> на якорной клетке <color=yellow>{1}</color>. Родитель: <color=lime>{2}</color>", _ColoredDebug, propSO.name, anchorPosition, anchorCellElement != null ? anchorCellElement.name : "Bridge_Root");
    }

    /// <summary>
    /// Логически "увольняет" активные пропы с поля, помечая их как недоступные для следующей генерации.
    /// Не деактивирует и не перемещает сами объекты, они остаются на своей арене.
    /// </summary>
    public void RetireActiveProps()
    {
        if (!_activeProps.Any()) return;
        // Сначала очищаем старый список "отдыхающих". Теперь они снова доступны.
        _propsOnCooldown.Clear();
        // Перемещаем все текущие активные пропы в список "на отдыхе".
        _propsOnCooldown.AddRange(_activeProps);
        _activeProps.Clear(); // Очищаем список активных, т.к. начинается новая битва.
        ColoredDebug.CLog(gameObject, "<color=orange>BattleGridPropManager:</color> <color=yellow>{0}</color> пропов с прошлой битвы отправлены 'на отдых'.", _ColoredDebug, _propsOnCooldown.Count);
    }

    /// <summary>
    /// Генерирует и размещает случайные пропы на поле.
    /// </summary>
    /// <param name="profile">Профиль размещения. Если null, используется профиль по умолчанию.</param>
    public void GenerateAndPlaceProps(PropPlacementProfileSO profile = null)
    {
        PropPlacementProfileSO activeProfile = profile ?? _defaultPlacementProfile;
        if (activeProfile == null)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>BattleGridPropManager:</color> Профиль размещения не назначен, генерация пропов отменена.", _ColoredDebug);
            return;
        }

        int propsToSpawn = Random.Range(activeProfile.MinPropsToSpawn, activeProfile.MaxPropsToSpawn + 1);
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleGridPropManager:</color> Генерация <color=yellow>{0}</color> пропов по профилю <color=yellow>{1}</color>.", _ColoredDebug, propsToSpawn, activeProfile.name);

        if (propsToSpawn == 0) return;
        for (int i = 0; i < propsToSpawn; i++)
        {
            PropSO propToSpawn = ChooseProp(activeProfile);
            if (propToSpawn == null)
            {
                ColoredDebug.CLog(gameObject, "<color=orange>BattleGridPropManager:</color> Не удалось выбрать проп для спавна на итерации {0}.", _ColoredDebug, i + 1);
                continue;
            }

            var availableCells = new List<BattleCell>(_battleGrid.AllCells.Where(c => c != null && c.IsEmpty()));
            var potentialAnchorCells = availableCells
        .Where(cell => CanPlaceProp(propToSpawn, cell.Position, activeProfile.MinDistanceBetweenProps))
        .ToList();
            if (potentialAnchorCells.Count > 0)
            {
                BattleCell anchorCell = potentialAnchorCells[Random.Range(0, potentialAnchorCells.Count)];
                PlacePropAt(propToSpawn, anchorCell.Position);
            }
            else
            {
                ColoredDebug.CLog(gameObject, "<color=orange>BattleGridPropManager:</color> Не найдено подходящего места для пропа <color=white>{0}</color> (размер {1}).", _ColoredDebug, propToSpawn.name, propToSpawn.PropSize);
            }
        }
    }

    /// <summary>
    /// Возвращает все активные пропы в пул.
    /// </summary>
    public void ReturnAllPropsToPool()
    {
        if (_activeProps.Count == 0) return;
        ColoredDebug.CLog(gameObject, "<color=orange>BattleGridPropManager:</color> Возвращаю <color=yellow>{0}</color> активных пропов в пул.", _ColoredDebug, _activeProps.Count);
        foreach (var cell in _battleGrid.AllCells)
        {
            if (cell != null && _activeProps.Contains(cell.Occupant as GameObject)) // Check if occupant is one of the active props
            {
                cell.SetOccupant(null);
            }
        }

        foreach (GameObject propInstance in _activeProps.ToList())
        {
            ReturnPropToPool(propInstance);
        }
        _activeProps.Clear();
    }

    /// <summary>
    /// Находит все активные пропы, являющиеся дочерними к указанному родителю, и возвращает их в пул.
    /// </summary>
    /// <param name="parent">Родительский Transform, дочерние пропы которого нужно очистить.</param>
    public void ClearPropsFromParent(Transform parent)
    {
        if (parent == null) return;
        // Ищем пропы на старой сцене. Теперь они должны быть в списке _propsOnCooldown.
        var propsToClear = _propsOnCooldown.Where(p => p != null && p.transform.IsChildOf(parent)).ToList();
        if (propsToClear.Count > 0)
        {
            ColoredDebug.CLog(gameObject, "<color=cyan>BattleGridPropManager:</color> Очистка <color=yellow>{0}</color> 'отдохнувших' пропов со старой сцены '{1}'.", _ColoredDebug, propsToClear.Count, parent.name);
            foreach (var prop in propsToClear)
            {
                // Возвращаем в пул (деактивируем, меняем родителя)
                ReturnPropToPool(prop);
                // Убираем из списка "на отдыхе", делая доступным для будущих битв
                _propsOnCooldown.Remove(prop);
            }
        }
    }

    #endregion Публичные методы

    #region Личные методы
    private PropSO ChooseProp(PropPlacementProfileSO profile)
    {
        float totalChance = profile.Props.Sum(p => p.SpawnChance);
        float randomValue = Random.Range(0, totalChance);
        foreach (var propChance in profile.Props)
        {
            if (randomValue < propChance.SpawnChance) return propChance.Prop;
            randomValue -= propChance.SpawnChance;
        }
        return null;
    }

    private bool CanPlaceProp(PropSO prop, Vector2Int anchorPos, int minDistance)
    {
        for (int x = 0; x < prop.PropSize.x; x++)
        {
            for (int y = 0; y < prop.PropSize.y; y++)
            {
                Vector2Int cellPos = anchorPos + new Vector2Int(x, y);
                if (cellPos.x < 0 || cellPos.x >= _battleGrid.Width || cellPos.y < 0 || cellPos.y >= _battleGrid.Height)
                {
                    ColoredDebug.CLog(gameObject, "<color=orange>BattleGridPropManager:</color> Нельзя разместить проп. Ячейка <color=yellow>{0}</color> выходит за пределы сетки ({1}x{2}).", _ColoredDebug, cellPos, _battleGrid.Width, _battleGrid.Height);
                    return false;
                }
                BattleCell cell = _battleGrid.Grid[cellPos.x, cellPos.y];
                if (cell == null || !cell.IsEmpty())
                {
                    ColoredDebug.CLog(gameObject, "<color=orange>BattleGridPropManager:</color> Нельзя разместить проп. Ячейка <color=yellow>{0}</color> уже занята ({1}), непроходима ({2}) или отсутствует ({3}).", _ColoredDebug, cellPos, cell?.IsOccupied(), cell?.IsPassable == false, cell == null);
                    return false;
                }
            }
        }

        Vector3 propWorldCenter = GetAreaCenterInWorldSpace(anchorPos, prop.PropSize);
        foreach (var activePropInstance in _activeProps)
        {
            if (activePropInstance == null) continue;
            if (Vector3.Distance(activePropInstance.transform.position, propWorldCenter) < minDistance)
            {
                ColoredDebug.CLog(gameObject, "<color=orange>BattleGridPropManager:</color> Нельзя разместить проп. Слишком близко к другому активному пропу (<color=yellow>{0}</color>).", _ColoredDebug, activePropInstance.name);
                return false;
            }
        }
        return true;
    }


    private GameObject GetPropFromPool(PropSO propSO)
    {
        if (!_propPool.ContainsKey(propSO)) _propPool[propSO] = new List<GameObject>();
        var pooledObj = _propPool[propSO].FirstOrDefault(p => p != null && !p.activeInHierarchy && !_propsOnCooldown.Contains(p));
        if (pooledObj != null)
        {
            if (pooledObj.TryGetComponent<PropHealth>(out var health))
            {
                health.ResetHealth();
            }
            return pooledObj;
        }


        GameObject newProp = Instantiate(propSO.PropPrefab, _poolParent);
        if (propSO.IsDestructible && newProp.GetComponent<PropHealth>() == null)
        {
            newProp.AddComponent<PropHealth>();
        }

        _propPool[propSO].Add(newProp);
        newProp.SetActive(false);

        if (newProp.TryGetComponent<PropHealth>(out var newHealth))
        {
            newHealth.ResetHealth();
        }

        return newProp;
    }

    private void ReturnPropToPool(GameObject propInstance)
    {
        if (propInstance == null) return;

        if (propInstance.TryGetComponent<PropHealth>(out var health))
        {
            health.ResetHealth();
        }

        propInstance.transform.SetParent(_poolParent, false);
        propInstance.SetActive(false);
    }


    private Vector3 GetVisualAreaCenterInWorldSpace(Vector2Int anchorPos, Vector2Int size)
    {
        Vector3 center = Vector3.zero;
        int count = 0;
        BridgeGrid activeBridge = BridgeManager.Instance.ActiveBridge;
        if (activeBridge == null)
        {
            Debug.LogError("[BattleGridPropManager] Не удалось найти ActiveBridge для расчета позиции пропа!");
            return GetAreaCenterInWorldSpace(anchorPos, size);
        }

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                BridgeCellElement cellElement = activeBridge.GetPhysicalCell(anchorPos + new Vector2Int(x, y));
                if (cellElement != null)
                {
                    center += cellElement.transform.position;
                    count++;
                }
            }
        }
        return count > 0 ? center / count : Vector3.zero;
    }


    private Vector3 GetAreaCenterInWorldSpace(Vector2Int anchorPos, Vector2Int size)
    {
        Vector3 center = Vector3.zero;
        int count = 0;
        if (_battleGrid.Grid == null || anchorPos.x < 0 || anchorPos.y < 0)
        {
            Debug.LogError($"[BattleGridPropManager] Invalid grid or anchor position ({anchorPos}) for GetAreaCenterInWorldSpace.");
            return Vector3.zero;
        }

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                int checkX = anchorPos.x + x;
                int checkY = anchorPos.y + y;

                if (checkX >= 0 && checkX < _battleGrid.Width && checkY >= 0 && checkY < _battleGrid.Height)
                {
                    BattleCell cell = _battleGrid.Grid[checkX, checkY];
                    if (cell != null)
                    {
                        center += cell.WorldPosition;
                        count++;
                    }
                }
                else
                {
                    Debug.LogWarning($"[BattleGridPropManager] Cell position ({checkX},{checkY}) out of bounds during GetAreaCenterInWorldSpace.");
                }
            }
        }
        return count > 0 ? center / count : Vector3.zero;
    }

    private void OccupyCells(Vector2Int anchorPos, Vector2Int size, GameObject occupant)
    {
        if (_battleGrid.Grid == null) return;

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                int checkX = anchorPos.x + x;
                int checkY = anchorPos.y + y;

                if (checkX >= 0 && checkX < _battleGrid.Width && checkY >= 0 && checkY < _battleGrid.Height)
                {
                    BattleCell cell = _battleGrid.Grid[checkX, checkY];
                    if (cell != null) cell.SetOccupant(occupant);
                }
            }
        }
    }

    #endregion Личные методы
}