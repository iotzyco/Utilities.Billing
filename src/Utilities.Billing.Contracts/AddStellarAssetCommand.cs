namespace Utilities.Billing.Contracts;

public class AddStellarAssetCommand
{
    public string AssetCode { get; set; } = null!;
    public string IssuerAccountId { get; set; } = null!;
    public string ReceiverAccountId { get; set; } = null!;
}
