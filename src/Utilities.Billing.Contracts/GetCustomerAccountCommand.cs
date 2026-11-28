namespace Utilities.Billing.Contracts;

public class GetCustomerAccountCommand
{
    public string CustomerAccountId { get; set; } = null!;
    public string TenantId { get; set; } = null!;
}
