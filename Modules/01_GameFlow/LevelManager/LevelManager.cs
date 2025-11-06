using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Events;

public enum BackgroundVariant
{
    Day,
    Evening,
    Night
}

public enum BackgroundType
{
    NotSet,
    City
}

/// <summary>
/// Управляет логикой уровней, используя LevelProgression для определения последовательности.
/// </summary>
public class LevelManager : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField, Tooltip("Текущий пакет уровней, который проходит игрок")]
    private LevelProgression _currentProgression;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly]
    private int _currentLevelIndex = 0;
    [BoxGroup("DEBUG"), SerializeField]
    protected bool _ColoredDebug;
    private static LevelManager _instance;
    #endregion Поля

    #region Свойства
    public LevelProgression CurrentProgression { get => _currentProgression; }
    public int CurrentLevelIndex { get => _currentLevelIndex; }
    public static LevelManager Instance => _instance;
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null)
        {
            Debug.LogWarning($"Экземпляр {GetType().Name} уже существует. Новый экземпляр на объекте {gameObject.name} будет уничтожен.");
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }

        if (GameInstance.Instance != null && GameInstance.Instance.SelectedLevelProgression != null)
        {
            _currentProgression = GameInstance.Instance.SelectedLevelProgression;
        }

        if (_currentProgression == null) Debug.LogError($"В LevelManager на объекте {gameObject.name} не назначена ссылка на LevelProgression.", this);
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Возвращает данные текущего уровня.
    /// </summary>
    public LevelData GetCurrentLevelData()
    {
        if (_currentProgression == null || _currentProgression.Levels.Count == 0)
        {
            Debug.LogError("LevelProgression не назначен или пуст в LevelManager!", this);
            return null;
        }

        if (_currentLevelIndex >= 0 && _currentLevelIndex < _currentProgression.Levels.Count)
        {
            return _currentProgression.Levels[_currentLevelIndex];
        }

        Debug.LogError($"Индекс текущего уровня ({_currentLevelIndex}) выходит за пределы списка уровней.", this);
        return null;
    }

    /// <summary>
    /// Переходит к следующему уровню в текущем пакете.
    /// </summary>
    /// <returns>Возвращает true, если следующий уровень существует, иначе false.</returns>
    public bool MoveToNextLevel()
    {
        if (_currentProgression != null && _currentLevelIndex < _currentProgression.Levels.Count - 1)
        {
            _currentLevelIndex++;
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Начинает прохождение текущего пакета уровней заново.
    /// </summary>
    public void ResetProgression()
    {
        _currentLevelIndex = 0;
    }

    /// <summary>
    /// Проверяет, является ли текущий уровень последним в списке прогрессии.
    /// </summary>
    /// <returns>True, если это последний уровень.</returns>
    public bool IsLastLevel()
    {
        if (_currentProgression == null || _currentProgression.Levels.Count == 0)
        {
            return true; // Если прогрессии нет, считаем что любой уровень - последний
        }
        return _currentLevelIndex >= _currentProgression.Levels.Count - 1;
    }
    #endregion Публичные методы
}

