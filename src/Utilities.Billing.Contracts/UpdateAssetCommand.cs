
namespace Utilities.Billing.Contracts;

public class UpdateAssetCommand
{
    public string Id { get; set; } = null!;
    public ICollection<string> ModelCodes { get; set; } = new List<string>();
    public string TenantId { get; set; } = null!;
}
