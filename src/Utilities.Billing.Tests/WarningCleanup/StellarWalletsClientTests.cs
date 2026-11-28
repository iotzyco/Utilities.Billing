using Microsoft.Extensions.Options;
using Moq;
using StellarDotnetSdk.Accounts;
using Utilities.Billing.StellarWallets;

namespace Utilities.Billing.Tests.WarningCleanup;

/// <summary>
/// Pins <see cref="StellarWalletsClient.GetMasterAccountAsync"/> after the CS1998 change (async keyword dropped):
/// the result and the "exception is on the returned task" semantics must match the old async method.
/// </summary>
public class StellarWalletsClientTests
{
    private static StellarWalletsClient CreateClient(string secretSeed)
    {
        var options = new Mock<IOptionsMonitor<StellarWalletsSettings>>();
        options.Setup(x => x.CurrentValue).Returns(new StellarWalletsSettings
        {
            HorizonUrl = "http://localhost",
            SecretSeed = secretSeed,
        });

        return new StellarWalletsClient(options.Object);
    }

    [Test]
    public async Task GetMasterAccountAsync_ReturnsAccountIdDerivedFromSecretSeed()
    {
        var keyPair = KeyPair.Random();
        var client = CreateClient(keyPair.SecretSeed!);

        var accountId = await client.GetMasterAccountAsync();

        Assert.That(accountId, Is.EqualTo(keyPair.AccountId));
    }

    [Test]
    public void GetMasterAccountAsync_InvalidSeed_DoesNotThrowSynchronously_ReturnsFaultedTask()
    {
        var client = CreateClient("not-a-stellar-seed");

        Task<string>? task = null;
        Assert.DoesNotThrow(() => task = client.GetMasterAccountAsync());

        Assert.That(task, Is.Not.Null);
        Assert.That(task!.IsFaulted, Is.True);
        Assert.ThrowsAsync(Is.InstanceOf<Exception>(), async () => await task);
    }
}
