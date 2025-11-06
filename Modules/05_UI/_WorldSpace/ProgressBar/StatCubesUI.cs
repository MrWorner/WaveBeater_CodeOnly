// НАЗНАЧЕНИЕ: Управляет UI-отображением любого числового параметра (здоровье, броня, щит) в виде набора иконок-"кубиков".
// ОСНОВНЫЕ ЗАВИСИМОСТИ: TextMeshProUGUI, Image. Настраивается через инспектор.
// ПРИМЕЧАНИЕ: Компонент динамически создает и уничтожает дочерние объекты в указанном контейнере.
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine.Events;

public class StatCubesUI : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _cubePrefab;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Transform _container;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _valueText;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Sprite _fullSprite;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Sprite _emptySprite;
    #endregion

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _maxValue;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _currentValue;
    [BoxGroup("DEBUG"), SerializeField] private bool _ColoredDebug;

    private readonly List<Image> _cubes = new List<Image>();
    #endregion

    #region Свойства
    /// <summary>
    /// Возвращает максимальное значение параметра.
    /// </summary>
    public int MaxValue { get => _maxValue; }

    /// <summary>
    /// Возвращает текущее значение параметра.
    /// </summary>
    public int CurrentValue { get => _currentValue; }
    #endregion

    #region Публичные методы
    /// <summary>
    /// Инициализирует UI с максимальным значением. Создает начальное количество "кубиков".
    /// </summary>
    /// <param name="maxValue">Максимальное значение для инициализации.</param>
    public void Initialize(int maxValue)
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>StatCubesUI:</color> Инициализация. Максимальное значение: <color=yellow>{0}</color>.", _ColoredDebug, maxValue);
        _maxValue = maxValue;
        _currentValue = maxValue;

        ClearCubes();
        CreateCubes(_maxValue);
        UpdateUI();
    }

    /// <summary>
    /// Устанавливает и отображает текущее значение параметра.
    /// </summary>
    /// <param name="value">Новое текущее значение.</param>
    public void SetCurrentValue(int value)
    {
        int clampedValue = Mathf.Clamp(value, 0, _maxValue);
        ColoredDebug.CLog(gameObject, "<color=cyan>StatCubesUI:</color> Установка текущего значения. Было: <color=yellow>{0}</color>, Стало: <color=lime>{1}</color>.", _ColoredDebug, _currentValue, clampedValue);
        _currentValue = clampedValue;
        UpdateUI();
    }

    /// <summary>
    /// Устанавливает новое максимальное значение и пересоздает UI элементы.
    /// </summary>
    /// <param name="newMaxValue">Новое максимальное значение.</param>
    /// <param name="resetToFull">Если true, текущее значение будет восстановлено до нового максимума.</param>
    public void SetMaxValue(int newMaxValue, bool resetToFull = false)
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>StatCubesUI:</color> Установка максимального значения <color=yellow>{0}</color>. Сбросить до полного: <color=orange>{1}</color>.", _ColoredDebug, newMaxValue, resetToFull);
        _maxValue = newMaxValue;
        if (resetToFull)
        {
            _currentValue = _maxValue;
        }

        _currentValue = Mathf.Min(_currentValue, _maxValue);

        ClearCubes();
        CreateCubes(_maxValue);
        UpdateUI();
    }
    #endregion

    #region Личные методы
    /// <summary>
    /// Создает указанное количество "кубиков" и добавляет их в контейнер.
    /// </summary>
    private void CreateCubes(int count)
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>StatCubesUI:</color> Создание <color=yellow>{0}</color> кубиков.", _ColoredDebug, count);
        for (int i = 0; i < count; i++)
        {
            GameObject cubeInstance = Instantiate(_cubePrefab, _container);
            if (cubeInstance.TryGetComponent<Image>(out var image))
            {
                _cubes.Add(image);
            }
        }
    }

    /// <summary>
    /// Удаляет все созданные "кубики" из контейнера и очищает список.
    /// </summary>
    private void ClearCubes()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>StatCubesUI:</color> Очистка <color=yellow>{0}</color> кубиков.", _ColoredDebug, _cubes.Count);
        foreach (Image cubeImage in _cubes)
        {
            if (cubeImage != null)
            {
                Destroy(cubeImage.gameObject);
            }
        }
        _cubes.Clear();
    }

    /// <summary>
    /// Обновляет спрайты "кубиков" и текстовое поле в соответствии с текущими значениями.
    /// </summary>
    private void UpdateUI()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>StatCubesUI:</color> Обновление UI. Текущее значение: <color=lime>{0}</color>/<color=yellow>{1}</color>.", _ColoredDebug, _currentValue, _maxValue);
        for (int i = 0; i < _cubes.Count; i++)
        {
            if (_cubes[i] == null) continue;
            _cubes[i].sprite = (i < _currentValue) ? _fullSprite : _emptySprite;
        }

        if (_valueText != null)
        {
            _valueText.text = _currentValue.ToString();
        }
    }
    #endregion
}