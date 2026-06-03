using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Omega.Invest.API.Contracts;
using Omega.Invest.Infrastructure.Data;
using Omega.Invest.Infrastructure.Identity;

namespace Omega.Invest.API.Controllers;

[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(AppDbContext context, IConfiguration configuration, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _configuration = configuration;
        _userManager = userManager;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "E-mail ou senha inválidos." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "Usuário inativo." });
        }

        string? companyName = null;
        if (user.CompanyId.HasValue)
        {
            var company = await _context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == user.CompanyId.Value);

            if (company is null || !company.IsActive)
            {
                return Unauthorized(new { message = "Empresa inativa ou não encontrada." });
            }

            companyName = company.TradeName ?? company.Name;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "CompanyUser";
        var token = GenerateToken(user, role);

        return Ok(new LoginResponse(token, user.FullName, user.Email!, role, user.CompanyId, companyName));
    }

    private string GenerateToken(ApplicationUser user, string role)
    {
        var jwt = _configuration.GetSection("Jwt");
        var jwtKey = jwt["Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("Jwt:Key must be configured before generating tokens.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(double.Parse(jwt["DurationInMinutes"] ?? "480"));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, role),
            new("CompanyId", user.CompanyId?.ToString() ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
