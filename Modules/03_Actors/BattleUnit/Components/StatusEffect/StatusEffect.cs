using UnityEngine;

/// <summary>
/// Абстрактный базовый класс для всех статус-эффектов, которые могут быть применены к юниту.
/// Является ScriptableObject для удобной настройки в инспекторе.
/// </summary>
public abstract class StatusEffect : ScriptableObject
{
    [Tooltip("Название эффекта для логов и UI.")]
    public string effectName;

    /// <summary>
    /// Применяет логику эффекта к цели.
    /// </summary>
    /// <param name="target">Юнит, к которому применяется эффект.</param>
    public abstract void ApplyEffect(BattleUnit target);
}