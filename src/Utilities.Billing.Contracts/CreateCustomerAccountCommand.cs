
namespace Utilities.Billing.Contracts;

public class CreateCustomerAccountCommand
{
    public string AssetId { get; set; } = null!;
    public string DeviceSerial { get; set; } = null!;
    public bool CreateMuxed { get; set; }
    public string InputCode { get; set; } = null!;
    public string TenantId { get; set; } = null!;
}
