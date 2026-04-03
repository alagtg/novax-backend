using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace YourProject.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected int? CurrentUserId
    {
        get
        {
            var uid = User.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;
            return int.TryParse(uid, out var id) ? id : null;
        }
    }

    protected string? CurrentRole => User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
}
