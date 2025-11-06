using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// Отслеживает производительность игрока и волн в бою.
/// Сохраняет историю всех пройденных волн.
/// </summary>
public class WavePerformanceManager : MonoBehaviour
{
    public static WavePerformanceManager Instance { get; private set; }

    [BoxGroup("DEBUG"), ShowInInspector, ReadOnly]
    public List<WavePerformanceRecord> WaveHistory { get; private set; } = new List<WavePerformanceRecord>();

    private int _playerMaxHealthAtWaveStart;
    private int _totalDamageTakenInCurrentWave;
    private WaveData _currentWaveData;
    private bool _isTrackingWave = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            ///DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Начинает отслеживание одной волны. Вызывается в WaveManager.
    /// </summary>
    public void StartWaveTracking(WaveData waveData, int playerMaxHealth)
    {
        _isTrackingWave = true;
        _currentWaveData = waveData;
        _playerMaxHealthAtWaveStart = playerMaxHealth;
        _totalDamageTakenInCurrentWave = 0;
    }

    /// <summary>
    /// Записывает урон, полученный игроком. Вызывается из Hero_DEPRECATED.
    /// </summary>
    public void RecordPlayerDamage(int damageAmount)
    {
        if (_isTrackingWave)
        {
            _totalDamageTakenInCurrentWave += damageAmount;
        }
    }

    /// <summary>
    /// Завершает отслеживание волны, рассчитывает результат и сохраняет в историю.
    /// </summary>
    public void EndWaveTracking()
    {
        if (!_isTrackingWave) return;
        _isTrackingWave = false;

        float damagePercent = 0;
        if (_playerMaxHealthAtWaveStart > 0)
        {
            damagePercent = (float)_totalDamageTakenInCurrentWave / _playerMaxHealthAtWaveStart * 100f;
        }

        var record = new WavePerformanceRecord(_currentWaveData, damagePercent);
        WaveHistory.Add(record);

        Debug.Log($"<color=green>WAVE PERFORMANCE:</color> Волна завершена. Нанесено игроку <color=red>{damagePercent:F1}%</color> урона. " +
                  $"Оценка: <color=yellow>{record.Rating}</color>.");
    }

    /// <summary>
    /// Проверяет, встречалась ли такая волна ранее.
    /// </summary>
    public bool IsWaveDuplicate(WaveData waveData)
    {
        string keyToCompare = waveData.GetCanonicalKey();
        foreach (var record in WaveHistory)
        {
            if (record.WaveComposition.GetCanonicalKey() == keyToCompare)
            {
                return true;
            }
        }
        return false;
    }
}
