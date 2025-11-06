// НАЗНАЧЕНИЕ: Отвечает за преобразование игровых событий и состояний в стандартизированные строковые форматы для записи в лог-файл.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnit, BattleGrid, BattleReport.
// ПРИМЕЧАНИЕ: Не имеет состояния и содержит только чистые функции для форматирования.
// Является вспомогательным классом для BattleLogger.
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class BattleLogFormatter
{
    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    // Словари для хранения символов юнитов и пропов для текущего боя
    private Dictionary<BattleUnit, string> _unitSymbols;
    private Dictionary<GameObject, string> _propSymbols;
    // Строки с символами для обозначения врагов и пропов
    private const string ENEMY_SYMBOLS = "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string PROP_SYMBOLS = "abcdefghijklmnopqrstuvwxyz";
    #endregion Поля

    #region Свойства
    public Dictionary<BattleUnit, string> UnitSymbols { get => _unitSymbols; }
    public Dictionary<GameObject, string> PropSymbols { get => _propSymbols; }
    #endregion Свойства

    #region Публичные методы
    /// <summary>
    /// Конструктор, инициализирует словари символов.
    /// </summary>
    public BattleLogFormatter()
    {
        _unitSymbols = new Dictionary<BattleUnit, string>();
        _propSymbols = new Dictionary<GameObject, string>();
    }

    /// <summary>
    /// Форматирует заголовок для начала новой сессии логирования.
    /// </summary>
    /// <param name="sessionID">Уникальный ID сессии (если используется).</param>
    /// <returns>Строка заголовка сессии.</returns>
    public string FormatSessionStart(string sessionID)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[SESSION_START: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}]");
        if (!string.IsNullOrEmpty(sessionID))
        {
            sb.AppendLine($"[SESSION_ID: {sessionID}]");
        }
        sb.AppendLine();
        return sb.ToString();
    }

    /// <summary>
    /// Форматирует заголовок для начала нового боя, включая легенду символов.
    /// </summary>
    /// <param name="battleID">Номер боя в текущей сессии.</param>
    /// <param name="allUnits">Список всех юнитов, участвующих в бою.</param>
    /// <param name="activeProps">Список всех активных пропов на поле.</param>
    /// <returns>Строка заголовка боя с легендой.</returns>
    public string FormatBattleStart(int battleID, List<BattleUnit> allUnits, IReadOnlyList<GameObject> activeProps)
    {
        // Используем null вместо gameObject, так как это не MonoBehaviour
        ColoredDebug.CLog(null, "<color=cyan>BattleLogFormatter:</color> Начало форматирования заголовка боя <color=yellow>#{0}</color>.", _ColoredDebug, battleID);
        _unitSymbols.Clear(); // Очищаем символы от прошлого боя
        _propSymbols.Clear();
        var sb = new StringBuilder();

        // Шапка боя
        sb.AppendLine("======================================================================");
        sb.AppendLine($"[BATTLE_START: {battleID}]");
        sb.AppendLine("======================================================================");

        // Легенда символов сетки
        sb.AppendLine("[GRID_SYMBOLS]");
        sb.AppendLine("[.] - Intact Cell (Проходимая целая клетка)");
        sb.AppendLine("[~] - Cracked Cell (Проходимая треснувшая клетка)");
        sb.AppendLine("[O] - Hole (Непроходимая дыра)");
        // --- ДОБАВЛЕНО ---
        sb.AppendLine("[#] - Indestructible Cell (Неразрушаемая клетка)");
        sb.AppendLine("[*] - Glass Cell (Стеклянная клетка)");
        // --- КОНЕЦ ДОБАВЛЕНИЯ ---
        sb.AppendLine("[X] - Blocked Cell (Существует, но непроходима/занята не юнитом/пропом)");
        sb.AppendLine("[ ] - Abyss/Null/Deactivated (Не существует или деактивирована)");
        sb.AppendLine();

        // Описание системы координат
        sb.AppendLine("[COORDINATE_SYSTEM]");
        sb.AppendLine("(0,0) is the TOP-LEFT corner based on grid generation logic.");
        sb.AppendLine("Y-axis increases downwards in the data.");
        sb.AppendLine("X-axis increases to the right.");
        sb.AppendLine();

        // Легенда юнитов
        sb.AppendLine("[UNIT_LEGEND]");
        int enemyIndex = 0; // Счетчик для символов врагов
                            // Сортируем юнитов для консистентности (сначала герой, потом враги по имени)
        foreach (var unit in allUnits.OrderBy(u => u.FactionType).ThenBy(u => u.name))
        {
            if (unit == null) continue; // Пропускаем null юнитов

            if (unit.FactionType == BattleUnit.Faction.Hero)
            {
                _unitSymbols[unit] = "H"; // Герой всегда 'H'
                sb.AppendLine($"[H] - {unit.name} [LEVEL: {(int)unit.Level}]");
            }
            else if (unit.FactionType == BattleUnit.Faction.Enemy)
            {
                if (enemyIndex < ENEMY_SYMBOLS.Length) // Проверяем, что символы не кончились
                {
                    string symbol = ENEMY_SYMBOLS[enemyIndex].ToString(); // Берем следующий символ
                    _unitSymbols[unit] = symbol; // Сохраняем символ для юнита
                    sb.AppendLine($"[{symbol}] - {unit.name} [LEVEL: {(int)unit.Level}]"); // Добавляем в легенду
                    enemyIndex++;
                }
                else
                {
                    sb.AppendLine($"[?] - {unit.name} [LEVEL: {(int)unit.Level}] (Symbol limit reached)"); // Если символы кончились
                }
            }
            // Можно добавить обработку других фракций (Friendly) при необходимости
        }

        // Легенда пропов (если они есть)
        if (activeProps != null && activeProps.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("[PROP_LEGEND]");
            int propIndex = 0; // Счетчик для символов пропов
                               // Сортируем пропы по имени для консистентности
            foreach (var prop in activeProps.Where(p => p != null).OrderBy(p => p.name))
            {
                if (propIndex < PROP_SYMBOLS.Length)
                {
                    string symbol = PROP_SYMBOLS[propIndex].ToString();
                    _propSymbols[prop] = symbol; // Сохраняем символ для пропа
                    sb.AppendLine($"[{symbol}] - {prop.name}"); // Добавляем в легенду
                    propIndex++;
                }
                else
                {
                    sb.AppendLine($"[?] - {prop.name} (Symbol limit reached)");
                }
            }
        }

        sb.AppendLine();
        ColoredDebug.CLog(null, "<color=cyan>BattleLogFormatter:</color> Легенда для <color=yellow>{0}</color> юнитов и <color=yellow>{1}</color> пропов создана.", _ColoredDebug, allUnits.Count, activeProps?.Count ?? 0);
        return sb.ToString();
    }

    /// <summary>
    /// Форматирует информацию о начале нового хода.
    /// </summary>
    /// <param name="turnNumber">Номер хода.</param>
    /// <param name="turnOwner">Владелец хода ("Player" или "Enemies").</param>
    /// <returns>Строка с информацией о ходе.</returns>
    public string FormatTurnStart(int turnNumber, string turnOwner)
    {
        // Добавляем +1 к turnNumber для отображения (ходы считаются с 0 внутри)
        return $"[TURN: {turnNumber + 1}]\n[TURN_OWNER: {turnOwner}]\n";
    }

    /// <summary>
    /// Форматирует текущее состояние игровой сетки в виде текстовой таблицы.
    /// </summary>
    /// <param name="grid">Объект BattleGrid.</param>
    /// <param name="allUnits">Список всех юнитов на поле.</param>
    /// <param name="activeProps">Список всех активных пропов на поле.</param>
    /// <returns>Многострочное строковое представление сетки.</returns>
    public string FormatGridState(BattleGrid grid, List<BattleUnit> allUnits, IReadOnlyList<GameObject> activeProps)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[GRID_STATE]");

        if (grid == null || grid.Grid == null) // Проверка на null
        {
            sb.AppendLine("ERROR: BattleGrid not available!");
            ColoredDebug.CLog(null, "<color=red>BattleLogFormatter:</color> Ошибка форматирования GridState - BattleGrid недоступен!", _ColoredDebug);
            return sb.ToString();
        }

        // --- Предварительный расчет позиций юнитов и пропов для ускорения ---
        // Словарь: Координата -> Юнит
        var unitPositions = allUnits
            .Where(u => u != null && u.IsAlive && u.Movement != null && u.Movement.OccupiedCells != null) // Доп. проверки
            .SelectMany(u => u.Movement.OccupiedCells.Select(cell => new { Pos = cell.Position, Unit = u })) // Получаем пары {Клетка, Юнит}
            .GroupBy(item => item.Pos) // Группируем по позиции (на случай ошибок, когда клетка занята несколькими)
            .ToDictionary(group => group.Key, group => group.First().Unit); // Берем первого юнита для каждой клетки

        // Словарь: Координата -> Проп
        var propPositions = new Dictionary<Vector2Int, GameObject>();
        if (grid.AllCells != null && activeProps != null)
        {
            foreach (var cell in grid.AllCells)
            {
                // Проверяем, что клетка существует, занята, это GameObject и он есть в списке активных пропов
                if (cell != null && cell.Occupant is GameObject prop && activeProps.Contains(prop))
                {
                    propPositions[cell.Position] = prop;
                }
            }
        }
        // --- Конец предварительного расчета ---

        // Идем по сетке сверху вниз (по Y)
        for (int y = 0; y < grid.Height; y++)
        {
            var rowBuilder = new StringBuilder(); // Строка для текущего ряда
                                                  // Идем по ряду слева направо (по X)
            for (int x = 0; x < grid.Width; x++)
            {
                var currentPos = new Vector2Int(x, y);
                BattleCell cell = grid.Grid[x, y]; // Получаем клетку
                string symbol;

                // 1. Проверяем, существует ли клетка и активна ли она
                if (cell == null || cell.IsDeactivated)
                {
                    symbol = " "; // Пробел для отсутствующей/деактивированной
                }
                // 2. Проверяем, занята ли клетка юнитом
                else if (unitPositions.TryGetValue(currentPos, out BattleUnit unit))
                {
                    // Используем символ из словаря или '?' если не найден
                    symbol = _unitSymbols.TryGetValue(unit, out string s) ? s : "?";
                }
                // 3. Проверяем, занята ли клетка пропом
                else if (propPositions.TryGetValue(currentPos, out GameObject prop))
                {
                    // Используем символ из словаря или '#' если не найден
                    symbol = _propSymbols.TryGetValue(prop, out string s) ? s : "#";
                }
                // 4. Проверка ТИПА клетки перед состоянием
                else if (cell.Type == BattleCell.CellType.Indestructible)
                {
                    symbol = "#"; // Символ для неразрушаемой
                }
                else if (cell.Type == BattleCell.CellType.Glass)
                {
                    symbol = "*"; // Символ для стеклянной
                }
                // 5. Если клетка не занята и не спец. тип, смотрим на ее состояние
                else if (!cell.IsPassable) // Если клетка НЕ проходима (но не null и не деактивирована)
                {
                    // Это может быть дыра (Hole) или другой тип непроходимой клетки
                    if (cell.CurrentState == BattleCell.CellState.Hole)
                    {
                        symbol = "O"; // Дыра
                    }
                    else
                    {
                        symbol = "X"; // Другая непроходимая (например, если IsPassable = false у пропа)
                    }
                }
                // 6. Если клетка проходима и не занята (и тип Standard)
                else
                {
                    switch (cell.CurrentState)
                    {
                        case BattleCell.CellState.Intact: symbol = "."; break; // Целая
                        case BattleCell.CellState.Cracked: symbol = "~"; break; // Треснувшая
                                                                                // Состояние Hole уже обработано выше в проверке IsPassable
                        default: symbol = "?"; break; // Неизвестное состояние
                    }
                }
                rowBuilder.Append($"[{symbol}]"); // Добавляем символ в скобках
            }
            sb.AppendLine(rowBuilder.ToString()); // Добавляем собранный ряд в общий лог
        }
        ColoredDebug.CLog(null, "<color=cyan>BattleLogFormatter:</color> Состояние сетки отформатировано.", _ColoredDebug);
        return sb.ToString();
    }


    /// <summary>
    /// Форматирует информацию о состоянии всех живых юнитов на поле.
    /// </summary>
    /// <param name="allUnits">Список всех юнитов.</param>
    /// <param name="unitSymbols">Словарь с символами для юнитов.</param>
    /// <returns>Многострочное представление состояния юнитов.</returns>
    public string FormatUnitStates(BattleGrid grid, List<BattleUnit> allUnits, Dictionary<BattleUnit, string> unitSymbols) // --- ИЗМЕНЕНО: Добавлен BattleGrid ---
    {
        var sb = new StringBuilder();
        sb.AppendLine("[UNITS_STATE]");
        // Сортируем юнитов для консистентности
        foreach (var unit in allUnits.OrderBy(u => u.FactionType).ThenBy(u => u.name))
        {
            // Пропускаем мертвых или null юнитов
            if (unit == null || !unit.IsAlive) continue;

            // Получаем символ юнита
            string symbol = unitSymbols.TryGetValue(unit, out string s) ? $"[{s}] " : "";
            string ammoStatus = ""; // Строка для информации о патронах

            // Получение символа клетки ---
            string cellSymbolString = "";
            BattleCell occupiedCell = grid?.GetCell(unit.CurrentPosition);
            if (occupiedCell != null)
            {
                string cellSymbol = GetCellSymbol(occupiedCell);
                cellSymbolString = $", CELL: [{cellSymbol}]";
            }

            // Проверяем наличие арсенала и основного оружия (не ближнего боя)
            var arsenal = unit.Arsenal;
            if (arsenal != null)
            {
                // Ищем первый режим не ближнего боя, требующий перезарядки
                var primaryWeaponMode = arsenal.GetAllAttackModes().FirstOrDefault(m => !m.isMelee && m.requiresReload);
                if (primaryWeaponMode != null)
                {
                    // Получаем текущее количество патронов (0, если ключ не найден)
                    arsenal.CurrentAmmo.TryGetValue(primaryWeaponMode, out int ammo);
                    ammoStatus = $"AMMO: {ammo}/{primaryWeaponMode.clipSize}"; // Формируем строку
                }
            }

            // Добавляем строку с информацией о юните
            sb.AppendLine($"- {symbol}[UNIT: {unit.name}] [HP: {unit.Health.CurrentHealth}/{unit.Stats.MaxHealth}] [POS: {unit.CurrentPosition}{cellSymbolString}] {ammoStatus}");
        }
        ColoredDebug.CLog(null, "<color=cyan>BattleLogFormatter:</color> Состояние юнитов отформатировано.", _ColoredDebug);
        return sb.ToString();
    }

    /// <summary>
    /// Форматирует информацию о выполненном действии.
    /// </summary>
    /// <param name="performer">Юнит, выполнивший действие.</param>
    /// <param name="actionName">Название действия.</param>
    /// <param name="details">Дополнительные детали (цель, режим атаки и т.д.).</param>
    /// <returns>Строка с информацией о действии.</returns>
    public string FormatAction(BattleUnit performer, string actionName, string details)
    {
        if (performer == null) return "[ACTION] [PERFORMER: NULL]\n"; // Защита от null
                                                                      // Получаем символ исполнителя
        string symbol = _unitSymbols.TryGetValue(performer, out string s) ? $"[{s}] " : "";
        // Формируем строку лога
        return $"[ACTION] {symbol}[PERFORMER: {performer.name}] [TYPE: {actionName}] [DETAILS: {details}]\n";
    }

    /// <summary>
    /// Форматирует информацию об изменении здоровья юнита.
    /// </summary>
    /// <param name="target">Юнит, чье здоровье изменилось.</param>
    /// <param name="amount">Величина изменения (отрицательная для урона, положительная для лечения).</param>
    /// <param name="reason">Причина изменения (например, "Damage from Enemy1").</param>
    /// <param name="isCritical">Было ли изменение результатом критического удара/лечения.</param>
    /// <returns>Строка с информацией об изменении здоровья.</returns>
    public string FormatHealthChange(BattleUnit target, int amount, string reason, bool isCritical = false)
    {
        if (target == null) return "[HEALTH_CHANGE] [TARGET: NULL]\n"; // Защита от null
                                                                       // Получаем символ цели
        string symbol = _unitSymbols.TryGetValue(target, out string s) ? $"[{s}] " : "";
        // Добавляем "CRITICAL", если нужно
        string critString = isCritical ? "CRITICAL " : "";
        // Формируем строку лога, включая новое значение HP
        return $"[HEALTH_CHANGE] {symbol}[TARGET: {target.name}] [AMOUNT: {amount}] [REASON: {critString}{reason}] [NEW_HP: {target.Health.CurrentHealth}]\n";
    }

    /// <summary>
    /// Форматирует информацию о смерти юнита.
    /// </summary>
    /// <param name="deceased">Погибший юнит.</param>
    /// <returns>Строка с информацией о смерти.</returns>
    public string FormatDeath(BattleUnit deceased)
    {
        if (deceased == null) return "[DEATH] [UNIT: NULL]\n"; // Защита от null
                                                               // Получаем символ погибшего
        string symbol = _unitSymbols.TryGetValue(deceased, out string s) ? $"[{s}] " : "";
        // Формируем строку лога
        return $"[DEATH] {symbol}[UNIT: {deceased.name}]\n";
    }

    /// <summary>
    /// Форматирует аналитический отчет по итогам волны.
    /// </summary>
    /// <param name="report">Объект с данными отчета.</param>
    /// <returns>Многострочное представление отчета.</returns>
    public string FormatWaveAnalysis(BattleReport report)
    {
        if (report == null) return "[WAVE_ANALYSIS] ERROR: Report is null!\n"; // Защита от null
        var sb = new StringBuilder();
        sb.AppendLine("[WAVE_ANALYSIS_START]");
        sb.AppendLine($"[WAVE_NUMBER: {report.WaveNumber}]");
        sb.AppendLine($"[PLAYER_PERFORMANCE: {report.Performance}]"); // Оценка производительности
        sb.AppendLine($"[HERO_HEALTH_RATIO: {report.HeroHealthRatio:F2}]"); // Отношение здоровья героя к макс. HP в конце волны
        sb.AppendLine($"[STRONG_PLAY_STREAK: {report.StrongPlayStreak}]"); // Серия сильной игры
        sb.AppendLine($"[SUSTAINED_SUCCESS_STREAK: {report.SustainedSuccessStreak}]"); // Серия успехов
        sb.AppendLine($"[DIRECTOR_DECISION: {report.SystemDecision}]"); // Решение Директора
        sb.AppendLine($"[NEXT_THREAT_LEVEL: {report.ThreatLevel:F2}]"); // Уровень угрозы для следующей волны
        sb.AppendLine("[WAVE_ANALYSIS_END]");
        sb.AppendLine();
        ColoredDebug.CLog(null, "<color=cyan>BattleLogFormatter:</color> Анализ волны <color=yellow>#{0}</color> отформатирован.", _ColoredDebug, report.WaveNumber);
        return sb.ToString();
    }

    /// <summary>
    /// Форматирует информацию о завершении боя.
    /// </summary>
    /// <param name="result">Результат боя ("ПОБЕДА" или "ПОРАЖЕНИЕ").</param>
    /// <returns>Строка с результатом боя.</returns>
    public string FormatBattleEnd(string result)
    {
        return $"\n[BATTLE_END] [RESULT: {result}]\n";
    }

    /// <summary>
    /// Определяет символ, представляющий состояние и тип клетки.
    /// </summary>
    /// <param name="cell">Клетка BattleCell.</param>
    /// <returns>Символ (., ~, O, X, #, *) или " ".</returns>
    private string GetCellSymbol(BattleCell cell)
    {
        if (cell == null || cell.IsDeactivated)
        {
            return " "; // Пробел для отсутствующей/деактивированной
        }

        // 1. Проверка ТИПА клетки (игнорируется, если она занята юнитом или пропом, но здесь мы уверены, что она занята только юнитом)
        // Мы проверяем тип, т.к. нам не нужно отображать юнита, а нужно отобразить СТАТУС клетки.
        if (cell.Occupant != null)
        {
            // Если клетка занята, но мы хотим знать ее *фоновый* тип:
            if (cell.Type == BattleCell.CellType.Indestructible)
            {
                return "#"; // Символ для неразрушаемой
            }
            if (cell.Type == BattleCell.CellType.Glass)
            {
                return "*"; // Символ для стеклянной
            }
        }

        // 2. Если клетка не занята пропом (или мы не хотим отображать символ пропа здесь), смотрим на ее состояние
        if (!cell.IsPassable)
        {
            if (cell.CurrentState == BattleCell.CellState.Hole)
            {
                return "O"; // Дыра
            }
            else if (cell.Occupant is GameObject)
            {
                return "X"; // Блокирована (пропом) - хотя этот код тут не должен выполняться, если GetCellSymbol вызывается только для юнитов. Оставим на всякий случай.
            }
            else
            {
                return "X"; // Другая непроходимая
            }
        }

        // 3. Если клетка проходима
        switch (cell.CurrentState)
        {
            case BattleCell.CellState.Intact: return "."; // Целая
            case BattleCell.CellState.Cracked: return "~"; // Треснувшая
            default: return "?"; // Неизвестное состояние
        }
    }

    #endregion Публичные методы
}