namespace Application.Identity.Commands.Login;

public sealed record LoginCommand(string Email, string Password, string? IpAddress, string? UserAgent);
