namespace Omega.Invest.Domain.Entities;

public sealed class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string? TradeName { get; set; }
}
