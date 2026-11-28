namespace Utilities.Billing.Grains;

[GenerateSerializer]
public class TenantGrainState
{
    [Id(0)]
    public string Name { get; set; } = null!;
    [Id(1)]
    public string Wallet { get; set; } = null!;
}