namespace SalesDashboard.Api.Analytics;

/// <summary>
/// Закрытый период дат. Обе границы включительны.
/// </summary>
/// <param name="From">Начало периода.</param>
/// <param name="To">Конец периода.</param>
public record DateRange(DateOnly From, DateOnly To)
{
    /// <summary>Число календарных дней, включая обе границы.</summary>
    public int InclusiveDays => To.DayNumber - From.DayNumber + 1;

    /// <summary>
    /// Предыдущий период той же длины, заканчивающийся за день до текущего.
    /// </summary>
    /// <returns>Сопоставимый предыдущий диапазон.</returns>
    public DateRange PreviousComparable()
    {
        var prevTo = From.AddDays(-1);
        var prevFrom = prevTo.AddDays(-(InclusiveDays - 1));
        return new DateRange(prevFrom, prevTo);
    }
}

/// <summary>
/// Значение метрики и процент изменения к предыдущему периоду.
/// </summary>
/// <param name="Value">Значение за текущий период.</param>
/// <param name="ChangePercent">Изменение в процентах или null, если базы нет.</param>
public record MetricValue(decimal Value, decimal? ChangePercent);

/// <summary>
/// KPI за период и сравнение с предыдущим сопоставимым периодом.
/// Маржинальность и средний чек равны null, если выручка или число продаж нулевые.
/// </summary>
/// <param name="Revenue">Выручка.</param>
/// <param name="GrossProfit">Валовая прибыль.</param>
/// <param name="Margin">Маржинальность (Gross Profit / Revenue) либо null.</param>
/// <param name="SalesCount">Число оплаченных продаж. Возвраты и отмены не считаются.</param>
/// <param name="AverageCheck">Средний чек либо null.</param>
/// <param name="BestManager">Менеджер с наибольшей валовой прибылью либо null.</param>
/// <param name="Period">Запрошенный период.</param>
/// <param name="PreviousPeriod">Предыдущий сопоставимый период.</param>
public record KpiResponse(
    MetricValue Revenue,
    MetricValue GrossProfit,
    MetricValue? Margin,
    MetricValue SalesCount,
    MetricValue? AverageCheck,
    BestManagerDto? BestManager,
    DateRangeDto Period,
    DateRangeDto PreviousPeriod);

/// <summary>Менеджер с максимальной валовой прибылью за период.</summary>
/// <param name="Id">Идентификатор.</param>
/// <param name="FullName">Полное имя.</param>
/// <param name="Initials">Инициалы.</param>
/// <param name="AvatarColor">Цвет аватара.</param>
/// <param name="GrossProfit">Валовая прибыль.</param>
public record BestManagerDto(int Id, string FullName, string Initials, string AvatarColor, decimal GrossProfit);

/// <summary>Диапазон дат для ответа API.</summary>
/// <param name="From">Начало, включительно.</param>
/// <param name="To">Конец, включительно.</param>
public record DateRangeDto(DateOnly From, DateOnly To);

/// <summary>
/// Строка рейтинга менеджера. При равной метрике сортировки Rank совпадает.
/// </summary>
/// <param name="Rank">Место с учётом ничьих (1224).</param>
/// <param name="ManagerId">Идентификатор менеджера.</param>
/// <param name="FullName">Полное имя.</param>
/// <param name="Team">Команда.</param>
/// <param name="Title">Должность.</param>
/// <param name="Initials">Инициалы.</param>
/// <param name="AvatarColor">Цвет аватара.</param>
/// <param name="IsActive">Действующий менеджер.</param>
/// <param name="SalesCount">Число оплаченных продаж.</param>
/// <param name="Revenue">Выручка.</param>
/// <param name="GrossProfit">Валовая прибыль.</param>
/// <param name="AverageCheck">Средний чек либо null.</param>
/// <param name="Margin">Маржинальность либо null.</param>
/// <param name="ChangePercent">Изменение метрики сортировки к предыдущему периоду.</param>
public record ManagerRankingRow(
    int Rank,
    int ManagerId,
    string FullName,
    string Team,
    string Title,
    string Initials,
    string AvatarColor,
    bool IsActive,
    int SalesCount,
    decimal Revenue,
    decimal GrossProfit,
    decimal? AverageCheck,
    decimal? Margin,
    decimal? ChangePercent);

/// <summary>Точка динамики выручки и валовой прибыли.</summary>
/// <param name="Period">Подпись оси: день или месяц.</param>
/// <param name="From">Начало бакета.</param>
/// <param name="To">Конец бакета.</param>
/// <param name="Revenue">Выручка.</param>
/// <param name="GrossProfit">Валовая прибыль.</param>
/// <param name="SalesCount">Число оплаченных продаж.</param>
public record TrendPoint(string Period, DateOnly From, DateOnly To, decimal Revenue, decimal GrossProfit, int SalesCount);

/// <summary>Агрегат по категории.</summary>
/// <param name="CategoryId">Идентификатор категории.</param>
/// <param name="Name">Название.</param>
/// <param name="Revenue">Выручка.</param>
/// <param name="GrossProfit">Валовая прибыль.</param>
/// <param name="Margin">Маржинальность либо null.</param>
/// <param name="Share">Доля выручки категории в общей выручке периода, 0..1.</param>
public record CategoryStats(int CategoryId, string Name, decimal Revenue, decimal GrossProfit, decimal? Margin, decimal Share);

/// <summary>Агрегат по товару для топа продуктов.</summary>
/// <param name="ProductId">Идентификатор товара.</param>
/// <param name="Name">Название.</param>
/// <param name="CategoryName">Категория.</param>
/// <param name="Revenue">Выручка.</param>
/// <param name="GrossProfit">Валовая прибыль.</param>
/// <param name="Quantity">Количество с учётом знака возврата.</param>
public record ProductStats(int ProductId, string Name, string CategoryName, decimal Revenue, decimal GrossProfit, int Quantity);

/// <summary>
/// Последняя продажа. Суммы уже со знаком: возврат отрицательный, отмена — ноль.
/// </summary>
/// <param name="Id">Идентификатор продажи.</param>
/// <param name="SaleDate">Дата.</param>
/// <param name="Status">Paid, Cancelled или Refunded.</param>
/// <param name="ManagerName">Имя менеджера.</param>
/// <param name="ManagerInitials">Инициалы.</param>
/// <param name="ManagerAvatarColor">Цвет аватара.</param>
/// <param name="CustomerName">Имя клиента.</param>
/// <param name="CustomerCompany">Компания.</param>
/// <param name="Products">Названия товаров в чеке.</param>
/// <param name="Amount">Сумма со знаком статуса.</param>
/// <param name="GrossProfit">Валовая прибыль со знаком статуса.</param>
public record RecentSaleDto(
    int Id,
    DateOnly SaleDate,
    string Status,
    string ManagerName,
    string ManagerInitials,
    string ManagerAvatarColor,
    string CustomerName,
    string CustomerCompany,
    IReadOnlyList<string> Products,
    decimal Amount,
    decimal GrossProfit);
