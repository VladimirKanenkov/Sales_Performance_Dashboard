using SalesDashboard.Api.Analytics;

namespace SalesDashboard.Api.Tests;

/// <summary>
/// Предыдущий период имеет ту же длину и заканчивается за день до текущего.
/// </summary>
public class DateRangeTests
{
    /// <summary>
    /// Предыдущий период той же длины заканчивается за день до начала текущего.
    /// </summary>
    [Fact]
    public void PreviousComparable_SameLength()
    {
        var range = new DateRange(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 16));
        var prev = range.PreviousComparable();
        Assert.Equal(7, range.InclusiveDays);
        Assert.Equal(7, prev.InclusiveDays);
        Assert.Equal(new DateOnly(2025, 3, 3), prev.From);
        Assert.Equal(new DateOnly(2025, 3, 9), prev.To);
    }
}
