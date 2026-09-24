namespace SalesDashboard.Api.Domain;

/// <summary>
/// Позиция продажи: товар, количество, цена и себестоимость единицы.
/// </summary>
public class SaleItem
{
    /// <summary>Идентификатор позиции.</summary>
    public int Id { get; set; }

    /// <summary>Идентификатор продажи.</summary>
    public int SaleId { get; set; }

    /// <summary>Идентификатор товара.</summary>
    public int ProductId { get; set; }

    /// <summary>Количество единиц.</summary>
    public int Quantity { get; set; }

    /// <summary>Цена продажи за единицу.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Себестоимость за единицу.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>Продажа, к которой относится позиция.</summary>
    public Sale Sale { get; set; } = null!;

    /// <summary>Товар позиции.</summary>
    public Product Product { get; set; } = null!;
}
