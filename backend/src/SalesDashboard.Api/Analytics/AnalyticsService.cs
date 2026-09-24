using Microsoft.EntityFrameworkCore;
using SalesDashboard.Api.Data;
using SalesDashboard.Api.Domain;

namespace SalesDashboard.Api.Analytics;

/// <summary>
/// Агрегаты продаж за период. Cancelled исключаются.
/// Refunded вычитаются из Revenue и Cost и не увеличивают число продаж.
/// </summary>
public class AnalyticsService(SalesDbContext db)
{
    /// <summary>
    /// Возвращает KPI за период и процент изменения к предыдущему сопоставимому периоду.
    /// </summary>
    /// <param name="range">Период, обе границы включительны.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Выручка, валовая прибыль, маржинальность, число продаж, средний чек и лучший менеджер.</returns>
    /// <remarks>
    /// Если продаж нет, суммы равны нулю, а маржинальность и средний чек — null.
    /// </remarks>
    public async Task<KpiResponse> GetKpisAsync(DateRange range, CancellationToken ct)
    {
        var previous = range.PreviousComparable();
        var current = await AggregateAsync(range, ct);
        var prev = await AggregateAsync(previous, ct);

        var best = await GetBestManagerAsync(range, ct);

        return new KpiResponse(
            new MetricValue(current.Revenue, PctChange(current.Revenue, prev.Revenue)),
            new MetricValue(current.GrossProfit, PctChange(current.GrossProfit, prev.GrossProfit)),
            current.Margin is null
                ? null
                : new MetricValue(current.Margin.Value, PctChange(current.Margin, prev.Margin)),
            new MetricValue(current.SalesCount, PctChange(current.SalesCount, prev.SalesCount)),
            current.AverageCheck is null
                ? null
                : new MetricValue(current.AverageCheck.Value, PctChange(current.AverageCheck, prev.AverageCheck)),
            best,
            new DateRangeDto(range.From, range.To),
            new DateRangeDto(previous.From, previous.To));
    }

    /// <summary>
    /// Рейтинг всех менеджеров, включая тех, у кого нет продаж за период.
    /// </summary>
    /// <param name="range">Период.</param>
    /// <param name="sort">grossProfit или averageCheck.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Строки рейтинга. При равной метрике место не пропускается (competition ranking).</returns>
    public async Task<IReadOnlyList<ManagerRankingRow>> GetManagerRankingAsync(
        DateRange range,
        string sort,
        CancellationToken ct)
    {
        var previous = range.PreviousComparable();
        var sortByAvgCheck = string.Equals(sort, "averageCheck", StringComparison.OrdinalIgnoreCase);

        var currentRows = await AggregateByManagerAsync(range, ct);
        var prevRows = await AggregateByManagerAsync(previous, ct);

        var managers = await db.Managers.AsNoTracking()
            .OrderBy(m => m.Id)
            .ToListAsync(ct);

        var joined = managers.Select(m =>
        {
            currentRows.TryGetValue(m.Id, out var cur);
            prevRows.TryGetValue(m.Id, out var prev);
            cur ??= Totals.Empty;
            prev ??= Totals.Empty;

            var sortMetric = sortByAvgCheck ? cur.AverageCheck ?? 0m : cur.GrossProfit;
            var prevMetric = sortByAvgCheck ? prev.AverageCheck : prev.GrossProfit;

            return new
            {
                Manager = m,
                Totals = cur,
                SortMetric = sortMetric,
                Change = PctChange(sortMetric, prevMetric)
            };
        })
        .OrderByDescending(x => x.SortMetric)
        .ThenByDescending(x => x.Totals.Revenue)
        .ThenBy(x => x.Manager.Id)
        .ToList();

        var result = new List<ManagerRankingRow>(joined.Count);
        var rank = 0;
        decimal? lastMetric = null;
        var position = 0;

        foreach (var row in joined)
        {
            position++;
            if (lastMetric is null || row.SortMetric != lastMetric)
            {
                rank = position;
                lastMetric = row.SortMetric;
            }

            result.Add(new ManagerRankingRow(
                rank,
                row.Manager.Id,
                row.Manager.FullName,
                row.Manager.Team,
                row.Manager.Title,
                row.Manager.Initials,
                row.Manager.AvatarColor,
                row.Manager.IsActive,
                (int)row.Totals.SalesCount,
                row.Totals.Revenue,
                row.Totals.GrossProfit,
                row.Totals.AverageCheck,
                row.Totals.Margin,
                row.Change));
        }

        return result;
    }

