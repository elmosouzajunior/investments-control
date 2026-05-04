namespace Omega.Invest.Domain.Entities;

public sealed class InvestmentPlan : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal? TargetTermYears { get; set; }
}
