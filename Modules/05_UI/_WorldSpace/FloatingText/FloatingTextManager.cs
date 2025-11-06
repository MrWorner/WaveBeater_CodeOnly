// НАЗНАЧЕНИЕ: Менеджер плавающего текста (Singleton)
// ЗАВИСИМОСТИ: ObjectPoolFloatingText, FloatingText
// ПРИМЕЧАНИЕ: Использует Object Pool для переиспользования элементов
using Sirenix.OdinInspector;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using System;

public class FloatingTextManager : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private ObjectPoolFloatingText _objectPoolFloatingText;
    #endregion

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly]
    private static FloatingTextManager _instance;
    [BoxGroup("DEBUG"), SerializeField]
    protected bool _ColoredDebug = true;
    #endregion

    #region Свойства
    public static FloatingTextManager Instance
    {
        get => _instance;
    }
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null)
        {
            ColoredDebug.CLog(gameObject, "<color=red>SYSTEM:</color> Instance <color=yellow>{0}</color> already exists. Destroying new one.", _ColoredDebug, nameof(FloatingTextManager));
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            ColoredDebug.CLog(gameObject, "<color=orange>SYSTEM:</color> <color=cyan>{0}</color> initialized.", _ColoredDebug, nameof(FloatingTextManager));
        }

        // Проверка Required полей
        if (_objectPoolFloatingText == null)
        {
            DebugUtils.LogMissingReference(this, nameof(_objectPoolFloatingText));
        }
    }
    #endregion

    #region Публичные методы
    /// <summary>Отображает плавающий текст.</summary>
    /// <param name="text">Текст для отображения.</param>
    /// <param name="type">Тип текста (например, урон, лечение).</param>
    /// <param name="position">Мировая позиция, где должен появиться текст.</param>
    public void SpawnFloatingText(string text, FloatingText.TextType type, Vector3 position)
    {
        FloatingText floatingText = _objectPoolFloatingText.RetrieveObject()?.GetComponent<FloatingText>();

        if (floatingText != null)
        {
            floatingText.transform.position = position;
            floatingText.SetText(text, type);

            ColoredDebug.CLog(gameObject, "<color=lime>[ACTION]</color> Отображаю Floating Text: <color=yellow>{0}</color> типа <color=yellow>{1}</color>.", _ColoredDebug, text, type.ToString());
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=red>[FAILSAFE]</color> Не удалось получить FloatingText из Object Pool.", _ColoredDebug);
        }
    }
    #endregion
}