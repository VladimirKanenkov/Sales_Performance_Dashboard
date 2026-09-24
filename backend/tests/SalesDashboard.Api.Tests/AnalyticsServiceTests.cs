using Microsoft.EntityFrameworkCore;
using SalesDashboard.Api.Analytics;
using SalesDashboard.Api.Data;
using SalesDashboard.Api.Domain;

namespace SalesDashboard.Api.Tests;

/// <summary>
/// Проверяет учёт Paid, Refunded и Cancelled, границы периода и ничью в рейтинге.
/// </summary>
public class AnalyticsServiceTests : IAsyncLifetime
{
    private SalesDbContext _db = null!;
    private AnalyticsService _sut = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new SalesDbContext(options);
        await SeedFixtureAsync(_db);
        _sut = new AnalyticsService(_db);
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    /// <summary>
    /// Paid входит в суммы, Refunded вычитается, Cancelled не влияет на агрегаты.
    /// </summary>
    [Fact]
    public async Task Kpis_Paid_Refunded_Cancelled_AreHandledCorrectly()
    {
        // В феврале только продажи менеджера «Статусы».
        var range = new DateRange(new DateOnly(2025, 2, 1), new DateOnly(2025, 2, 28));
        var kpis = await _sut.GetKpisAsync(range, CancellationToken.None);

        // Paid 1000/400, Refunded вычитает 200/80, Cancelled не входит в агрегаты.
        Assert.Equal(800m, kpis.Revenue.Value);
        Assert.Equal(480m, kpis.GrossProfit.Value);
        Assert.Equal(1m, kpis.SalesCount.Value);
        Assert.Equal(800m, kpis.AverageCheck!.Value);
        Assert.Equal(0.6m, kpis.Margin!.Value);
    }

    /// <summary>
    /// Дата на границе периода входит в выборку, пустой диапазон даёт нули.
    /// </summary>
    [Fact]
    public async Task Period_Filter_ExcludesOutsideDates_IncludesBoundaries()
    {
        var onBoundary = await _sut.GetKpisAsync(
            new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 1)),
            CancellationToken.None);
        Assert.Equal(1000m, onBoundary.Revenue.Value);

        var empty = await _sut.GetKpisAsync(
            new DateRange(new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 30)),
            CancellationToken.None);
        Assert.Equal(0m, empty.Revenue.Value);
        Assert.Equal(0m, empty.SalesCount.Value);
    }

    /// <summary>
    /// Менеджер без продаж остаётся в рейтинге, равная прибыль даёт одно место.
    /// </summary>
    [Fact]
    public async Task Ranking_IncludesManagersWithoutSales_AndHandlesTies()
    {
        var range = new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));
        var ranking = await _sut.GetManagerRankingAsync(range, "grossProfit", CancellationToken.None);

        Assert.Contains(ranking, r => r.FullName == "Без Продаж" && r.SalesCount == 0 && r.Revenue == 0);

        var tied = ranking.Where(r => r.FullName is "Альфа Один" or "Альфа Два").ToList();
        Assert.Equal(2, tied.Count);
        Assert.Equal(tied[0].GrossProfit, tied[1].GrossProfit);
        Assert.Equal(tied[0].Rank, tied[1].Rank);
    }

    private static async Task SeedFixtureAsync(SalesDbContext db)
    {
        var m1 = new Manager { FullName = "Альфа Один", Team = "A", Title = "Менеджер", Initials = "АО", AvatarColor = "#111" };
        var m2 = new Manager { FullName = "Альфа Два", Team = "A", Title = "Менеджер", Initials = "АД", AvatarColor = "#222" };
        var m3 = new Manager { FullName = "Без Продаж", Team = "B", Title = "Менеджер", Initials = "БП", AvatarColor = "#333" };
        var mStatus = new Manager { FullName = "Статусы", Team = "C", Title = "Менеджер", Initials = "СТ", AvatarColor = "#444" };
        var customer = new Customer { Name = "Клиент", Company = "Компания", Segment = "SME" };
        var category = new Category { Name = "Кат" };
        var product = new Product { Name = "Товар", Sku = "S1", Brand = "B", Category = category };

        db.AddRange(m1, m2, m3, mStatus, customer, category, product);
        await db.SaveChangesAsync();

        // Одинаковые январские сделки дают ничью в рейтинге.
        db.Sales.Add(new Sale
        {
            Manager = m1,
            Customer = customer,
            SaleDate = new DateOnly(2025, 1, 1),
            Status = SaleStatuses.Paid,
            Items = { new SaleItem { Product = product, Quantity = 1, UnitPrice = 1000m, UnitCost = 400m } }
        });
        db.Sales.Add(new Sale
        {
            Manager = m2,
            Customer = customer,
            SaleDate = new DateOnly(2025, 1, 2),
            Status = SaleStatuses.Paid,
            Items = { new SaleItem { Product = product, Quantity = 1, UnitPrice = 1000m, UnitCost = 400m } }
        });

        // Февраль: Paid, Refunded и Cancelled для проверки агрегатов.
        db.Sales.Add(new Sale
        {
            Manager = mStatus,
            Customer = customer,
            SaleDate = new DateOnly(2025, 2, 5),
            Status = SaleStatuses.Paid,
            Items = { new SaleItem { Product = product, Quantity = 1, UnitPrice = 1000m, UnitCost = 400m } }
        });
        db.Sales.Add(new Sale
        {
            Manager = mStatus,
            Customer = customer,
            SaleDate = new DateOnly(2025, 2, 6),
            Status = SaleStatuses.Refunded,
            Items = { new SaleItem { Product = product, Quantity = 1, UnitPrice = 200m, UnitCost = 80m } }
        });
        db.Sales.Add(new Sale
        {
            Manager = mStatus,
            Customer = customer,
            SaleDate = new DateOnly(2025, 2, 7),
            Status = SaleStatuses.Cancelled,
            Items = { new SaleItem { Product = product, Quantity = 5, UnitPrice = 999m, UnitCost = 1m } }
        });

        await db.SaveChangesAsync();
    }
}
