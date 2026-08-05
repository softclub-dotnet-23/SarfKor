using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Stores;

namespace Application.Stores.Commands.UpdateStoreTaxSettings;

// Admin-only (ADMIN_PROMPT.md §2.8: a store picks a tax rate from the Admin catalog, but the
// VAT-payer flag/regime that governs whether a rate applies at all is Admin-controlled too — a
// store can't self-declare its way out of tax it owes).
public sealed class UpdateStoreTaxSettingsCommandHandler(
    IStoreRepository storeRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateStoreTaxSettingsCommand, UpdateStoreTaxSettingsResult>
{
    public async Task<UpdateStoreTaxSettingsResult> Handle(UpdateStoreTaxSettingsCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new UpdateStoreTaxSettingsResult(UpdateStoreTaxSettingsOutcome.NotFound);

        var before = $$"""{"isVatPayer":{{store.IsVatPayer.ToString().ToLowerInvariant()}},"taxRegime":"{{store.TaxRegime}}"}""";
        store.IsVatPayer = command.IsVatPayer;
        store.TaxRegime = command.TaxRegime;
        var after = $$"""{"isVatPayer":{{command.IsVatPayer.ToString().ToLowerInvariant()}},"taxRegime":"{{command.TaxRegime}}"}""";

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "Store.TaxSettingsUpdated",
            EntityType = nameof(Store),
            EntityId = store.Id,
            IpAddress = command.PerformedByIpAddress,
            BeforeStateJson = before,
            AfterStateJson = after,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new UpdateStoreTaxSettingsResult(UpdateStoreTaxSettingsOutcome.Updated);
    }
}
