namespace Utilities.Billing.Contracts;

public class AddPaymentCommand
{
    public string RecieverAccountId { get; set; } = null!;
    public string AssetCode { get; set; } = null!;
    public string AssetIssuerAccountId { get; set; } = null!;
    public decimal Amount { get; set; }
}
