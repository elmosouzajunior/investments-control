namespace Omega.Invest.Domain.Entities;

public sealed class FinancialInstitution : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Notes { get; set; }
}
