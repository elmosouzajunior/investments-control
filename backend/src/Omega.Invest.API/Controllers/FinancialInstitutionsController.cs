using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omega.Invest.API.Contracts;
using Omega.Invest.Domain.Entities;
using Omega.Invest.Infrastructure.Data;

namespace Omega.Invest.API.Controllers;

[Authorize(Roles = "CompanyUser")]
[Route("api/institutions")]
public sealed class FinancialInstitutionsController : ApiControllerBase
{
    private readonly AppDbContext _context;

    public FinancialInstitutionsController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<List<InstitutionResponse>>> GetAll()
    {
        var companyId = RequiredCompanyId();
        return Ok(await _context.FinancialInstitutions
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .Select(x => new InstitutionResponse(x.Id, x.Name, x.Code, x.Notes, x.IsActive))
            .ToListAsync());
    }

    [HttpPost]
    public async Task<ActionResult<InstitutionResponse>> Create(InstitutionRequest request)
    {
        var companyId = RequiredCompanyId();
        var name = request.Name.Trim();
        if (await ExistsWithNameAsync(companyId, name))
        {
            return Conflict(new { message = "Já existe uma instituição financeira com esse nome." });
        }

        var entity = new FinancialInstitution
        {
            CompanyId = companyId,
            Name = name,
            Code = request.Code?.Trim(),
            Notes = request.Notes?.Trim(),
            IsActive = request.IsActive
        };
        _context.FinancialInstitutions.Add(entity);
        await _context.SaveChangesAsync();
        var response = new InstitutionResponse(entity.Id, entity.Name, entity.Code, entity.Notes, entity.IsActive);
        return CreatedAtAction(nameof(GetAll), new { id = entity.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, InstitutionRequest request)
    {
        var companyId = RequiredCompanyId();
        var entity = await _context.FinancialInstitutions.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId);
        if (entity is null) return NotFound();
        var name = request.Name.Trim();
        if (await ExistsWithNameAsync(companyId, name, id))
        {
            return Conflict(new { message = "Já existe uma instituição financeira com esse nome." });
        }

        entity.Name = name;
        entity.Code = request.Code?.Trim();
        entity.Notes = request.Notes?.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> ExistsWithNameAsync(Guid companyId, string name, Guid? ignoredId = null)
    {
        var normalizedName = name.ToUpper();
        return await _context.FinancialInstitutions
            .AsNoTracking()
            .AnyAsync(x =>
                x.CompanyId == companyId
                && x.Name.ToUpper() == normalizedName
                && (!ignoredId.HasValue || x.Id != ignoredId.Value));
    }
}
