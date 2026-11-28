using System.Reflection;
using Utilities.Billing.Contracts;
using Utilities.Billing.Data.Entities;
using Utilities.Billing.Grains;
using Utilities.Billing.StellarWallets;

namespace Utilities.Billing.Tests.WarningCleanup;

/// <summary>
/// The nullability cleanup added <c>= null!</c> initializers to entities, DTOs, grain state and settings.
/// <c>null!</c> is still <c>null</c> at run time, so a freshly created object must look exactly as before:
/// every non-collection reference property is <c>null</c> and collections are still pre-created.
/// </summary>
public class NullDefaultsTests
{
    private static readonly Type[] TouchedTypes =
    {
        // Data entities
        typeof(Account), typeof(AccountHolder), typeof(AccountType), typeof(Utilities.Billing.Data.Entities.Asset),
        typeof(EquipmentModel), typeof(ExchangeRate), typeof(Invoice), typeof(Payment), typeof(Tenant),
        // Contracts
        typeof(AccountTypeItem), typeof(AddAccountTypeCommand), typeof(AddAssetCommand), typeof(AddPaymentCommand),
        typeof(AddStellarAssetCommand), typeof(AddTenantCommand), typeof(CreateCustomerAccountCommand),
        typeof(CreateInvoiceCommand), typeof(CreateInvoiceReply), typeof(CreateInvoiceXdrCommand),
        typeof(DeleteCustomerAccount), typeof(DeleteCustomerAccountCommand), typeof(GetAssetCommand),
        typeof(GetAssetReply), typeof(GetCustomerAccountCommand), typeof(GetCustomerAccountReply),
        typeof(ListAssetsCommand), typeof(AssetItem), typeof(ListCustomerAccountsCommand),
        typeof(AccountItem), typeof(ListInvoicesCommand),
        typeof(ListInvoicesReply.InvoiceItem), typeof(MakePaymentCommand), typeof(UpdateAssetCommand),
        typeof(UpdateTenantCommand),
        // Grains and settings
        typeof(InputInfo), typeof(TenantGrainState), typeof(StellarWalletsSettings),
    };

    private static IEnumerable<TestCaseData> Cases() =>
        TouchedTypes.Select(t => new TestCaseData(t).SetArgDisplayNames(t.FullName!.Replace("Utilities.Billing.", "")));

    [TestCaseSource(nameof(Cases))]
    public void NewInstance_HasNullReferenceProperties_AndPreCreatedCollections(Type type)
    {
        var instance = Activator.CreateInstance(type)!;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.PropertyType.IsValueType || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var value = property.GetValue(instance);
            var isCollection = property.PropertyType != typeof(string)
                && typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType);

            if (isCollection)
            {
                Assert.That(value, Is.Not.Null, $"{type.Name}.{property.Name} collection must still be pre-created");
            }
            else
            {
                Assert.That(value, Is.Null, $"{type.Name}.{property.Name} must still default to null");
            }
        }
    }
}
