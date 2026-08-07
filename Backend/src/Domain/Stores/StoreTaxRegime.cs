namespace Domain.Stores;

// A store on the simplified regime (упрощённый режим) doesn't charge VAT at all, regardless of
// IsVatPayer — General is the only regime where IsVatPayer has any effect on a sale's tax calc.
public enum StoreTaxRegime
{
    General,
    Simplified
}
