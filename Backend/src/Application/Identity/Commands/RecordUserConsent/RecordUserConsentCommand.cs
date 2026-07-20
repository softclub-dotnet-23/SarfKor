using Domain.Identity;

namespace Application.Identity.Commands.RecordUserConsent;

public sealed record RecordUserConsentCommand(string UserId, ConsentType Type, bool IsGranted);
