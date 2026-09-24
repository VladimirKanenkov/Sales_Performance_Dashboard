using Microsoft.EntityFrameworkCore;
using SalesDashboard.Api.Domain;

namespace SalesDashboard.Api.Data;

/// <summary>
/// Детерминированный генератор демо-данных за последние 12 месяцев.
/// Повторный запуск ничего не делает, если менеджеры уже есть.
/// </summary>
public static class SeedData
{
    /// <summary>Фиксированное зерно генератора, чтобы набор продаж не менялся между запусками.</summary>
    public const int RandomSeed = 42;

    private static readonly string[] FirstNames =
    [
        "Александр", "Мария", "Дмитрий", "Анна", "Сергей", "Елена", "Иван", "Ольга",
        "Андрей", "Наталья", "Павел", "Екатерина", "Никита", "Ирина", "Максим",
        "Татьяна", "Артём", "Юлия", "Владимир", "Светлана", "Роман", "Дарья"
    ];

    private static readonly string[] LastNames =
    [
        "Иванов", "Смирнов", "Кузнецов", "Попов", "Васильев", "Петров", "Соколов",
        "Михайлов", "Новиков", "Фёдоров", "Морозов", "Волков", "Алексеев", "Лебедев",
        "Семёнов", "Егоров", "Павлов", "Козлов", "Степанов", "Николаев"
    ];

    private static readonly string[] Teams = ["Север", "Юг", "Центр", "Восток", "Запад"];
    private static readonly string[] Titles = ["Менеджер по продажам", "Старший менеджер", "Key Account Manager"];
    private static readonly string[] Segments = ["SME", "Enterprise", "Retail", "Startup"];
    private static readonly string[] Companies =
    [
        "ТехноСфера", "АльфаТрейд", "СеверСтрой", "Орион", "ГлобалСофт", "МеталлПром",
        "АгроЛайн", "РитейлПлюс", "ИнноВейв", "СитиМаркет", "БайкалТранс", "НеваЛогистик",
        "УралКом", "ВолгаСервис", "ЛучСвет", "ПрофиОфис", "ДанТек", "МирОкон",
        "ЭкоДом", "ФармаЛайн", "КаргоЭкспресс", "СмартХоум", "АртПринт", "КофеДень"
    ];

    // BasePrice — типичная цена, Margin — целевая доля валовой прибыли в цене.
    private static readonly (string Category, string[] Products, decimal BasePrice, decimal Margin)[] Catalog =
    [
        ("Ноутбуки", ["Ноутбук Pro 14", "Ноутбук Air 13", "Ноутбук Gaming 16", "Ноутбук Business 15", "Ноутбук Ultra 17", "Ноутбук Compact 12"], 65000m, 0.18m),
        ("Смартфоны", ["Смартфон X1", "Смартфон X2 Pro", "Смартфон Lite", "Смартфон Ultra", "Смартфон Mini", "Смартфон Fold"], 28000m, 0.22m),
        ("Мониторы", ["Монитор 24\" IPS", "Монитор 27\" 4K", "Монитор 32\" Curved", "Монитор Office 22\"", "Монитор Pro 34\""], 18000m, 0.25m),
        ("Периферия", ["Клавиатура Mech", "Мышь Pro", "Гарнитура HD", "Веб-камера 1080p", "Док-станция USB-C", "Коврик XL"], 3500m, 0.35m),
        ("Серверы", ["Сервер Rack 1U", "Сервер Tower", "СХД NAS 8TB", "Сервер Edge Mini", "Блок питания UPS"], 120000m, 0.12m),
        ("ПО и лицензии", ["Офисный пакет", "Антивирус Pro", "CRM лицензия", "CAD подписка", "Backup Suite"], 9000m, 0.55m),
        ("Сетевое оборудование", ["Коммутатор 24p", "Роутер Wi-Fi 6", "Точка доступа", "Межсетевой экран", "Кабель Cat6 100м"], 15000m, 0.28m),
        ("Принтеры", ["Лазерный A4", "МФУ Цветной", "Струйный Photo", "Этикеточный", "Сканер документный"], 22000m, 0.20m)
    ];

    private static readonly string[] AvatarColors =
    [
        "#2563eb", "#7c3aed", "#db2777", "#dc2626", "#ea580c",
        "#ca8a04", "#16a34a", "#0d9488", "#0891b2", "#4f46e5"
    ];

