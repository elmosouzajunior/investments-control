namespace Omega.Invest.Domain.Entities;

public sealed class InvestmentOperation : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid InvestmentId { get; set; }
    public Investment Investment { get; set; } = null!;
    public InvestmentOperationType Type { get; set; }
    public DateOnly OperationDate { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}