    /// <summary>
    /// Динамика выручки и валовой прибыли: по дням, если период не длиннее 62 дней, иначе по месяцам.
    /// </summary>
    /// <param name="range">Период.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Точки графика. Пустые дни заполняются нулями.</returns>
    public async Task<IReadOnlyList<TrendPoint>> GetTrendAsync(DateRange range, CancellationToken ct)
    {
        var useDaily = range.InclusiveDays <= 62;
        var daily = await db.Sales.AsNoTracking()
            .Where(s => s.SaleDate >= range.From && s.SaleDate <= range.To)
            .Select(s => new
            {
                s.SaleDate,
                s.Status,
                Amount = s.Items.Sum(i => i.Quantity * i.UnitPrice),
                Cost = s.Items.Sum(i => i.Quantity * i.UnitCost)
            })
            .ToListAsync(ct);

        if (useDaily)
        {
            var byDay = daily
                .GroupBy(x => x.SaleDate)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var t = Fold(g.Select(x => (x.Status, x.Amount, x.Cost)));
                    return new TrendPoint(
                        g.Key.ToString("dd.MM"),
                        g.Key,
                        g.Key,
                        t.Revenue,
                        t.GrossProfit,
                        (int)t.SalesCount);
                })
                .ToList();

            // Пустые дни заполняются нулями, чтобы линия на графике не рвалась.
            var filled = new List<TrendPoint>();
            for (var d = range.From; d <= range.To; d = d.AddDays(1))
            {
                var existing = byDay.FirstOrDefault(x => x.From == d);
                filled.Add(existing ?? new TrendPoint(d.ToString("dd.MM"), d, d, 0, 0, 0));
            }

