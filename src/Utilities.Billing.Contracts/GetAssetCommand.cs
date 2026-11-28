
namespace Utilities.Billing.Contracts;

public class GetAssetCommand
{
    public string Id { get; set; } = null!;
    public string TenantId { get; set; } = null!;
}
