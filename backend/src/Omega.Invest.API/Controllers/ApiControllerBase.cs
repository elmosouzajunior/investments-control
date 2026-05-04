using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Omega.Invest.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected bool IsMaster => User.IsInRole("Master");

    protected Guid? CurrentCompanyId
    {
        get
        {
            var value = User.FindFirstValue("CompanyId");
            return Guid.TryParse(value, out var companyId) ? companyId : null;
        }
    }

    protected Guid RequiredCompanyId()
    {
        var companyId = CurrentCompanyId;
        if (companyId is null)
        {
            throw new UnauthorizedAccessException("Empresa não encontrada no token.");
        }

        return companyId.Value;
    }
}
