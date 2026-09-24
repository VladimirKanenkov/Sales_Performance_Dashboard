namespace SalesDashboard.Api.Domain;

/// <summary>
/// Категория товаров каталога.
/// </summary>
public class Category
{
    /// <summary>Идентификатор категории.</summary>
    public int Id { get; set; }

    /// <summary>Название категории.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Товары категории.</summary>
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
