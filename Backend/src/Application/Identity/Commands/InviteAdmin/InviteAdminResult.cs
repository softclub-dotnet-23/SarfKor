namespace Application.Identity.Commands.InviteAdmin;

public enum InviteAdminOutcome
{
    Invited,
    EmailAlreadyRegistered
}

public sealed record InviteAdminResult(InviteAdminOutcome Outcome, int? AdminInvitationId);
