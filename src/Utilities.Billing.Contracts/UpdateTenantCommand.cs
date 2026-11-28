namespace Utilities.Billing.Contracts
{
    public class UpdateTenantCommand
    {
        public string Name { get; set; } = null!;
        public string Account { get; set; } = null!;
        public string TenantId { get; set; } = null!;
    }
}