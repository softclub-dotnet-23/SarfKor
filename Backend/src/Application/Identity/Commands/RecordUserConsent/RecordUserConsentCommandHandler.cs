using Application.Abstractions;
using Application.Common;
using Domain.Identity;

namespace Application.Identity.Commands.RecordUserConsent;

public sealed class RecordUserConsentCommandHandler(
    IUserConsentRepository userConsentRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RecordUserConsentCommand, RecordUserConsentResult>
{
    public async Task<RecordUserConsentResult> Handle(RecordUserConsentCommand command, CancellationToken cancellationToken)
    {
        var consent = await userConsentRepository.GetByUserIdAndTypeAsync(command.UserId, command.Type, cancellationToken);
        if (consent is null)
        {
            consent = new UserConsent
            {
                UserId = command.UserId,
                Type = command.Type,
                IsGranted = command.IsGranted,
                RecordedAt = DateTimeOffset.UtcNow
            };
            userConsentRepository.Add(consent);
        }
        else
        {
            consent.IsGranted = command.IsGranted;
            consent.RecordedAt = DateTimeOffset.UtcNow;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RecordUserConsentResult(consent.Id);
    }
}
