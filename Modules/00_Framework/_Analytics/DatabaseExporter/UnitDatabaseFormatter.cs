// НАЗНАЧЕНИЕ: Отвечает за преобразование данных о юнитах, их оружии и действиях в стандартизированный машиночитаемый формат для базы данных ИИ.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnit, WeaponData, AIAction, ActionTrigger.
// ПРИМЕЧАНИЕ: Не имеет состояния и содержит только чистые функции для форматирования.
// Является вспомогательным классом для UnitDatabaseExporter.
using System.Text;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;

public class UnitDatabaseFormatter
{
    /// <summary>
    /// Форматирует заголовок файла базы данных.
    /// </summary>
    public string FormatHeader()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[DATABASE_EXPORT_DATE: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}]");
        sb.AppendLine();
        sb.Append(FormatLevelSystemExplanation());
        return sb.ToString();
    }

    /// <summary>
    /// Форматирует полную информацию о одном юните.
    /// </summary>
    public string FormatUnit(BattleUnit unitPrefab)
    {
        if (unitPrefab == null) return "";
        var sb = new StringBuilder();
        sb.AppendLine("[UNIT_START]");
        sb.AppendLine($"[NAME: {unitPrefab.name}]");
        sb.AppendLine($"[FACTION: {unitPrefab.FactionType}]");
        sb.AppendLine($"[BASE_LEVEL: {(int)unitPrefab.Level}]");
        // Форматируем статы
        var stats = unitPrefab.Stats;
        if (stats != null)
        {
            sb.AppendLine("[STATS_START]");
            sb.AppendLine($"[MAX_HEALTH: {stats.MaxHealth}]");
            sb.AppendLine($"[ACTION_POINTS: {stats.MaxActionPoints}]");
            sb.AppendLine($"[UNIT_SIZE: {stats.UnitSize.x}x{stats.UnitSize.y}]");
            sb.AppendLine($"[BOUNTY: {stats.Bounty}]");
            sb.AppendLine($"[CRITICAL_CHANCE: {stats.CriticalHitChance}]");
            sb.AppendLine($"[EVASION_CHANCE: {stats.EvasionChance:F2}]");
            sb.AppendLine($"[CRIT_EVASION_CHANCE: {stats.CriticalHitEvasionChance:F2}]");
            sb.AppendLine($"[AUTO_HEAL: {stats.AutoHealValue}]");
            sb.AppendLine($"[ELECTRO_SHIELD: {stats.MaxElectroShieldCharges}]");
            sb.AppendLine($"[IRON_VEST: {stats.MaxIronVestCharges}]");
            sb.AppendLine($"[EMERGENCY_SYSTEM: {stats.MaxEmergencySystemCharges}]");
            sb.AppendLine($"[BACKLASH_CHANCE: {stats.BacklashChance:P0}]");
            sb.AppendLine("[STATS_END]");
        }

        // Форматируем систему наведения
        var targeting = unitPrefab.TargetingSystem;
        if (targeting != null && targeting.TargetingProfile != null)
        {
            sb.AppendLine("[TARGETING_SYSTEM_START]");
            sb.AppendLine($"[PROFILE_NAME: {targeting.TargetingProfile.name}]");
            sb.AppendLine("[TARGETING_SYSTEM_END]");
        }

        // Форматируем арсенал
        var arsenal = unitPrefab.Arsenal;
        if (arsenal != null && arsenal.InitialWeapons.Any())
        {
            sb.AppendLine("[ARSENAL_START]");
            foreach (var weaponData in arsenal.InitialWeapons)
            {
                sb.Append(FormatWeapon(weaponData));
            }
            sb.AppendLine("[ARSENAL_END]");
        }

        // Форматируем AI и действия
        var brain = unitPrefab.Brain;
        if (brain != null && brain.Profile != null)
        {
            sb.AppendLine("[AI_PROFILE_START]");
            sb.AppendLine($"[PROFILE_NAME: {brain.Profile.name}]");
            sb.AppendLine($"[BEHAVIOR_PATTERN: {brain.Profile.behaviorPattern.name}]");

            if (brain.Profile.behaviorPattern is PrioritizedActionPattern prioritizedPattern)
            {
                sb.AppendLine("\t[PRIORITIZED_ACTIONS_START]");
                if (prioritizedPattern.prioritizedActions != null)
                {
                    int priority = 1;
                    foreach (var prioritizedAction in prioritizedPattern.prioritizedActions)
                    {
                        if (prioritizedAction != null)
                        {
                            sb.AppendLine($"\t\t[P{priority:00}: {prioritizedAction.actionName}] [TYPE: {prioritizedAction.GetType().Name}]");
                        }
                        else
                        {
                            sb.AppendLine($"\t\t[P{priority:00}: NULL_ACTION]");
                        }
                        priority++;
                    }
                }
                sb.AppendLine("\t[PRIORITIZED_ACTIONS_END]");
            }

            foreach (var action in brain.Profile.availableActions)
            {
                sb.Append(FormatAction(action));
            }
            sb.AppendLine("[AI_PROFILE_END]");
        }

        sb.AppendLine("[UNIT_END]");
        sb.AppendLine();
        return sb.ToString();
    }

    /// <summary>
    /// Форматирует информацию об одном оружии.
    /// </summary>
    private string FormatWeapon(WeaponData weaponData)
    {
        if (weaponData == null) return "";
        var sb = new StringBuilder();
        sb.AppendLine("\t[WEAPON_START]");
        sb.AppendLine($"\t[WEAPON_NAME: {weaponData.WeaponName}]");
        foreach (var mode in weaponData.AttackModes)
        {
            sb.Append(FormatAttackMode(mode));
        }
        sb.AppendLine("\t[WEAPON_END]");
        return sb.ToString();
    }

    /// <summary>
    /// Форматирует информацию об одном режиме атаки.
    /// </summary>
    private string FormatAttackMode(AttackMode mode)
    {
        if (mode == null) return "";
        var sb = new StringBuilder();
        sb.AppendLine("\t\t[ATTACK_MODE_START]");
        sb.AppendLine($"\t\t[MODE_NAME: {mode.modeName}]");
        sb.AppendLine($"\t\t[DAMAGE: {mode.damage}]");
        sb.AppendLine($"\t\t[MIN_RANGE: {mode.minRange}]");
        sb.AppendLine($"\t\t[MAX_RANGE: {mode.range}]");
        sb.AppendLine($"\t\t[HIT_CHANCE: {mode.hitChance:P0}]");
        sb.AppendLine($"\t\t[IS_MELEE: {mode.isMelee}]");
        sb.AppendLine($"\t\t[SHOTS_PER_ACTION: {mode.shotsPerAction}]");
        sb.AppendLine($"\t\t[REQUIRES_RELOAD: {mode.requiresReload}]");
        sb.AppendLine($"\t\t[CLIP_SIZE: {mode.clipSize}]");
        sb.AppendLine($"\t\t[RELOAD_TURNS: {mode.reloadTimeTurns}]");
        sb.AppendLine($"\t\t[REQUIRES_AIM: {mode.requiresAim}]");
        sb.AppendLine($"\t\t[TURNS_TO_AIM: {mode.turnsToAim}]");
        sb.AppendLine($"\t\t[IS_DISPOSABLE: {mode.isDisposable}]");
        sb.AppendLine("\t\t[ATTACK_MODE_END]");
        return sb.ToString();
    }

    /// <summary>
    /// Форматирует информацию об одном действии AI, включая его триггеры и специфичные параметры.
    /// </summary>
    private string FormatAction(AIAction action)
    {
        if (action == null) return "";
        var sb = new StringBuilder();
        sb.AppendLine("\t[ACTION_START]");
        sb.AppendLine($"\t[ACTION_NAME: {action.actionName}]");
        sb.AppendLine($"\t[ACTION_TYPE: {action.GetType().Name}]"); // <-- **ДОБАВЛЕНО**
        sb.AppendLine($"\t[ACTION_COST: {action.actionPointCost}]");
        sb.AppendLine($"\t[MAX_USES_PER_TURN: {action.maxUsesPerTurn}]");
        // Форматирование специфичных для действия полей
        if (action is AttackAction attackAction)
        {
            sb.AppendLine($"\t[PREFERS_MELEE: {attackAction.preferMelee}]");
        }
        else if (action is SpecialAttackAction specialAttackAction)
        {
            // Используем рефлексию для доступа к приватным полям
            var modeNameField = typeof(SpecialAttackAction).GetField("_attackModeName", BindingFlags.NonPublic | BindingFlags.Instance);
            var selfEffectsField = typeof(SpecialAttackAction).GetField("_effectsToApplyOnSelf", BindingFlags.NonPublic | BindingFlags.Instance);
            var targetEffectsField = typeof(SpecialAttackAction).GetField("_effectsToApplyOnTarget", BindingFlags.NonPublic | BindingFlags.Instance);

            if (modeNameField != null) sb.AppendLine($"\t[USES_MODE: {modeNameField.GetValue(specialAttackAction)}]");
            var selfEffects = selfEffectsField?.GetValue(specialAttackAction) as List<StatusEffect>;
            if (selfEffects != null && selfEffects.Any())
                sb.AppendLine($"\t[SELF_EFFECTS: {string.Join(", ", selfEffects.Select(e => e.effectName))}]");
            var targetEffects = targetEffectsField?.GetValue(specialAttackAction) as List<StatusEffect>;
            if (targetEffects != null && targetEffects.Any())
                sb.AppendLine($"\t[TARGET_EFFECTS: {string.Join(", ", targetEffects.Select(e => e.effectName))}]");
        }

        // Форматирование триггеров
        if (action.triggers != null && action.triggers.Any())
        {
            sb.AppendLine("\t\t[TRIGGERS_START]");
            foreach (var trigger in action.triggers)
            {
                sb.Append(FormatTrigger(trigger));
            }
            sb.AppendLine("\t\t[TRIGGERS_END]");
        }

        sb.AppendLine("\t[ACTION_END]");
        return sb.ToString();
    }

    /// <summary>
    /// Форматирует информацию об одном триггере действия, используя рефлексию для доступа к его настройкам.
    /// </summary>
    private string FormatTrigger(ActionTrigger trigger)
    {
        if (trigger == null) return "";
        var sb = new StringBuilder();
        sb.AppendLine("\t\t\t[TRIGGER_START]");
        sb.AppendLine($"\t\t\t[TYPE: {trigger.GetType().Name}]");

        // Используем рефлексию для получения всех сериализованных полей
        var fields = trigger.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.IsDefined(typeof(SerializeField), true));
        foreach (var field in fields)
        {
            // Пропускаем поле для отладочных логов
            if (field.Name == "_ColoredDebug") continue;
            // Очищаем имя поля для красивого вывода
            string fieldName = field.Name.Replace("_", "").ToUpper();
            object value = field.GetValue(trigger);
            sb.AppendLine($"\t\t\t[{fieldName}: {value}]");
        }

        sb.AppendLine("\t\t\t[TRIGGER_END]");
        return sb.ToString();
    }

    /// <summary>
    /// Форматирует блок с объяснением системы уровней и формулами.
    /// </summary>
    private string FormatLevelSystemExplanation()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[LEVEL_SYSTEM_START]");
        sb.AppendLine("[DESCRIPTION: This section explains how unit stats are modified by their level.]");
        sb.AppendLine();

        sb.AppendLine("[LEVEL_LEGEND]");
        sb.AppendLine("[LEVEL: 0] [NAME: Green_00] [DESCRIPTION: Обычный]");
        sb.AppendLine("[LEVEL: 1] [NAME: Blue_01] [DESCRIPTION: Базовый]");
        sb.AppendLine("[LEVEL: 2] [NAME: Yellow_02] [DESCRIPTION: Усиленный]");
        sb.AppendLine("[LEVEL: 3] [NAME: Orange_03] [DESCRIPTION: Ветеран]");
        sb.AppendLine("[LEVEL: 4] [NAME: Red_04] [DESCRIPTION: Опасный]");
        sb.AppendLine("[LEVEL: 5] [NAME: Pink_05] [DESCRIPTION: Стремительный]");
        sb.AppendLine("[LEVEL: 6] [NAME: Turquoise_06] [DESCRIPTION: Дух стихий]");
        sb.AppendLine("[LEVEL: 7] [NAME: DarkBlue_07] [DESCRIPTION: Элитный]");
        sb.AppendLine("[LEVEL: 8] [NAME: Violet_08] [DESCRIPTION: Мощный]");
        sb.AppendLine("[LEVEL: 9] [NAME: Maroon_09] [DESCRIPTION: Яростный]");
        sb.AppendLine("[LEVEL: 10] [NAME: Indigo_10] [DESCRIPTION: Мистик]");
        sb.AppendLine("[LEVEL: 11] [NAME: Grey_11] [DESCRIPTION: Каменный]");
        sb.AppendLine("[LEVEL: 12] [NAME: Brown_12] [DESCRIPTION: Стойкий]");
        sb.AppendLine("[LEVEL: 13] [NAME: Silver_13] [DESCRIPTION: Чемпион]");
        sb.AppendLine("[LEVEL: 14] [NAME: Gold_14] [DESCRIPTION: Легендарный]");
        sb.AppendLine("[LEVEL: 15] [NAME: White_15] [DESCRIPTION: Призрачный]");
        sb.AppendLine("[LEVEL: 16] [NAME: Black_16] [DESCRIPTION: Проклятый]");
        sb.AppendLine();

        sb.AppendLine("[FORMULAS]");
        sb.AppendLine("[HP_FORMULA: final_hp = round(base_hp + (base_hp * level * 0.5))]");
        sb.AppendLine("[DAMAGE_FORMULA: final_damage = base_damage + (level / 2)]");
        sb.AppendLine();

        sb.AppendLine("[LEVEL_SYSTEM_END]");
        sb.AppendLine();
        return sb.ToString();
    }
}