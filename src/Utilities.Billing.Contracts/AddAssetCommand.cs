
namespace Utilities.Billing.Contracts;

public class AddAssetCommand
{
    public string AssetCode { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public ICollection<string> ModelCodes { get; set; } = new List<string>();
    public string TenantId { get; set; } = null!;
}
