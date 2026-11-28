namespace Utilities.Billing.Contracts
{
    [GenerateSerializer]
    public class DeleteCustomerAccount
    {
        [Id(0)]
        public string CustomerAccountId { get; set; } = null!;
        [Id(1)]
        public string AssetCode { get; set; } = null!;
        [Id(2)]
        public string AssetIssuerAccountId { get; set; } = null!;
    }
}