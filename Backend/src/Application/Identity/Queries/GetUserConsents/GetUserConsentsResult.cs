using Domain.Identity;

namespace Application.Identity.Queries.GetUserConsents;

public sealed record UserConsentDto(ConsentType Type, bool IsGranted, DateTimeOffset RecordedAt);

public sealed record GetUserConsentsResult(IReadOnlyList<UserConsentDto> Consents);
