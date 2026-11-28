namespace Utilities.Billing.Contracts;

[GenerateSerializer]
public class GetAssetReply
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string Code { get; set; } = null!;
    [Id(2)]
    public string IssuerAccount { get; set; } = null!;
    [Id(3)]
    public string MasterAccount { get; set; } = null!;
    [Id(4)]
    public ICollection<string> ModelCodes { get; set; } = new List<string>();

}
