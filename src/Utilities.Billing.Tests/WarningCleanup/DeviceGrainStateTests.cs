using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orleans.TestingHost;
using Utilities.Billing.Contracts;
using Utilities.Billing.Data;
using Utilities.Billing.Data.Entities;
using Utilities.Billing.Grains;
using BillingAsset = Utilities.Billing.Data.Entities.Asset;

namespace Utilities.Billing.Tests.WarningCleanup;

/// <summary>
/// Pins DeviceGrain input-state behaviour (its <c>_inputStates</c> field got <c>= null!</c>).
/// The activation query (SelectMany + DefaultIfEmpty) can't be translated by the EF in-memory provider,
/// so this fixture runs the in-process Orleans TestCluster over an in-memory SQLite database.
/// </summary>
public class DeviceGrainStateTests
{
    private static SqliteConnection? _connection;
    private static BillingDbContext? _dbContext;
    private TestCluster _cluster = null!;

    private static BillingDbContext DbContext => _dbContext!;

    public class SqliteSiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton(DbContext);
                services.AddSingleton(Mock.Of<IPaymentSystem>());
            });
        }
    }

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbContext = new BillingDbContext(new DbContextOptionsBuilder<BillingDbContext>().UseSqlite(_connection).Options);
        _dbContext.Database.EnsureCreated();

        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<SqliteSiloConfigurator>();
        _cluster = builder.Build();
        await _cluster.DeployAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _cluster.StopAllSilosAsync();
        await _cluster.DisposeAsync();
        await _dbContext!.DisposeAsync();
        await _connection!.DisposeAsync();
    }

    [Test]
    public async Task DeviceGrain_LoadsInputStatesOnActivation_AndSupportsUpdateAndDelete()
    {
        var serial = "serial-" + Guid.NewGuid();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "tenant", Wallet = "GTENANT" };
        var asset = new BillingAsset { Id = Guid.NewGuid(), TenantId = tenant.Id, Tenant = tenant, Code = "KWH", Issuer = "GISSUER" };
        var account = new Account
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Tenant = tenant,
            AssetId = asset.Id,
            Asset = asset,
            Wallet = "GWALLET",
            DeviceSerial = serial,
            InputCode = "in1",
            State = AccountState.Ok,
        };
        DbContext.Tenants.Add(tenant);
        DbContext.Assets.Add(asset);
        DbContext.Accounts.Add(account);
        DbContext.Payments.Add(new Payment { AccountId = account.Id, Account = account, AssetId = asset.Id, Asset = asset, Amount = 1.5M });
        DbContext.Payments.Add(new Payment { AccountId = account.Id, Account = account, AssetId = asset.Id, Asset = asset, Amount = 2M });
        await DbContext.SaveChangesAsync();

        var grain = _cluster.GrainFactory.GetGrain<IDeviceGrain>(serial);

        var info = await grain.GetInputState("in1");
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.AccountId, Is.EqualTo(account.Id));
        Assert.That(info.Wallet, Is.EqualTo("GWALLET"));
        Assert.That(info.DeviceSerial, Is.EqualTo(serial));
        Assert.That(info.AssetId, Is.EqualTo(asset.Id));
        Assert.That(info.AssetCode, Is.EqualTo("KWH"));
        Assert.That(info.AssetIssuer, Is.EqualTo("GISSUER"));
        Assert.That(info.CurrentValue, Is.EqualTo(3.5M));

        Assert.That(await grain.GetInputState("unknown"), Is.Null);

        info.CurrentValue = 10M;
        await grain.UpdateInputState("in1", info);
        Assert.That((await grain.GetInputState("in1"))!.CurrentValue, Is.EqualTo(10M));

        await grain.DeleteInputState("in1");
        Assert.That(await grain.GetInputState("in1"), Is.Null);
    }
}
