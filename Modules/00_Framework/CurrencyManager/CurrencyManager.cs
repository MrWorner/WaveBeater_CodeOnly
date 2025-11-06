using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;

public class CurrencyManager : MonoBehaviour
{
    #region Поля
    [BoxGroup("DEBUG"), SerializeField] private int _currency = 0;
    #endregion Поля

    #region Свойства
    private static CurrencyManager _instance;
    public static CurrencyManager Instance { get => _instance; }
    public int Currency { get => _currency; }
    #endregion Свойства

    #region События
    public UnityAction<int> OnCurrencyChanged;
    #endregion События

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null) { DebugUtils.LogInstanceAlreadyExists(this); } else _instance = this;
    }
    #endregion Методы UNITY

    #region Публичные методы
    public void AddCurrency(int amount)
    {
        if (amount <= 0)
        {
            amount = 1;
        }
        _currency += amount;
        OnCurrencyChanged?.Invoke(_currency);
    }

    public bool SpendCurrency(int amount)
    {
        if (_currency >= amount)
        {
            _currency -= amount;
            OnCurrencyChanged?.Invoke(_currency);
            return true;
        }
        return false;
    }

    public void SetCurrency(int amount)
    {
        _currency = amount;
        OnCurrencyChanged?.Invoke(_currency);
    }
    #endregion Публичные методы
}
