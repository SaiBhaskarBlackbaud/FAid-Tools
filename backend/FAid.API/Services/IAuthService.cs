using FAid.API.DTOs;

namespace FAid.API.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<CurrentUserDto?> GetCurrentUserAsync(string username);
}
