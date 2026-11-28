
using Utilities.Billing.Data.Entities;

namespace Utilities.Billing.Contracts
{
    [GenerateSerializer]
    public class AccountItem
    {
        [Id(0)]
        public Guid Id { get; set; }
        [Id(1)]
        public string Wallet { get; set; } = null!;
        [Id(2)]
        public string AssetCode { get; set; } = null!;
        [Id(3)]
        public string DeviceSerial { get; set; } = null!;
        [Id(4)]
        public string InputCode { get; set; } = null!;
        [Id(5)]
        public AccountState State { get; set; }
    }

}