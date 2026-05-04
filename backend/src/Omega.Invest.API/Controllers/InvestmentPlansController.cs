using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omega.Invest.API.Contracts;
using Omega.Invest.Domain.Entities;
using Omega.Invest.Infrastructure.Data;

namespace Omega.Invest.API.Controllers;

[Authorize(Roles = "CompanyUser")]
[Route("api/plans")]
public sealed class InvestmentPlansController : ApiControllerBase
{
    private readonly AppDbContext _context;

    public InvestmentPlansController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<List<PlanResponse>>> GetAll()
    {
        var companyId = RequiredCompanyId();
        return Ok(await _context.InvestmentPlans
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .Select(x => new PlanResponse(x.Id, x.Name, x.Description, x.TargetAmount, x.TargetTermYears, x.IsActive))
            .ToListAsync());
    }

    [HttpPost]
    public async Task<ActionResult<PlanResponse>> Create(PlanRequest request)
    {
        if (!IsValidPlanProjection(request))
        {
            return BadRequest(new { message = "Informe taxa projetada entre 0,00% e 100,00% e prazo entre 0 e 30 anos." });
        }

        var plan = new InvestmentPlan
        {
            CompanyId = RequiredCompanyId(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            TargetAmount = request.TargetAmount,
            TargetTermYears = request.TargetTermYears,
            IsActive = request.IsActive
        };
        _context.InvestmentPlans.Add(plan);
        await _context.SaveChangesAsync();
        var response = new PlanResponse(plan.Id, plan.Name, plan.Description, plan.TargetAmount, plan.TargetTermYears, plan.IsActive);
        return CreatedAtAction(nameof(GetAll), new { id = plan.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, PlanRequest request)
    {
        var companyId = RequiredCompanyId();
        var plan = await _context.InvestmentPlans.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId);
        if (plan is null) return NotFound();
        if (!IsValidPlanProjection(request))
        {
            return BadRequest(new { message = "Informe taxa projetada entre 0,00% e 100,00% e prazo entre 0 e 30 anos." });
        }

        plan.Name = request.Name.Trim();
        plan.Description = request.Description?.Trim();
        plan.TargetAmount = request.TargetAmount;
        plan.TargetTermYears = request.TargetTermYears;
        plan.IsActive = request.IsActive;
        plan.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static bool IsValidPlanProjection(PlanRequest request)
    {
        return request.TargetAmount is >= 0 and <= 100
            && (!request.TargetTermYears.HasValue || request.TargetTermYears.Value is >= 0 and <= 30);
    }
}
