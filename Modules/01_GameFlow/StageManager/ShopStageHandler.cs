using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;

/// <summary>
/// Управляет этапом магазина в игре, отображая UI и вызывая колбэк по его завершении.
/// </summary>
public class ShopStageHandler : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private UpgradeShopController _UI_CardManager;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private UnityAction _onShopClosed;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Методы UNITY
    private void Awake()
    {
        if (_UI_CardManager == null) DebugUtils.LogMissingReference(this, nameof(_UI_CardManager));
    }

    private void OnDestroy()
    {
        if (_UI_CardManager != null)
        {
            _UI_CardManager.OnCardManagerFinished -= CloseShop;
        }
    }
    #endregion Методы UNITY

    #region Публичные методы
    public void StartShop(UnityAction onFinished, UpgradeCardDataSO.CardTCategory category)
    {
        ColoredDebug.CLog(gameObject, $"<color=cyan>ShopStageHandler:</color> Запуск этапа магазина. Категория: <color=orange>{category}</color>. Колбэк: <color=yellow>{onFinished?.Method.Name ?? "NONE"}</color>.", _ColoredDebug);

        _onShopClosed = onFinished;

        _UI_CardManager.OnCardManagerFinished += CloseShop;
        ColoredDebug.CLog(gameObject, "<color=cyan>ShopStageHandler:</color> Подписан метод <color=yellow>HandleShopClosed</color> на событие <color=lime>OnCardManagerFinished</color>.", _ColoredDebug);

        _UI_CardManager.ShowShop(category);

        if (Settings.AutoBuyUpgradesInShop)
        {
            //----MusicManager.ActiveInstance.PlayWanderingAroundMusic();
        }
        else
        {
            MusicManager.Instance.PlayShopMusic();
        }
    }
    #endregion Публичные методы

    #region Личные методы
    private void CloseShop()
    {
        if (!Settings.AutoBuyUpgradesInShop)
        {
            MusicManager.Instance.PlayWanderingAroundMusic();
        }

        ColoredDebug.CLog(gameObject, "<color=orange>ShopStageHandler:</color> Магазин закрыт.", _ColoredDebug);

        _UI_CardManager.OnCardManagerFinished -= CloseShop;
        ColoredDebug.CLog(gameObject, "<color=cyan>ShopStageHandler:</color> Отписан метод <color=yellow>HandleShopClosed</color> от события <color=lime>OnCardManagerFinished</color>.", _ColoredDebug);

        if (_onShopClosed != null)
        {
            ColoredDebug.CLog(gameObject, "<color=cyan>ShopStageHandler:</color> Вызов колбэка <color=yellow>{0}</color>.", _ColoredDebug, _onShopClosed.Method.Name);
            _onShopClosed.Invoke();
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=cyan>ShopStageHandler:</color> Колбэк для вызова не найден.", _ColoredDebug);
        }
    }
    #endregion Личные методы
}