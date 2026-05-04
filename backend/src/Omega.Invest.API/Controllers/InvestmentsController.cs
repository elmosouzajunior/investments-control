using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omega.Invest.API.Contracts;
using Omega.Invest.Domain.Entities;
using Omega.Invest.Infrastructure.Data;

namespace Omega.Invest.API.Controllers;

[Authorize(Roles = "CompanyUser")]
[Route("api/investments")]
public sealed class InvestmentsController : ApiControllerBase
{
    private readonly AppDbContext _context;

    public InvestmentsController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<List<InvestmentResponse>>> GetAll()
    {
        var companyId = RequiredCompanyId();
        var items = await _context.Investments
            .AsNoTracking()
            .Include(x => x.Plan)
            .Include(x => x.Type)
            .Include(x => x.Institution)
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .GroupJoin(
                _context.Users.AsNoTracking(),
                investment => investment.ResponsibleUserId,
                user => user.Id,
                (investment, users) => new { investment, users })
            .SelectMany(
                x => x.users.DefaultIfEmpty(),
                (x, responsibleUser) => new InvestmentResponse(
                x.investment.Id,
                x.investment.PlanId,
                x.investment.Plan.Name,
                x.investment.TypeId,
                x.investment.Type.Name,
                x.investment.InstitutionId,
                x.investment.Institution.Name,
                x.investment.ResponsibleUserId,
                responsibleUser == null ? null : responsibleUser.FullName,
                x.investment.Name,
                x.investment.Ticker,
                x.investment.InitialAmount,
                x.investment.StartDate,
                x.investment.Notes,
                x.investment.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("responsible-users")]
    public async Task<ActionResult<List<ResponsibleUserResponse>>> GetResponsibleUsers()
    {
        var companyId = RequiredCompanyId();
        return Ok(await _context.Users
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.FullName)
            .Select(x => new ResponsibleUserResponse(x.Id, x.FullName, x.Email!, x.IsActive))
            .ToListAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvestmentResponse>> GetById(Guid id)
    {
        var companyId = RequiredCompanyId();
        var item = await _context.Investments
            .AsNoTracking()
            .Include(x => x.Plan)
            .Include(x => x.Type)
            .Include(x => x.Institution)
            .Where(x => x.CompanyId == companyId && x.Id == id)
            .GroupJoin(
                _context.Users.AsNoTracking(),
                investment => investment.ResponsibleUserId,
                user => user.Id,
                (investment, users) => new { investment, users })
            .SelectMany(
                x => x.users.DefaultIfEmpty(),
                (x, responsibleUser) => new InvestmentResponse(
                x.investment.Id,
                x.investment.PlanId,
                x.investment.Plan.Name,
                x.investment.TypeId,
                x.investment.Type.Name,
                x.investment.InstitutionId,
                x.investment.Institution.Name,
                x.investment.ResponsibleUserId,
                responsibleUser == null ? null : responsibleUser.FullName,
                x.investment.Name,
                x.investment.Ticker,
                x.investment.InitialAmount,
                x.investment.StartDate,
                x.investment.Notes,
                x.investment.IsActive))
            .FirstOrDefaultAsync();

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(InvestmentRequest request)
    {
        var companyId = RequiredCompanyId();
        if (!await RelatedDataBelongsToCompany(companyId, request.PlanId, request.TypeId, request.InstitutionId))
        {
            return BadRequest(new { message = "Plano, tipo ou instituição inválidos para a empresa." });
        }
        if (!await ResponsibleUserBelongsToCompany(companyId, request.ResponsibleUserId))
        {
            return BadRequest(new { message = "Responsável inválido para a empresa." });
        }

        var entity = new Investment
        {
            CompanyId = companyId,
            PlanId = request.PlanId,
            TypeId = request.TypeId,
            InstitutionId = request.InstitutionId,
            ResponsibleUserId = request.ResponsibleUserId,
            Name = request.Name.Trim(),
            Ticker = request.Ticker?.Trim().ToUpperInvariant(),
            InitialAmount = request.InitialAmount,
            StartDate = request.StartDate,
            Notes = request.Notes?.Trim(),
            IsActive = request.IsActive
        };
        _context.Investments.Add(entity);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, InvestmentRequest request)
    {
        var companyId = RequiredCompanyId();
        var entity = await _context.Investments.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId);
        if (entity is null) return NotFound();
        if (!await RelatedDataBelongsToCompany(companyId, request.PlanId, request.TypeId, request.InstitutionId))
        {
            return BadRequest(new { message = "Plano, tipo ou instituição inválidos para a empresa." });
        }
        if (!await ResponsibleUserBelongsToCompany(companyId, request.ResponsibleUserId))
        {
            return BadRequest(new { message = "Responsável inválido para a empresa." });
        }

        entity.PlanId = request.PlanId;
        entity.TypeId = request.TypeId;
        entity.InstitutionId = request.InstitutionId;
        entity.ResponsibleUserId = request.ResponsibleUserId;
        entity.Name = request.Name.Trim();
        entity.Ticker = request.Ticker?.Trim().ToUpperInvariant();
        entity.InitialAmount = request.InitialAmount;
        entity.StartDate = request.StartDate;
        entity.Notes = request.Notes?.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{investmentId:guid}/operations")]
    public async Task<ActionResult<List<OperationResponse>>> GetOperations(Guid investmentId)
    {
        var companyId = RequiredCompanyId();
        return Ok(await _context.InvestmentOperations
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.InvestmentId == investmentId)
            .OrderByDescending(x => x.OperationDate)
            .Select(x => new OperationResponse(x.Id, x.InvestmentId, x.Type, x.OperationDate, x.Amount, x.Description))
            .ToListAsync());
    }

    [HttpPost("{investmentId:guid}/operations")]
    public async Task<ActionResult<OperationResponse>> CreateOperation(Guid investmentId, OperationRequest request)
    {
        var companyId = RequiredCompanyId();
        if (!await _context.Investments.AnyAsync(x => x.Id == investmentId && x.CompanyId == companyId))
        {
            return NotFound();
        }

        var operation = new InvestmentOperation
        {
            CompanyId = companyId,
            InvestmentId = investmentId,
            Type = request.Type,
            OperationDate = request.OperationDate,
            Amount = request.Amount,
            Description = request.Description?.Trim()
        };
        _context.InvestmentOperations.Add(operation);
        await _context.SaveChangesAsync();
        return CreatedAtAction(
            nameof(GetOperations),
            new { investmentId },
            new OperationResponse(operation.Id, operation.InvestmentId, operation.Type, operation.OperationDate, operation.Amount, operation.Description));
    }

    [HttpPut("{investmentId:guid}/operations/{operationId:guid}")]
    public async Task<IActionResult> UpdateOperation(Guid investmentId, Guid operationId, OperationRequest request)
    {
        var companyId = RequiredCompanyId();
        var operation = await _context.InvestmentOperations
            .FirstOrDefaultAsync(x => x.Id == operationId && x.InvestmentId == investmentId && x.CompanyId == companyId);

        if (operation is null) return NotFound();

        operation.Type = request.Type;
        operation.OperationDate = request.OperationDate;
        operation.Amount = request.Amount;
        operation.Description = request.Description?.Trim();
        operation.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{investmentId:guid}/operations/{operationId:guid}")]
    public async Task<IActionResult> DeleteOperation(Guid investmentId, Guid operationId)
    {
        var companyId = RequiredCompanyId();
        var operation = await _context.InvestmentOperations
            .FirstOrDefaultAsync(x => x.Id == operationId && x.InvestmentId == investmentId && x.CompanyId == companyId);

        if (operation is null) return NotFound();

        _context.InvestmentOperations.Remove(operation);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{investmentId:guid}/measurements")]
    public async Task<ActionResult<List<MeasurementResponse>>> GetMeasurements(Guid investmentId)
    {
        var companyId = RequiredCompanyId();
        return Ok(await _context.InvestmentMonthlyMeasurements
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.InvestmentId == investmentId)
            .OrderByDescending(x => x.Month)
            .Select(x => new MeasurementResponse(x.Id, x.InvestmentId, x.Month, x.MarketValue, x.IncomeAmount, x.Notes))
            .ToListAsync());
    }

    [HttpPost("{investmentId:guid}/measurements")]
    public async Task<ActionResult<MeasurementResponse>> CreateMeasurement(Guid investmentId, MeasurementRequest request)
    {
        var companyId = RequiredCompanyId();
        if (!await _context.Investments.AnyAsync(x => x.Id == investmentId && x.CompanyId == companyId))
        {
            return NotFound();
        }

        var measurement = new InvestmentMonthlyMeasurement
        {
            CompanyId = companyId,
            InvestmentId = investmentId,
            Month = request.Month,
            MarketValue = request.MarketValue,
            IncomeAmount = request.IncomeAmount,
            Notes = request.Notes?.Trim()
        };
        _context.InvestmentMonthlyMeasurements.Add(measurement);
        await _context.SaveChangesAsync();
        return CreatedAtAction(
            nameof(GetMeasurements),
            new { investmentId },
            new MeasurementResponse(measurement.Id, measurement.InvestmentId, measurement.Month, measurement.MarketValue, measurement.IncomeAmount, measurement.Notes));
    }

    [HttpPut("{investmentId:guid}/measurements/{measurementId:guid}")]
    public async Task<IActionResult> UpdateMeasurement(Guid investmentId, Guid measurementId, MeasurementRequest request)
    {
        var companyId = RequiredCompanyId();
        var measurement = await _context.InvestmentMonthlyMeasurements
            .FirstOrDefaultAsync(x => x.Id == measurementId && x.InvestmentId == investmentId && x.CompanyId == companyId);

        if (measurement is null) return NotFound();

        measurement.Month = request.Month;
        measurement.MarketValue = request.MarketValue;
        measurement.IncomeAmount = request.IncomeAmount;
        measurement.Notes = request.Notes?.Trim();
        measurement.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{investmentId:guid}/measurements/{measurementId:guid}")]
    public async Task<IActionResult> DeleteMeasurement(Guid investmentId, Guid measurementId)
    {
        var companyId = RequiredCompanyId();
        var measurement = await _context.InvestmentMonthlyMeasurements
            .FirstOrDefaultAsync(x => x.Id == measurementId && x.InvestmentId == investmentId && x.CompanyId == companyId);

        if (measurement is null) return NotFound();

        _context.InvestmentMonthlyMeasurements.Remove(measurement);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> RelatedDataBelongsToCompany(Guid companyId, Guid planId, Guid typeId, Guid institutionId)
    {
        return await _context.InvestmentPlans.AnyAsync(x => x.Id == planId && x.CompanyId == companyId)
            && await _context.InvestmentTypes.AnyAsync(x => x.Id == typeId && x.CompanyId == companyId)
            && await _context.FinancialInstitutions.AnyAsync(x => x.Id == institutionId && x.CompanyId == companyId);
    }

    private async Task<bool> ResponsibleUserBelongsToCompany(Guid companyId, Guid? responsibleUserId)
    {
        return responsibleUserId.HasValue
            && await _context.Users.AnyAsync(x => x.Id == responsibleUserId.Value && x.CompanyId == companyId && x.IsActive);
    }
}
