namespace SalesDashboard.Api.Domain;

/// <summary>
/// Товар каталога, принадлежащий одной категории.
/// </summary>
public class Product
{
    /// <summary>Идентификатор товара.</summary>
    public int Id { get; set; }

    /// <summary>Идентификатор категории.</summary>
    public int CategoryId { get; set; }

    /// <summary>Название товара.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Артикул.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Бренд.</summary>
    public string Brand { get; set; } = string.Empty;

    /// <summary>Категория товара.</summary>
    public Category Category { get; set; } = null!;

    /// <summary>Позиции продаж с этим товаром.</summary>
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
