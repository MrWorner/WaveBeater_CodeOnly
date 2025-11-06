using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class StageFlowController : MonoBehaviour
{
    #region Поля
    private List<StageType> _stages;

    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _currentIndex = -1;
    #endregion Поля

    #region Свойства
    public int CurrentIndex => _currentIndex;
    public StageType CurrentStageType
    {
        get
        {
            if (_stages == null || _currentIndex < 0 || _currentIndex >= _stages.Count) return StageType.Battle;
            return _stages[_currentIndex];
        }
    }
    #endregion Свойства

    #region Публичные методы
    public void Init(List<StageType> stages)
    {
        _stages = stages;
        _currentIndex = -1;
    }

    public bool MoveNext()
    {
        _currentIndex++;
        return _stages != null && _currentIndex < _stages.Count;
    }

    public string GetStageInfo()
    {
        if (_stages == null || _stages.Count == 0) return "Empty";
        if (_currentIndex < 0) return "NotStarted";
        if (_currentIndex >= _stages.Count) return "Completed";
        return $"{_stages[_currentIndex]} ({_currentIndex + 1}/{_stages.Count})";
    }
    #endregion Публичные методы
}