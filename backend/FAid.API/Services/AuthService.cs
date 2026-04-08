using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FAid.API.Data;
using FAid.API.DTOs;

namespace FAid.API.Services;

public class AuthService : IAuthService
{
    private readonly FAidDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(FAidDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username && u.IsActive);
        if (user == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) return null;

        user.LastLoginDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user.Username, user.Role);
        var expiresAt = DateTime.UtcNow.AddHours(8);

        return new AuthResponseDto(token, user.Username, user.FullName, user.Role, expiresAt);
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        if (user == null) return null;
        return new CurrentUserDto(user.UserId, user.Username, user.FullName, user.Role, user.EmailAddress);
    }

    private string GenerateJwtToken(string username, string role)
    {
        var jwtKey = _config["Jwt:Key"] ?? "FAid-Super-Secret-Key-Replace-In-Production-2024";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "FAid.API",
            audience: _config["Jwt:Audience"] ?? "FAid.Client",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
