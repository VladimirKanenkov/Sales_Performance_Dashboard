namespace SalesDashboard.Api.Domain;

/// <summary>
/// Клиент, на которого оформляются продажи.
/// </summary>
public class Customer
{
    /// <summary>Идентификатор клиента.</summary>
    public int Id { get; set; }

    /// <summary>Имя контактного лица.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Компания клиента.</summary>
    public string Company { get; set; } = string.Empty;

    /// <summary>Сегмент: SME, Enterprise, Retail или Startup.</summary>
    public string Segment { get; set; } = string.Empty;

    /// <summary>Продажи клиента.</summary>
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
