namespace Omega.Invest.Domain.Entities;

public sealed class InvestmentType : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
