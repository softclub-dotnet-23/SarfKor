namespace Application.Sales.Commands.ProcessReturn;

public sealed record ProcessReturnLineInput(int SaleLineItemId, int Quantity);

public sealed record ProcessReturnCommand(
    int SaleTransactionId,
    IReadOnlyList<ProcessReturnLineInput> Lines,
    string Reason,
    string PerformedByUserId);
