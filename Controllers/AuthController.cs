using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourProject.API.DTOs.Auth;
using YourProject.API.Services;

namespace YourProject.API.Controllers;

public class AuthController : BaseApiController
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var res = await _auth.Login(req);
        if (res == null) return Unauthorized("Email ou mot de passe incorrect.");
        return Ok(res);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var uid = CurrentUserId;
        if (!uid.HasValue) return Unauthorized();

        var ok = await _auth.ChangePassword(uid.Value, req.OldPassword, req.NewPassword);
        if (!ok) return BadRequest("Ancien mot de passe incorrect.");
        return Ok(new { message = "Mot de passe modifié." });
    }
}
