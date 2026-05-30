using Merkatto.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Merkatto.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Administrator")]
public sealed class UsersController(UserService users) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<UserListItem>> List(CancellationToken ct) => users.ListAsync(ct);

    [HttpPost]
    public async Task<ActionResult> Create(CreateUserRequest request, CancellationToken ct)
    {
        var id = await users.CreateAsync(request, ct);
        return CreatedAtAction(nameof(List), new { id }, new { id });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdateUserRequest request, CancellationToken ct)
    {
        await users.UpdateAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("{id:long}/reset-password")]
    public async Task<IActionResult> ResetPassword(long id, ResetPasswordRequest request, CancellationToken ct)
    {
        await users.ResetPasswordAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        await users.DeactivateAsync(id, ct);
        return NoContent();
    }
}
