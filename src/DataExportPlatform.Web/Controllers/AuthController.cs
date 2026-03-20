using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataExportPlatform.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpGet("whoami")]
    public IActionResult WhoAmI() =>
        Ok(new { username = User.Identity?.Name ?? string.Empty });
}
