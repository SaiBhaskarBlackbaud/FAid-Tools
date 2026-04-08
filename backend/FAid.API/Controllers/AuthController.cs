using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FAid.API.DTOs;
using FAid.API.Services;

namespace FAid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service) => _service = service;

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        var result = await _service.LoginAsync(dto);
        return result == null ? Unauthorized(new { message = "Invalid credentials" }) : Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("current-user")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> GetCurrentUser()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (username == null) return Unauthorized();
        var user = await _service.GetCurrentUserAsync(username);
        return user == null ? NotFound() : Ok(user);
    }
}
