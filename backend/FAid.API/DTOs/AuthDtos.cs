namespace FAid.API.DTOs;

public record LoginDto(string Username, string Password);

public record AuthResponseDto(
    string Token,
    string Username,
    string FullName,
    string Role,
    DateTime ExpiresAt
);

public record CurrentUserDto(
    Guid UserId,
    string Username,
    string FullName,
    string Role,
    string EmailAddress
);
