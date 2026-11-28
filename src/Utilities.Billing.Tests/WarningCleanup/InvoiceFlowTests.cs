using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Utilities.Billing.Api.GrpcServices;
using ListInvoicesRequest = Utilities.Billing.Api.Protos.ListInvoicesRequest;
using Utilities.Billing.Api.Services;
using Utilities.Billing.Contracts;
using Utilities.Billing.Data.Entities;
using BillingAsset = Utilities.Billing.Data.Entities.Asset;

namespace Utilities.Billing.Tests.WarningCleanup;

/// <summary>
/// Pins the invoice paths touched by the nullability cleanup:
/// <list type="bullet">
/// <item><see cref="TenantService.CreateInvoice"/> passes the tenant grain's wallet to the payment system (<c>tenantState!</c>, no IL change).</item>
/// <item>An unknown tenant fails on the customer-account tenant check before the tenant state is read.</item>
/// <item><see cref="ListInvoicesReply.InvoiceItem.Xdr"/> is now <c>string?</c>: an invoice without XDR is listed with a null XDR,
/// and the gRPC mapping still fails for it (outlined under "Needs decision").</item>
/// </list>
/// </summary>
public class InvoiceFlowTests : GrainsFixtureBase<SiloConfigurator>
{
    private async Task<(Tenant Tenant, Account Account)> SeedAsync()
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "tenant", Wallet = "GTENANT" + Guid.NewGuid().ToString("N") };
        var asset = new BillingAsset { Id = Guid.NewGuid(), TenantId = tenant.Id, Code = "KWH", Issuer = "GISSUER" };
        var account = new Account
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            AssetId = asset.Id,
            Asset = asset,
            Wallet = "GDEVICE",
            DeviceSerial = "serial-" + Guid.NewGuid(),
            InputCode = "in1",
            State = AccountState.Ok,
        };
        DbContext.Tenants.Add(tenant);
        DbContext.Assets.Add(asset);
        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync();
        return (tenant, account);
    }

    private TenantService CreateService(IPaymentSystem paymentSystem) =>
        new(DbContext, paymentSystem, Cluster.GrainFactory, Mock.Of<IServiceProvider>(), NullLogger<TenantService>.Instance);

    [Test]
    public async Task CreateInvoice_PassesTenantWalletAndAccountDataToPaymentSystem()
    {
        var (tenant, account) = await SeedAsync();
        CreateInvoiceXdrCommand? xdrCommand = null;
        var paymentSystem = new Mock<IPaymentSystem>();
        paymentSystem.Setup(x => x.GetMasterAccountAsync()).ReturnsAsync("GMASTER");
        paymentSystem.Setup(x => x.CreateInvoiceXdr(It.IsAny<CreateInvoiceXdrCommand>()))
            .Callback<CreateInvoiceXdrCommand>(c => xdrCommand = c)
            .ReturnsAsync("XDR-BASE64");

        var reply = await CreateService(paymentSystem.Object).CreateInvoice(new CreateInvoiceCommand
        {
            TenantId = tenant.Id.ToString(),
            CustomerAccountId = account.Id.ToString(),
            PayerAccount = "GPAYER",
            Amount = "12.5",
        });

        Assert.That(reply.Xdr, Is.EqualTo("XDR-BASE64"));
        Assert.That(xdrCommand, Is.Not.Null);
        Assert.That(xdrCommand!.TenantAccountId, Is.EqualTo(tenant.Wallet));
        Assert.That(xdrCommand.DeviceAccountId, Is.EqualTo("GDEVICE"));
        Assert.That(xdrCommand.PayerAccountId, Is.EqualTo("GPAYER"));
        Assert.That(xdrCommand.AssetCode, Is.EqualTo("KWH"));
        Assert.That(xdrCommand.AssetsIssuerAccountId, Is.EqualTo("GISSUER"));
        Assert.That(xdrCommand.Amount, Is.EqualTo(12.5M));

        var invoice = DbContext.Invoices.Single(x => x.Id == xdrCommand.InvoiceId);
        Assert.That(invoice.Xdr, Is.EqualTo("XDR-BASE64"));
        Assert.That(invoice.Status, Is.EqualTo(InvoiceStatus.Pending));
    }

    [Test]
    public async Task CreateInvoice_UnknownTenant_FailsOnAccountTenantCheck_BeforeTenantStateIsUsed()
    {
        var (_, account) = await SeedAsync();
        var paymentSystem = new Mock<IPaymentSystem>();

        var ex = Assert.ThrowsAsync<Utilities.Common.OperationException>(() => CreateService(paymentSystem.Object).CreateInvoice(new CreateInvoiceCommand
        {
            TenantId = Guid.NewGuid().ToString(),
            CustomerAccountId = account.Id.ToString(),
            PayerAccount = "GPAYER",
            Amount = "1",
        }));

        Assert.That(ex!.Message, Does.Contain("belongs another Tenant"));
        paymentSystem.Verify(x => x.CreateInvoiceXdr(It.IsAny<CreateInvoiceXdrCommand>()), Times.Never);
    }

    [Test]
    public async Task ListInvoices_InvoiceWithoutXdr_IsListedWithNullXdr()
    {
        var (tenant, account) = await SeedAsync();
        DbContext.Invoices.Add(new Invoice { AccountId = account.Id, Account = account, Amount = 3M, PayerWallet = "GPAYER", Status = InvoiceStatus.New, Created = DateTime.UtcNow });
        await DbContext.SaveChangesAsync();

        var reply = await CreateService(Mock.Of<IPaymentSystem>()).ListInvoices(new ListInvoicesCommand
        {
            TenantId = tenant.Id.ToString(),
            CustomerAccountId = account.Id.ToString(),
            PeriodFrom = 0,
            PeriodTo = 253402300799,
        });

        var item = reply.Items.Single();
        Assert.That(item.Xdr, Is.Null);
        Assert.That(item.Amount, Is.EqualTo("3"));
        Assert.That(item.Processed, Is.False);
    }

    [Test]
    public void StellarServiceListInvoices_ItemWithNullXdr_StillThrows()
    {
        var reply = new ListInvoicesReply();
        reply.Items.Add(new ListInvoicesReply.InvoiceItem { TransactionId = "1", Amount = "3", Xdr = null, Processed = false });
        var tenantService = new Mock<ITenantService>();
        tenantService.Setup(x => x.ListInvoices(It.IsAny<ListInvoicesCommand>())).ReturnsAsync(reply);

        var service = new StellarService(tenantService.Object);

        Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.ListInvoices(new ListInvoicesRequest { TenantId = "t", CustomerAccountId = "a" }, Mock.Of<ServerCallContext>()));
    }
}
