using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omega.Invest.API.Contracts;
using Omega.Invest.Infrastructure.Data;
using Omega.Invest.Infrastructure.Identity;

namespace Omega.Invest.API.Controllers;

[Authorize(Roles = "Master")]
[Route("api/users")]
public sealed class CompanyUsersController : ApiControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CompanyUsersController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<List<CompanyUserResponse>>> GetAll()
    {
        var users = await (
            from user in _context.Users.AsNoTracking()
            join company in _context.Companies.AsNoTracking() on user.CompanyId equals company.Id
            where user.CompanyId != null
            orderby company.Name, user.FullName
            select new CompanyUserResponse(user.Id, company.Id, company.Name, user.FullName, user.Email!, user.IsActive))
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CompanyUserRequest request)
    {
        var company = await _context.Companies.FindAsync(request.CompanyId);
        if (company is null)
        {
            return BadRequest(new { message = "Empresa não encontrada." });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true,
            CompanyId = request.CompanyId,
            FullName = request.FullName.Trim(),
            IsActive = request.IsActive
        };

        var initialPassword = string.IsNullOrWhiteSpace(request.Password) ? "User@123" : request.Password;
        var result = await _userManager.CreateAsync(user, initialPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = string.Join("; ", result.Errors.Select(x => x.Description)) });
        }

        await _userManager.AddToRoleAsync(user, "CompanyUser");
        return CreatedAtAction(nameof(GetAll), new { id = user.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CompanyUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        user.CompanyId = request.CompanyId;
        user.FullName = request.FullName.Trim();
        user.Email = request.Email.Trim();
        user.UserName = request.Email.Trim();
        user.IsActive = request.IsActive;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = string.Join("; ", result.Errors.Select(x => x.Description)) });
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.Password);
            if (!passwordResult.Succeeded)
            {
                return BadRequest(new { message = string.Join("; ", passwordResult.Errors.Select(x => x.Description)) });
            }
        }

        return NoContent();
    }
}
