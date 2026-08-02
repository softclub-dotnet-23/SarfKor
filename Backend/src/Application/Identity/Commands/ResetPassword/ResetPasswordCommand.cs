namespace Application.Identity.Commands.ResetPassword;

public sealed record ResetPasswordCommand(string Email, string Code, string NewPassword);
