namespace Omega.Invest.API.Contracts;

public sealed record CompanyRequest(string Name, string Cnpj, string? TradeName, bool IsActive = true);
public sealed record CompanyResponse(Guid Id, string Name, string Cnpj, string? TradeName, bool IsActive);

public sealed record CompanyUserRequest(
    Guid CompanyId,
    string FullName,
    string Email,
    string? Password,
    bool IsActive = true);

public sealed record CompanyUserResponse(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string FullName,
    string Email,
    bool IsActive);
