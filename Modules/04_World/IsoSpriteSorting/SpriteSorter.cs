using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways] // Атрибут, который заставляет скрипт работать всегда: и в редакторе, и в режиме Play
public class SpriteSorter : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private SpriteRenderer _spriteRenderer;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private SpriteRenderer _spriteRendererColor;
    [BoxGroup("SETTINGS"), SerializeField] private Canvas _healthBarCanvas;
    [BoxGroup("SETTINGS"), SerializeField] private Canvas _electroShieldCanvas;
    [BoxGroup("SETTINGS"), SerializeField] private Canvas _ironClawCanvas;
    [BoxGroup("SETTINGS"), SerializeField] private Canvas _aimCanvas;
    [BoxGroup("DEBUG"), SerializeField] private bool _coloredDebug;
    #endregion Поля

    #region Свойства
    public SpriteRenderer SpriteRenderer => _spriteRenderer;
    public SpriteRenderer SpriteRendererColor => _spriteRendererColor;
    public Canvas HealthBarCanvas => _healthBarCanvas;
    public Canvas ElectroShieldCanvas => _electroShieldCanvas;
    public Canvas IronClawCanvas => _ironClawCanvas;
    public Canvas AimCanvas => _aimCanvas;
    #endregion Свойства

#if UNITY_EDITOR
    private const float UPDATE_INTERVAL = 0.33f; // Интервал обновления в секундах
    private double _lastUpdateTime = 0; // Время последнего обновления
#endif

    #region Методы UNITY
    private void Awake()
    {
        if (_spriteRenderer == null) DebugUtils.LogMissingReference(this, nameof(_spriteRenderer));
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        // Подписываемся на событие обновления редактора, когда объект активен
        EditorApplication.update += EditorUpdate;
    }

    private void OnDisable()
    {
        // Отписываемся, чтобы избежать утечек памяти и ошибок
        EditorApplication.update -= EditorUpdate;
    }

    private void EditorUpdate()
    {
        // Выходим, если игра запущена (в режиме Play будет работать LateUpdate)
        if (Application.isPlaying) return;

        // Проверяем, прошло ли достаточно времени с последнего обновления
        if (EditorApplication.timeSinceStartup > _lastUpdateTime + UPDATE_INTERVAL)
        {
            _lastUpdateTime = EditorApplication.timeSinceStartup;
            SetSpriteSortingOrder();
        }
    }
#endif

    private void LateUpdate()
    {
        // Этот метод будет работать как и раньше, но только в режиме Play
        if (!Application.isPlaying) return;

        SetSpriteSortingOrder();
    }
    #endregion Методы UNITY

    #region Личные методы
    [Button]
    private void SetSpriteSortingOrder()
    {
        if (_spriteRenderer == null || _spriteRenderer.transform.parent == null) return;

        int parentBaseOrder = Mathf.RoundToInt(-_spriteRenderer.transform.parent.position.y * 10);
        int childOffset = Mathf.RoundToInt(-_spriteRenderer.transform.localPosition.y * 10);
        int finalOrder = parentBaseOrder + childOffset + 999999;

        _spriteRenderer.sortingOrder = finalOrder;

        if (_spriteRendererColor != null) _spriteRendererColor.sortingOrder = finalOrder + 1;
        if (_healthBarCanvas != null) _healthBarCanvas.sortingOrder = finalOrder + 2;
        if (_electroShieldCanvas != null) _electroShieldCanvas.sortingOrder = finalOrder + 2;
        if (_ironClawCanvas != null) _ironClawCanvas.sortingOrder = finalOrder + 2;
        if (_aimCanvas != null) _aimCanvas.sortingOrder = finalOrder + 2;

        ColoredDebug.CLog(gameObject, "<color=cyan>SpriteSorter:</color> Обновляю сортировочный порядок. Итоговый порядок: <color=lime>{0}</color>.", _coloredDebug, finalOrder);
    }
    #endregion Личные методы
}