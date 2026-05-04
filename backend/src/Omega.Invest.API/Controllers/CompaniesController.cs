using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omega.Invest.API.Contracts;
using Omega.Invest.Domain.Entities;
using Omega.Invest.Infrastructure.Data;

namespace Omega.Invest.API.Controllers;

[Authorize(Roles = "Master")]
[Route("api/companies")]
public sealed class CompaniesController : ApiControllerBase
{
    private readonly AppDbContext _context;

    public CompaniesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<CompanyResponse>>> GetAll()
    {
        var companies = await _context.Companies
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CompanyResponse(x.Id, x.Name, x.Cnpj, x.TradeName, x.IsActive))
            .ToListAsync();

        return Ok(companies);
    }

    [HttpPost]
    public async Task<ActionResult<CompanyResponse>> Create(CompanyRequest request)
    {
        var company = new Company
        {
            Name = request.Name.Trim(),
            TradeName = request.TradeName?.Trim(),
            Cnpj = OnlyDigits(request.Cnpj),
            IsActive = request.IsActive
        };

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new CompanyResponse(company.Id, company.Name, company.Cnpj, company.TradeName, company.IsActive));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CompanyRequest request)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company is null)
        {
            return NotFound();
        }

        company.Name = request.Name.Trim();
        company.TradeName = request.TradeName?.Trim();
        company.Cnpj = OnlyDigits(request.Cnpj);
        company.IsActive = request.IsActive;
        company.UpdatedAtUtc = DateTime.UtcNow;

        if (!company.IsActive)
        {
            var users = await _context.Users.Where(x => x.CompanyId == company.Id).ToListAsync();
            foreach (var user in users)
            {
                user.IsActive = false;
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static string OnlyDigits(string value) => new(value.Where(char.IsDigit).ToArray());
}
