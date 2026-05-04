using Omega.Invest.Domain.Entities;

namespace Omega.Invest.API.Contracts;

public sealed record SimpleRequest(string Name, string? Description, bool IsActive = true);
public sealed record SimpleResponse(Guid Id, string Name, string? Description, bool IsActive);

public sealed record InstitutionRequest(string Name, string? Code, string? Notes, bool IsActive = true);
public sealed record InstitutionResponse(Guid Id, string Name, string? Code, string? Notes, bool IsActive);

public sealed record PlanRequest(string Name, string? Description, decimal TargetAmount, decimal? TargetTermYears, bool IsActive = true);
public sealed record PlanResponse(Guid Id, string Name, string? Description, decimal TargetAmount, decimal? TargetTermYears, bool IsActive);

public sealed record InvestmentRequest(
    Guid PlanId,
    Guid TypeId,
    Guid InstitutionId,
    Guid? ResponsibleUserId,
    string Name,
    string? Ticker,
    decimal InitialAmount,
    DateOnly StartDate,
    string? Notes,
    bool IsActive = true);

public sealed record InvestmentResponse(
    Guid Id,
    Guid PlanId,
    string PlanName,
    Guid TypeId,
    string TypeName,
    Guid InstitutionId,
    string InstitutionName,
    Guid? ResponsibleUserId,
    string? ResponsibleUserName,
    string Name,
    string? Ticker,
    decimal InitialAmount,
    DateOnly StartDate,
    string? Notes,
    bool IsActive);

public sealed record OperationRequest(InvestmentOperationType Type, DateOnly OperationDate, decimal Amount, string? Description);
public sealed record OperationResponse(Guid Id, Guid InvestmentId, InvestmentOperationType Type, DateOnly OperationDate, decimal Amount, string? Description);

public sealed record MeasurementRequest(DateOnly Month, decimal MarketValue, decimal IncomeAmount, string? Notes);
public sealed record MeasurementResponse(Guid Id, Guid InvestmentId, DateOnly Month, decimal MarketValue, decimal IncomeAmount, string? Notes);

public sealed record ResponsibleUserResponse(Guid Id, string FullName, string Email, bool IsActive);
