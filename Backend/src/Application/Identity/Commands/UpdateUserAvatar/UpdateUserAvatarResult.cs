namespace Application.Identity.Commands.UpdateUserAvatar;

/// <summary>PreviousAvatarReference lets the caller (WebApi's multipart upload endpoint, which owns
/// all file-system I/O per the same pattern ReceiptsController already uses) delete the old stored
/// file only after the new reference is safely committed, instead of risking an orphaned row
/// pointing at nothing if the delete happened first and the save then failed.</summary>
public sealed record UpdateUserAvatarResult(int UserProfileId, string? PreviousAvatarReference);