    /// <summary>
    /// Заполняет базу менеджерами, клиентами, каталогом и продажами, если она пуста.
    /// </summary>
    /// <param name="db">Контекст базы.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Задача заполнения.</returns>
    public static async Task EnsureSeededAsync(SalesDbContext db, CancellationToken ct = default)
    {
        if (await db.Managers.AnyAsync(ct))
            return;

        var moscow = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Russian Standard Time" : "Europe/Moscow");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, moscow));
        var start = today.AddMonths(-12);

        var rng = new Random(RandomSeed);

        var managers = CreateManagers(rng);
        var customers = CreateCustomers(rng);
        var (categories, products) = CreateCatalog();

        db.Managers.AddRange(managers);
        db.Customers.AddRange(customers);
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync(ct);

        db.Products.AddRange(products);
        await db.SaveChangesAsync(ct);

        var sales = CreateSales(rng, managers, customers, products, start, today);
        db.Sales.AddRange(sales);
        await db.SaveChangesAsync(ct);
    }

    private static List<Manager> CreateManagers(Random rng)
    {
        var used = new HashSet<string>();
        var list = new List<Manager>(20);
        for (var i = 0; i < 20; i++)
        {
            string fullName;
            do
            {
                var first = FirstNames[rng.Next(FirstNames.Length)];
                var last = LastNames[rng.Next(LastNames.Length)];
                // Женские имена получают фамилию с окончанием «а» / «ая».
                if (first is "Мария" or "Анна" or "Елена" or "Ольга" or "Наталья" or "Екатерина" or "Ирина" or "Татьяна" or "Юлия" or "Светлана" or "Дарья")
                    last = Feminize(last);
                fullName = $"{first} {last}";
            } while (!used.Add(fullName));

            var parts = fullName.Split(' ');
            list.Add(new Manager
            {
                FullName = fullName,
                Team = Teams[i % Teams.Length],
                Title = Titles[rng.Next(Titles.Length)],
                IsActive = i != 18, // один неактивный менеджер остаётся в рейтинге
                Initials = $"{parts[0][0]}{parts[1][0]}",
                AvatarColor = AvatarColors[i % AvatarColors.Length]
            });
        }

        return list;
    }

    private static string Feminize(string last)
    {
        if (last.EndsWith("ов") || last.EndsWith("ев") || last.EndsWith("ёв") || last.EndsWith("ин"))
            return last + "а";
        if (last.EndsWith("кий"))
            return last[..^2] + "ая";
        return last;
    }

    private static List<Customer> CreateCustomers(Random rng)
    {
        var list = new List<Customer>(80);
        for (var i = 0; i < 80; i++)
        {
            var first = FirstNames[rng.Next(FirstNames.Length)];
            var last = LastNames[rng.Next(LastNames.Length)];
            list.Add(new Customer
            {
                Name = $"{first} {last}",
                Company = $"{Companies[i % Companies.Length]} {(i / Companies.Length) + 1}",
                Segment = Segments[rng.Next(Segments.Length)]
            });
        }

        return list;
    }

    private static (List<Category> categories, List<Product> products) CreateCatalog()
    {
        var categories = new List<Category>();
        var products = new List<Product>();
        var sku = 1000;

        foreach (var (catName, names, _, _) in Catalog)
        {
            var cat = new Category { Name = catName };
            categories.Add(cat);
            foreach (var name in names)
            {
                products.Add(new Product
                {
                    Category = cat,
                    Name = name,
                    Sku = $"SKU-{sku++}",
                    Brand = catName switch
                    {
                        "Ноутбуки" or "Смартфоны" => "NovaTech",
                        "Серверы" or "Сетевое оборудование" => "InfraCore",
                        "ПО и лицензии" => "SoftLine",
                        _ => "OfficeGear"
                    }
                });
            }
        }

        return (categories, products);
    }

