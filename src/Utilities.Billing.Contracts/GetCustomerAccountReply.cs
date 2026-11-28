using Utilities.Billing.Data.Entities;

namespace Utilities.Billing.Contracts;

[GenerateSerializer]
public class GetCustomerAccountReply
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string Wallet { get; set; } = null!;
    [Id(2)]
    public Guid AssetId { get; set; }
    [Id(3)]
    public string AssetCode { get; set; } = null!;
    [Id(4)]
    public string AssetIssuer { get; set; } = null!;
    [Id(5)]
    public string MasterAccount { get; set; } = null!;
    public AccountState State { get; set; }
}
