namespace Omega.Invest.Domain.Entities;

public sealed class InvestmentMonthlyMeasurement : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid InvestmentId { get; set; }
    public Investment Investment { get; set; } = null!;
    public DateOnly Month { get; set; }
    public decimal MarketValue { get; set; }
    public decimal IncomeAmount { get; set; }
    public string? Notes { get; set; }
}
