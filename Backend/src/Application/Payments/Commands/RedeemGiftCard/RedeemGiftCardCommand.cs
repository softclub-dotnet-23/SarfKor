namespace Application.Payments.Commands.RedeemGiftCard;

public sealed record RedeemGiftCardCommand(string Code, decimal Amount);
