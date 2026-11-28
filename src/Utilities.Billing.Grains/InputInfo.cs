
namespace Utilities.Billing.Grains
{
    [GenerateSerializer]
    public class InputInfo
    {
        [Id(0)]
        public Guid AccountId { get; set; }
        [Id(1)]
        public string DeviceSerial { get; set; } = null!;
        [Id(2)]
        public decimal CurrentValue { get; set; }
        [Id(3)]
        public string AssetIssuer { get; set; } = null!;
        [Id(4)]
        public string AssetCode { get; set; } = null!;
        [Id(5)]
        public string Wallet { get; set; } = null!;
        [Id(6)]
        public Guid AssetId { get; set; }
    }
}