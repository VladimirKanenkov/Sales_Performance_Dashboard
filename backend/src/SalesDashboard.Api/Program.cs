using Microsoft.EntityFrameworkCore;
using SalesDashboard.Api.Analytics;
using SalesDashboard.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=127.0.0.1;Port=15432;Database=sales_dashboard;Username=sales;Password=sales";

builder.Services.AddDbContext<SalesDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<AnalyticsService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    for (var attempt = 1; attempt <= 30; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            await SeedData.EnsureSeededAsync(db);
            logger.LogInformation("Database migrated and seeded");
            break;
        }
        catch (Exception ex) when (attempt < 30)
        {
            logger.LogWarning(ex, "Database not ready (attempt {Attempt}/30), retrying...", attempt);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

// Проверка готовности API и наличия менеджеров после миграции и seed.
app.MapGet("/api/health", async (SalesDbContext db, CancellationToken ct) =>
{
    var managers = await db.Managers.CountAsync(ct);
    return Results.Ok(new { status = "ok", managers });
});

// KPI за период from..to (включительно) и сравнение с предыдущим периодом той же длины.
app.MapGet("/api/analytics/kpis", async (DateOnly? from, DateOnly? to, AnalyticsService analytics, CancellationToken ct) =>
{
    if (!TryRange(from, to, out var range, out var error))
        return Results.BadRequest(new { title = error });
    return Results.Ok(await analytics.GetKpisAsync(range, ct));
});

// Рейтинг менеджеров. sort=grossProfit | averageCheck.
app.MapGet("/api/analytics/managers", async (DateOnly? from, DateOnly? to, string? sort, AnalyticsService analytics, CancellationToken ct) =>
{
    if (!TryRange(from, to, out var range, out var error))
        return Results.BadRequest(new { title = error });
    sort ??= "grossProfit";
    if (sort is not ("grossProfit" or "averageCheck"))
        return Results.BadRequest(new { title = "sort must be grossProfit or averageCheck" });
    return Results.Ok(await analytics.GetManagerRankingAsync(range, sort, ct));
});

// Динамика выручки и валовой прибыли по дням или месяцам.
app.MapGet("/api/analytics/trend", async (DateOnly? from, DateOnly? to, AnalyticsService analytics, CancellationToken ct) =>
{
    if (!TryRange(from, to, out var range, out var error))
        return Results.BadRequest(new { title = error });
    return Results.Ok(await analytics.GetTrendAsync(range, ct));
});

// Агрегаты по категориям и доля в выручке периода.
app.MapGet("/api/analytics/categories", async (DateOnly? from, DateOnly? to, AnalyticsService analytics, CancellationToken ct) =>
{
    if (!TryRange(from, to, out var range, out var error))
        return Results.BadRequest(new { title = error });
    return Results.Ok(await analytics.GetCategoriesAsync(range, ct));
});

// Топ товаров по валовой прибыли. limit по умолчанию 8, максимум 50.
app.MapGet("/api/analytics/products", async (DateOnly? from, DateOnly? to, int? limit, AnalyticsService analytics, CancellationToken ct) =>
{
    if (!TryRange(from, to, out var range, out var error))
        return Results.BadRequest(new { title = error });
    var take = Math.Clamp(limit ?? 8, 1, 50);
    return Results.Ok(await analytics.GetTopProductsAsync(range, take, ct));
});

// Последние продажи периода, включая Cancelled и Refunded. limit по умолчанию 12.
app.MapGet("/api/sales/recent", async (DateOnly? from, DateOnly? to, int? limit, AnalyticsService analytics, CancellationToken ct) =>
{
    if (!TryRange(from, to, out var range, out var error))
        return Results.BadRequest(new { title = error });
    var take = Math.Clamp(limit ?? 12, 1, 100);
    return Results.Ok(await analytics.GetRecentSalesAsync(range, take, ct));
});

app.Run();

static bool TryRange(DateOnly? from, DateOnly? to, out DateRange range, out string error)
{
    range = default!;
    error = string.Empty;
    if (from is null || to is null)
    {
        error = "Query parameters 'from' and 'to' (yyyy-MM-dd) are required";
        return false;
    }

    if (to < from)
    {
        error = "'to' must be greater than or equal to 'from'";
        return false;
    }

    range = new DateRange(from.Value, to.Value);
    return true;
}

public partial class Program;
