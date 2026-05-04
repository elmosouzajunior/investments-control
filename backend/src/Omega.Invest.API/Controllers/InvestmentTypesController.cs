using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omega.Invest.API.Contracts;
using Omega.Invest.Domain.Entities;
using Omega.Invest.Infrastructure.Data;

namespace Omega.Invest.API.Controllers;

[Authorize(Roles = "CompanyUser")]
[Route("api/investment-types")]
public sealed class InvestmentTypesController : ApiControllerBase
{
    private readonly AppDbContext _context;

    public InvestmentTypesController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<List<SimpleResponse>>> GetAll()
    {
        var companyId = RequiredCompanyId();
        return Ok(await _context.InvestmentTypes
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .Select(x => new SimpleResponse(x.Id, x.Name, x.Description, x.IsActive))
            .ToListAsync());
    }

    [HttpPost]
    public async Task<ActionResult<SimpleResponse>> Create(SimpleRequest request)
    {
        var entity = new InvestmentType
        {
            CompanyId = RequiredCompanyId(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive
        };
        _context.InvestmentTypes.Add(entity);
        await _context.SaveChangesAsync();
        var response = new SimpleResponse(entity.Id, entity.Name, entity.Description, entity.IsActive);
        return CreatedAtAction(nameof(GetAll), new { id = entity.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SimpleRequest request)
    {
        var companyId = RequiredCompanyId();
        var entity = await _context.InvestmentTypes.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId);
        if (entity is null) return NotFound();
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
