namespace Omega.Invest.Domain.Entities;

public sealed class Investment : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid PlanId { get; set; }
    public InvestmentPlan Plan { get; set; } = null!;
    public Guid TypeId { get; set; }
    public InvestmentType Type { get; set; } = null!;
    public Guid InstitutionId { get; set; }
    public FinancialInstitution Institution { get; set; } = null!;
    public Guid? ResponsibleUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Ticker { get; set; }
    public decimal InitialAmount { get; set; }
    public DateOnly StartDate { get; set; }
    public string? Notes { get; set; }
}
