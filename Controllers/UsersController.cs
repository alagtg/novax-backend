using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourProject.API.DTOs.Users;
using YourProject.API.Services;

namespace YourProject.API.Controllers;

[Authorize(Roles = "ADMIN")]
public class UsersController : BaseApiController
{
    private readonly UserService _users;

    public UsersController(UserService users) => _users = users;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _users.GetAll());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var u = await _users.GetById(id);
        return u == null ? NotFound() : Ok(u);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req) => Ok(await _users.Create(req));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest req)
    {
        var u = await _users.Update(id, req);
        return u == null ? NotFound() : Ok(u);
    }
    [HttpPut("{id}/password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] AdminChangePasswordRequest req)
    {
        var ok = await _users.ChangePassword(id, req.NewPassword);
        return ok ? Ok(new { message = "Mot de passe modifié." }) : NotFound();
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _users.Delete(id);
        return ok ? Ok(new { message = "Supprimé." }) : NotFound();
    }
}
