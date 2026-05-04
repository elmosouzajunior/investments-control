namespace Omega.Invest.API.Contracts;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string Token,
    string FullName,
    string Email,
    string Role,
    Guid? CompanyId,
    string? CompanyName);
