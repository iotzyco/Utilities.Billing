namespace Utilities.Billing.Contracts;

public class CreateInvoiceCommand
{
    public string CustomerAccountId { get; set; } = null!;
    public string PayerAccount { get; set; } = null!;
    public string Amount { get; set; } = null!;
    public string TenantId { get; set; } = null!;
}
