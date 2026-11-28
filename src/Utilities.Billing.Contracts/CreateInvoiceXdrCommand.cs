namespace Utilities.Billing.Contracts;

public class CreateInvoiceXdrCommand
{
    public string PayerAccountId { get; set; } = null!;
    public string DeviceAccountId { get; set; } = null!;
    public string AssetCode { get; set; } = null!;
    public decimal Amount { get; set; }
    public string AssetsIssuerAccountId { get; set; } = null!;
    public string TenantAccountId { get; set; } = null!;
    public long InvoiceId { get; set; }
}
