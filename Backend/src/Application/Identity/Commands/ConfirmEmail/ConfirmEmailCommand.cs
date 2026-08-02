namespace Application.Identity.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(string Email, string Code);
