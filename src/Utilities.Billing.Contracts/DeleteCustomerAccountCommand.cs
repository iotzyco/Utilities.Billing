namespace Utilities.Billing.Contracts
{
    public class DeleteCustomerAccountCommand
    {
        public string CustomerAccountId { get; set; } = null!;
        public string TenantId { get; set; } = null!;
    }
}