            return filled;
        }

        var byMonth = daily
            .GroupBy(x => new { x.SaleDate.Year, x.SaleDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g =>
            {
                var from = new DateOnly(g.Key.Year, g.Key.Month, 1);
                var to = from.AddMonths(1).AddDays(-1);
                if (to > range.To) to = range.To;
                if (from < range.From) from = range.From;
                var t = Fold(g.Select(x => (x.Status, x.Amount, x.Cost)));
                return new TrendPoint(
                    $"{g.Key.Month:D2}.{g.Key.Year}",
                    from,
                    to,
                    t.Revenue,
                    t.GrossProfit,
                    (int)t.SalesCount);
            })
            .ToList();

        return byMonth;
    }

    /// <summary>
    /// Выручка, валовая прибыль и доля каждой категории за период.
    /// </summary>
    /// <param name="range">Период.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Категории по убыванию выручки.</returns>
    public async Task<IReadOnlyList<CategoryStats>> GetCategoriesAsync(DateRange range, CancellationToken ct)
    {
        var rows = await (
            from s in db.Sales.AsNoTracking()
            where s.SaleDate >= range.From && s.SaleDate <= range.To
                  && (s.Status == SaleStatuses.Paid || s.Status == SaleStatuses.Refunded)
            from i in s.Items
            group new { s.Status, i } by new { i.Product.CategoryId, i.Product.Category.Name }
            into g
            select new
            {
                g.Key.CategoryId,
                g.Key.Name,
                Revenue = g.Sum(x =>
                    x.Status == SaleStatuses.Paid
                        ? x.i.Quantity * x.i.UnitPrice
                        : -(x.i.Quantity * x.i.UnitPrice)),
                Cost = g.Sum(x =>
                    x.Status == SaleStatuses.Paid
                        ? x.i.Quantity * x.i.UnitCost
                        : -(x.i.Quantity * x.i.UnitCost))
            }).ToListAsync(ct);

        var totalRevenue = rows.Sum(x => x.Revenue);
        return rows
            .Select(x =>
            {
                var gp = x.Revenue - x.Cost;
                return new CategoryStats(
                    x.CategoryId,
                    x.Name,
                    x.Revenue,
                    gp,
                    x.Revenue == 0 ? null : Math.Round(gp / x.Revenue, 4),
                    totalRevenue == 0 ? 0 : Math.Round(x.Revenue / totalRevenue, 4));
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();
    }

    /// <summary>
    /// Товары с наибольшей валовой прибылью за период.
    /// </summary>
    /// <param name="range">Период.</param>
    /// <param name="limit">Сколько позиций вернуть.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Топ товаров по Gross Profit.</returns>
    public async Task<IReadOnlyList<ProductStats>> GetTopProductsAsync(DateRange range, int limit, CancellationToken ct)
    {
        var rows = await (
            from s in db.Sales.AsNoTracking()
            where s.SaleDate >= range.From && s.SaleDate <= range.To
                  && (s.Status == SaleStatuses.Paid || s.Status == SaleStatuses.Refunded)
            from i in s.Items
            group new { s.Status, i } by new
            {
                i.ProductId,
                i.Product.Name,
                CategoryName = i.Product.Category.Name
            }
            into g
            select new
            {
                g.Key.ProductId,
                g.Key.Name,
                g.Key.CategoryName,
                Revenue = g.Sum(x =>
                    x.Status == SaleStatuses.Paid
                        ? x.i.Quantity * x.i.UnitPrice
                        : -(x.i.Quantity * x.i.UnitPrice)),
                Cost = g.Sum(x =>
                    x.Status == SaleStatuses.Paid
                        ? x.i.Quantity * x.i.UnitCost
                        : -(x.i.Quantity * x.i.UnitCost)),
                Quantity = g.Sum(x =>
                    x.Status == SaleStatuses.Paid ? x.i.Quantity : -x.i.Quantity)
            }).ToListAsync(ct);

        return rows
            .Select(x => new ProductStats(
                x.ProductId,
                x.Name,
                x.CategoryName,
                x.Revenue,
                x.Revenue - x.Cost,
                x.Quantity))
            .OrderByDescending(x => x.GrossProfit)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Последние продажи за период, включая отмены и возвраты.
    /// </summary>
    /// <param name="range">Период.</param>
    /// <param name="limit">Максимум строк.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Продажи от новых к старым. Суммы со знаком статуса.</returns>
    public async Task<IReadOnlyList<RecentSaleDto>> GetRecentSalesAsync(DateRange range, int limit, CancellationToken ct)
    {
        var sales = await db.Sales.AsNoTracking()
            .Where(s => s.SaleDate >= range.From && s.SaleDate <= range.To)
            .OrderByDescending(s => s.SaleDate)
            .ThenByDescending(s => s.Id)
            .Take(limit)
            .Select(s => new
            {
                s.Id,
                s.SaleDate,
                s.Status,
                ManagerName = s.Manager.FullName,
                ManagerInitials = s.Manager.Initials,
                ManagerAvatarColor = s.Manager.AvatarColor,
                CustomerName = s.Customer.Name,
                CustomerCompany = s.Customer.Company,
                Products = s.Items.Select(i => i.Product.Name).ToList(),
                Amount = s.Items.Sum(i => i.Quantity * i.UnitPrice),
                Cost = s.Items.Sum(i => i.Quantity * i.UnitCost)
            })
            .ToListAsync(ct);

        return sales.Select(s =>
        {
            var signedAmount = s.Status == SaleStatuses.Refunded ? -s.Amount
                : s.Status == SaleStatuses.Cancelled ? 0m
                : s.Amount;
            var signedCost = s.Status == SaleStatuses.Refunded ? -s.Cost
                : s.Status == SaleStatuses.Cancelled ? 0m
                : s.Cost;
            return new RecentSaleDto(
                s.Id,
                s.SaleDate,
                s.Status,
                s.ManagerName,
                s.ManagerInitials,
                s.ManagerAvatarColor,
                s.CustomerName,
                s.CustomerCompany,
                s.Products,
                signedAmount,
                signedAmount - signedCost);
        }).ToList();
    }

    private async Task<BestManagerDto?> GetBestManagerAsync(DateRange range, CancellationToken ct)
    {
        var byManager = await AggregateByManagerAsync(range, ct);
        if (byManager.Count == 0)
            return null;

        var best = byManager.Values
            .OrderByDescending(x => x.GrossProfit)
            .ThenByDescending(x => x.Revenue)
            .ThenBy(x => x.ManagerId)
            .FirstOrDefault();

        if (best is null || best.GrossProfit <= 0)
            return null;

        var manager = await db.Managers.AsNoTracking().FirstAsync(m => m.Id == best.ManagerId, ct);
        return new BestManagerDto(manager.Id, manager.FullName, manager.Initials, manager.AvatarColor, best.GrossProfit);
    }

    private async Task<Totals> AggregateAsync(DateRange range, CancellationToken ct)
    {
        var rows = await db.Sales.AsNoTracking()
            .Where(s => s.SaleDate >= range.From && s.SaleDate <= range.To)
            .Select(s => new
            {
                s.Status,
                Amount = s.Items.Sum(i => i.Quantity * i.UnitPrice),
                Cost = s.Items.Sum(i => i.Quantity * i.UnitCost)
            })
            .ToListAsync(ct);

        return Fold(rows.Select(x => (x.Status, x.Amount, x.Cost)));
    }

    private async Task<Dictionary<int, Totals>> AggregateByManagerAsync(DateRange range, CancellationToken ct)
    {
        var rows = await db.Sales.AsNoTracking()
            .Where(s => s.SaleDate >= range.From && s.SaleDate <= range.To)
            .Select(s => new
            {
                s.ManagerId,
                s.Status,
                Amount = s.Items.Sum(i => i.Quantity * i.UnitPrice),
                Cost = s.Items.Sum(i => i.Quantity * i.UnitCost)
            })
            .ToListAsync(ct);

        return rows
            .GroupBy(x => x.ManagerId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var t = Fold(g.Select(x => (x.Status, x.Amount, x.Cost)));
                    return t with { ManagerId = g.Key };
                });
    }

    private static Totals Fold(IEnumerable<(string Status, decimal Amount, decimal Cost)> rows)
    {
        decimal revenue = 0, cost = 0;
        var paidCount = 0;

        foreach (var (status, amount, c) in rows)
        {
            // Refunded вычитаем из Revenue и Cost, чтобы маржинальность была сопоставима между периодами.
            // Cancelled не попадает ни в суммы, ни в счётчик оплаченных продаж.
            if (status == SaleStatuses.Paid)
            {
                revenue += amount;
                cost += c;
                paidCount++;
            }
            else if (status == SaleStatuses.Refunded)
            {
                revenue -= amount;
                cost -= c;
            }
        }

        var gp = revenue - cost;
        return new Totals(
            0,
            revenue,
            cost,
            gp,
            paidCount,
            revenue == 0 ? null : Math.Round(gp / revenue, 4),
            paidCount == 0 ? null : Math.Round(revenue / paidCount, 2));
    }

    private static decimal? PctChange(decimal? current, decimal? previous)
    {
        if (current is null || previous is null || previous == 0)
            return null;
        return Math.Round((current.Value - previous.Value) / Math.Abs(previous.Value) * 100m, 1);
    }

    private sealed record Totals(
        int ManagerId,
        decimal Revenue,
        decimal Cost,
        decimal GrossProfit,
        decimal SalesCount,
        decimal? Margin,
        decimal? AverageCheck)
    {
        public static Totals Empty { get; } = new(0, 0, 0, 0, 0, null, null);
    }
}
