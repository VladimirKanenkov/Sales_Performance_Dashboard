namespace SalesDashboard.Api.Domain;

/// <summary>
/// Менеджер по продажам. Неактивные менеджеры остаются в рейтинге.
/// </summary>
public class Manager
{
    /// <summary>Идентификатор менеджера.</summary>
    public int Id { get; set; }

    /// <summary>Полное имя.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Команда.</summary>
    public string Team { get; set; } = string.Empty;

    /// <summary>Должность.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Признак действующего менеджера.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Инициалы для аватара.</summary>
    public string Initials { get; set; } = string.Empty;

    /// <summary>Цвет аватара в формате #RRGGBB.</summary>
    public string AvatarColor { get; set; } = "#64748b";

    /// <summary>Продажи менеджера.</summary>
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
