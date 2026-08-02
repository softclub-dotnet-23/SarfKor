namespace Application.Stores.Commands.AdminCreateStorePartner;

public enum AdminCreateStorePartnerOutcome
{
    Invited,
    EmailAlreadyRegistered
}

public sealed record AdminCreateStorePartnerResult(AdminCreateStorePartnerOutcome Outcome, int? InvitationId);