    private static List<Sale> CreateSales(
        Random rng,
        List<Manager> managers,
        List<Customer> customers,
        List<Product> products,
        DateOnly start,
        DateOnly today)
    {
        // Веса силы менеджеров: трое лидеров, середняки и аутсайдеры.
        var strength = managers.Select((_, i) => i switch
        {
            < 3 => 2.4,
            < 8 => 1.4,
            < 14 => 1.0,
            < 18 => 0.55,
            _ => 0.25
        }).ToArray();

        // Последний менеджер без продаж около 45 дней в середине окна.
        var gapManagerIndex = 19;
        var gapFrom = start.AddMonths(5);
        var gapTo = gapFrom.AddDays(45);

        var catalogMeta = products.Select(p =>
        {
            var meta = Catalog.First(c => c.Products.Contains(p.Name));
            return (Product: p, meta.BasePrice, meta.Margin);
        }).ToArray();

        var sales = new List<Sale>(3600);
        var dayCount = today.DayNumber - start.DayNumber + 1;

        for (var dayOffset = 0; dayOffset < dayCount; dayOffset++)
        {
            var date = start.AddDays(dayOffset);
            // Сезонность: пик в ноябре–декабре и марте/сентябре, спад в январе–феврале и летом.
            var monthFactor = date.Month switch
            {
                11 or 12 => 1.55,
                3 or 9 => 1.25,
                1 or 2 => 0.65,
                7 or 8 => 0.8,
                _ => 1.0
            };

            // 9.2 сделки в день в среднем даёт порядка 3500 продаж за год с учётом сезонности.
            var dailyTarget = (int)Math.Round(9.2 * monthFactor);
            for (var n = 0; n < dailyTarget; n++)
            {
                var managerIndex = PickWeighted(rng, strength);
                if (managerIndex == gapManagerIndex && date >= gapFrom && date <= gapTo)
                    continue;

                var statusRoll = rng.NextDouble();
                // Около 7% отмен и 5% возвратов, остальные — оплаченные.
                var status = statusRoll switch
                {
                    < 0.07 => SaleStatuses.Cancelled,
                    < 0.12 => SaleStatuses.Refunded,
                    _ => SaleStatuses.Paid
                };

                var sale = new Sale
                {
                    Manager = managers[managerIndex],
                    Customer = customers[rng.Next(customers.Count)],
                    SaleDate = date,
                    Status = status
                };

                var itemCount = rng.NextDouble() < 0.15 ? rng.Next(3, 6) : rng.Next(1, 3);
                // 12% сделок — мелкие (цена × 0.15), чтобы средний чек не был плоским.
                var tiny = rng.NextDouble() < 0.12;

                for (var i = 0; i < itemCount; i++)
                {
                    var (product, basePrice, margin) = catalogMeta[rng.Next(catalogMeta.Length)];
                    var priceJitter = 0.9m + (decimal)rng.NextDouble() * 0.25m;
                    var unitPrice = Math.Round(basePrice * priceJitter, 2);
                    if (tiny)
                        unitPrice = Math.Round(unitPrice * 0.15m, 2);

                    var costRatio = 1m - margin + ((decimal)rng.NextDouble() * 0.06m - 0.03m);
                    var unitCost = Math.Round(unitPrice * Math.Clamp(costRatio, 0.35m, 0.92m), 2);
                    var qty = tiny ? 1 : rng.Next(1, 5);

                    sale.Items.Add(new SaleItem
                    {
                        Product = product,
                        Quantity = qty,
                        UnitPrice = unitPrice,
                        UnitCost = unitCost
                    });
                }

                sales.Add(sale);
            }
        }

        // Одна крупная оплаченная сделка ближе к концу окна — выброс для топа продуктов.
        var bigManager = managers[0];
        var bigProduct = catalogMeta.OrderByDescending(x => x.BasePrice).First();
        sales.Add(new Sale
        {
            Manager = bigManager,
            Customer = customers[0],
            SaleDate = today.AddDays(-12),
            Status = SaleStatuses.Paid,
            Items =
            {
                new SaleItem
                {
                    Product = bigProduct.Product,
                    Quantity = 40,
                    UnitPrice = bigProduct.BasePrice * 1.1m,
                    UnitCost = Math.Round(bigProduct.BasePrice * 1.1m * (1m - bigProduct.Margin), 2)
                }
            }
        });

        // Продажи ровно на границах окна: первый день периода и сегодня.
        sales.Add(new Sale
        {
            Manager = managers[1],
            Customer = customers[1],
            SaleDate = start,
            Status = SaleStatuses.Paid,
            Items =
            {
                new SaleItem
                {
                    Product = products[0],
                    Quantity = 1,
                    UnitPrice = 10000m,
                    UnitCost = 7000m
                }
            }
        });
        sales.Add(new Sale
        {
            Manager = managers[2],
            Customer = customers[2],
            SaleDate = today,
            Status = SaleStatuses.Paid,
            Items =
            {
                new SaleItem
                {
                    Product = products[1],
                    Quantity = 2,
                    UnitPrice = 5000m,
                    UnitCost = 3200m
                }
            }
        });

        return sales;
    }

    private static int PickWeighted(Random rng, double[] weights)
    {
        var total = weights.Sum();
        var roll = rng.NextDouble() * total;
        var acc = 0.0;
        for (var i = 0; i < weights.Length; i++)
        {
            acc += weights[i];
            if (roll <= acc)
                return i;
        }

        return weights.Length - 1;
    }
}
