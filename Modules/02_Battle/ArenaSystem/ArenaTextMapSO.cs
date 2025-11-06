// НАЗНАЧЕНИЕ: Хранит сопоставления между символами в текстовом файле и префабами PropSO, а также типами клеток (CellType) для импорта/экспорта шаблонов арен.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: PropSO, BattleCell.CellType.
// ПРИМЕЧАНИЕ: Является ScriptableObject'ом. Один экземпляр этого объекта может использоваться для всех ArenaTemplateSO.
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ArenaTextMap", menuName = "WaveBeater/Arena Text Map")]
public class ArenaTextMapSO : ScriptableObject
{
    [System.Serializable]
    public struct CharPropMapping
    {
        [Tooltip("Символ, который будет представлять проп в текстовом файле.")]
        public char Character;
        [AssetsOnly, Tooltip("Проп, соответствующий этому символу.")]
        public PropSO Prop;
    }

    [System.Serializable]
    public struct CharTypeMapping
    {
        [Tooltip("Символ, который будет представлять тип клетки в текстовом файле (например, '#' для неразрушаемой, '*' для стекла).")]
        public char Character;
        [Tooltip("Тип клетки, соответствующий этому символу.")]
        public BattleCell.CellType Type;
    }

    #region Поля
    [BoxGroup("SETTINGS/Props"), Tooltip("Список сопоставлений символов и пропов.")]
    [SerializeField] private List<CharPropMapping> _propMappings = new List<CharPropMapping>();
    [BoxGroup("SETTINGS/Cell Types"), Tooltip("Список сопоставлений символов и типов клеток.")]
    [SerializeField] private List<CharTypeMapping> _typeMappings = new List<CharTypeMapping>();
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug; // Добавлено поле
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Находит PropSO, соответствующий указанному символу.
    /// </summary>
    /// <param name="character">Символ для поиска.</param>
    /// <returns>Найденный PropSO или null, если сопоставление не найдено.</returns>
    public PropSO GetProp(char character)
    {
        // Проверяем сначала типы клеток, так как они могут использовать те же символы, что и пропы (хотя это не рекомендуется)
        if (HasTypeMapping(character))
        {
            return null; // Если символ обозначает тип клетки, это не проп
        }
        var mapping = _propMappings.FirstOrDefault(m => m.Character == character);
        if (mapping.Prop != null)
        {
            ColoredDebug.CLog(null, "<color=cyan>ArenaTextMap:</color> Found Prop '{0}' for char '{1}'.", _ColoredDebug, mapping.Prop.name, character);
        }
        return mapping.Prop;
    }

    /// <summary>
    /// Находит символ, соответствующий указанному PropSO.
    /// </summary>
    /// <param name="prop">Проп для поиска.</param>
    /// <returns>Найденный символ или символ '?' по умолчанию, если сопоставление не найдено.</returns>
    public char GetChar(PropSO prop)
    {
        if (prop == null) return '?';
        var mapping = _propMappings.FirstOrDefault(m => m.Prop == prop);
        char result = mapping.Prop != null ? mapping.Character : '?';
        // ColoredDebug.CLog(null, "<color=cyan>ArenaTextMap:</color> Found char '{0}' for Prop '{1}'.", _ColoredDebug, result, prop.name); // Слишком часто
        return result;
    }

    /// <summary>
    /// Находит CellType, соответствующий указанному символу.
    /// </summary>
    /// <param name="character">Символ для поиска.</param>
    /// <returns>Найденный CellType или null, если сопоставление не найдено.</returns>
    public BattleCell.CellType? GetCellType(char character)
    {
        var mapping = _typeMappings.FirstOrDefault(m => m.Character == character);
        if (mapping.Character == character) // Проверка, что нашли соответствие
        {
            ColoredDebug.CLog(null, "<color=cyan>ArenaTextMap:</color> Found CellType '{0}' for char '{1}'.", _ColoredDebug, mapping.Type, character);
            return mapping.Type;
        }
        return null;
    }

    /// <summary>
    /// Находит символ, соответствующий указанному CellType.
    /// </summary>
    /// <param name="type">Тип клетки для поиска.</param>
    /// <returns>Найденный символ или null, если сопоставление не найдено.</returns>
    public char? GetChar(BattleCell.CellType type)
    {
        var mapping = _typeMappings.FirstOrDefault(m => m.Type == type);
        if (mapping.Type == type && mapping.Character != default(char)) // Проверка, что нашли и символ не пустой
        {
            // ColoredDebug.CLog(null, "<color=cyan>ArenaTextMap:</color> Found char '{0}' for CellType '{1}'.", _ColoredDebug, mapping.Character, type); // Слишком часто
            return mapping.Character;
        }
        return null;
    }

    /// <summary>
    /// Проверяет, есть ли для символа сопоставление с типом клетки.
    /// </summary>
    public bool HasTypeMapping(char character)
    {
        return _typeMappings.Any(m => m.Character == character);
    }
    #endregion Публичные методы
}