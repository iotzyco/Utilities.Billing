using Utilities.Billing.Data.Entities;
using Utilities.Billing.Grains;
using BillingAsset = Utilities.Billing.Data.Entities.Asset;

namespace Utilities.Billing.Tests.WarningCleanup;

/// <summary>
/// Pins TenantGrain / DeviceGrain state behaviour touched by the nullability cleanup
/// (TenantGrain state became nullable, unused _paymentSystem removed, DeviceGrain is covered in DeviceGrainStateTests).
/// Runs on the in-process Orleans TestCluster with the in-memory database from <see cref="SiloConfigurator"/>.
/// </summary>
public class GrainStateTests : GrainsFixtureBase<SiloConfigurator>
{
    [Test]
    public async Task TenantGrain_UnknownTenant_GetStateReturnsNull()
    {
        var grain = Cluster.GrainFactory.GetGrain<ITenantGrain>(Guid.NewGuid());

        var state = await grain.GetState();

        Assert.That(state, Is.Null);
    }

    [Test]
    public async Task TenantGrain_ExistingTenant_GetStateReturnsNameAndWallet()
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "tenant-" + Guid.NewGuid(), Wallet = "G" + Guid.NewGuid().ToString("N") };
        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync();

        var state = await Cluster.GrainFactory.GetGrain<ITenantGrain>(tenant.Id).GetState();

        Assert.That(state, Is.Not.Null);
        Assert.That(state!.Name, Is.EqualTo(tenant.Name));
        Assert.That(state.Wallet, Is.EqualTo(tenant.Wallet));
    }

    [Test]
    public async Task TenantGrain_UpdateState_ReplacesState()
    {
        var grain = Cluster.GrainFactory.GetGrain<ITenantGrain>(Guid.NewGuid());

        await grain.UpdateState(new TenantGrainState { Name = "n", Wallet = "w" });
        var state = await grain.GetState();

        Assert.That(state, Is.Not.Null);
        Assert.That(state!.Name, Is.EqualTo("n"));
        Assert.That(state.Wallet, Is.EqualTo("w"));
    }
}
