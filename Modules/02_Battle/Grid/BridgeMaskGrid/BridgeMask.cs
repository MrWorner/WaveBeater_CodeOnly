// НАЗНАЧЕНИЕ: Является контейнером для всех элементов (SpriteRenderer) спрайтовой маски моста. Предоставляет централизованный доступ к элементам и методы для их установки и очистки.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: BridgeMaskGenerator (который управляет этим компонентом).
// ПРИМЕЧАНИЕ: Не имеет собственной логики, служит только для хранения данных.
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine.Events;

public class BridgeMask : MonoBehaviour
{
    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly]
    private List<SpriteRenderer> _maskElements = new List<SpriteRenderer>();
    [BoxGroup("DEBUG"), SerializeField] private bool _ColoredDebug;
    #endregion

    #region Свойства
    /// <summary>
    /// Предоставляет доступ только для чтения к списку всех элементов маски.
    /// </summary>
    //public List<SpriteRenderer> AllMaskElements { get => _maskElements; }
    #endregion


    #region Публичные методы
    /**
    /// <summary>
    /// Устанавливает и сохраняет список сгенерированных элементов маски.
    /// </summary>
    /// <param name="elements">Список компонентов SpriteRenderer, составляющих маску.</param>
    public void SetElements(List<SpriteRenderer> elements)
    {
        _maskElements = elements;
        ColoredDebug.CLog(gameObject, "<color=cyan>BridgeMask:</color> Маска установлена. Всего элементов: <color=yellow>{0}</color>.", _ColoredDebug, elements.Count);
    }
    **/

    /// <summary>
    /// Полностью очищает маску, удаляя все дочерние игровые объекты и обнуляя список элементов.
    /// </summary>
    public void ClearMask()
    {
        int childCount = transform.childCount;
#if UNITY_EDITOR
        // В редакторе используем DestroyImmediate для мгновенного удаления, чтобы избежать ошибок.
        for (int i = childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
#else
        // В билде используем обычный Destroy.
        for (int i = childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
#endif
        _maskElements.Clear();
        ColoredDebug.CLog(gameObject, "<color=cyan>BridgeMask:</color> Маска очищена. Удалено дочерних объектов: <color=yellow>{0}</color>.", _ColoredDebug, childCount);
    }
    #endregion
}