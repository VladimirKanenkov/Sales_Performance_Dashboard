namespace SalesDashboard.Api.Domain;

/// <summary>
/// Допустимые статусы продажи. Строковые константы совпадают со значениями в БД.
/// </summary>
public static class SaleStatuses
{
    /// <summary>Оплаченная продажа. Входит во все агрегаты как плюс.</summary>
    public const string Paid = "Paid";

    /// <summary>Отменённая продажа. Исключается из всех агрегатов.</summary>
    public const string Cancelled = "Cancelled";

    /// <summary>Возврат. Вычитается из Revenue и Cost, в число продаж не входит.</summary>
    public const string Refunded = "Refunded";
}

/// <summary>
/// Продажа: менеджер, клиент, дата и набор позиций.
/// </summary>
public class Sale
{
    /// <summary>Идентификатор продажи.</summary>
    public int Id { get; set; }

    /// <summary>Идентификатор менеджера.</summary>
    public int ManagerId { get; set; }

    /// <summary>Идентификатор клиента.</summary>
    public int CustomerId { get; set; }

    /// <summary>Дата продажи (календарный день, без времени).</summary>
    public DateOnly SaleDate { get; set; }

    /// <summary>Статус: Paid, Cancelled или Refunded.</summary>
    public string Status { get; set; } = SaleStatuses.Paid;

    /// <summary>Менеджер, оформивший продажу.</summary>
    public Manager Manager { get; set; } = null!;

    /// <summary>Клиент продажи.</summary>
    public Customer Customer { get; set; } = null!;

    /// <summary>Позиции продажи.</summary>
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}
