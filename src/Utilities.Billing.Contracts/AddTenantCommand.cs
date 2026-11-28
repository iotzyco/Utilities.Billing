
namespace Utilities.Billing.Contracts
{
    public class AddTenantCommand
    {
        public string Name { get; set; } = null!;
        public string Account { get; set; } = null!;
    }
}