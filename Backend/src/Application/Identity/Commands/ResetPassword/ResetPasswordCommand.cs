namespace Application.Identity.Commands.ResetPassword;

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword);
