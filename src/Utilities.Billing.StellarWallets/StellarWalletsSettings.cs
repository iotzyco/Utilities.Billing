namespace Utilities.Billing.StellarWallets;

public class StellarWalletsSettings
{
    public static string SectionName = nameof(StellarWalletsSettings);
    public string HorizonUrl { get; set; } = null!;
    public string SecretSeed { get; set; } = null!;
    public bool UseTestnet { get; set; }
}