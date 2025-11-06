using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "UpgradeCardDataSO_", menuName = "--->WaveBeater/UpgradeCardDataSO ", order = 9999)]
public class UpgradeCardDataSO : ScriptableObject
{
    public enum CardTypeBonus
    {
        NotSet,
        Health,
        Damage,
        SlowDownWave,
        Heal,
        Money,
        AutoHeal,
        ElectroShield,
        EmergencySystem,
        Backlash,
        DoubleMoney,
        Ironclad,
        CriticalHit,
    }

    public enum CardTCategory
    {
        NotSet,
        Shop,
        Hospital,
        Treasure,
        Award
    }

    [BoxGroup("SETTINGS"), SerializeField] private CardTypeBonus _bonusType;
    [BoxGroup("SETTINGS"), SerializeField] private CardTCategory _сategory;
    [BoxGroup("SETTINGS"), SerializeField] private int _bonusValue = 5;
    [BoxGroup("SETTINGS"), SerializeField] private int _initialCost = 15;
    [BoxGroup("SETTINGS"), SerializeField] private int _costIncrease = 7;
    [BoxGroup("SETTINGS"), SerializeField] private bool _canIncreaseCost = true;
    [BoxGroup("UI CONTENT"), SerializeField] private string _title = "Bonus Title";
    [BoxGroup("UI CONTENT"), PreviewField, SerializeField] private Sprite _icon;
    [BoxGroup("UI STYLE"), SerializeField] private Color _borderColor = Color.white;
    [BoxGroup("UI STYLE"), SerializeField] private Color _backgroundColor = Color.gray;
    public CardTypeBonus BonusType { get => _bonusType; }
    public int BonusValue { get => _bonusValue; }
    public int InitialCost { get => _initialCost; }
    public int CostIncrease { get => _costIncrease; }
    public string Title { get => _title; }
    public Sprite Icon { get => _icon; }
    public Color BorderColor { get => _borderColor; }
    public Color BackgroundColor { get => _backgroundColor; }
    public bool CanIncreaseCost { get => _canIncreaseCost; }
    public CardTCategory Сategory { get => _сategory; }
}
