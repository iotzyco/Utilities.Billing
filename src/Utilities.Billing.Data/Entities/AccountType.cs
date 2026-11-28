using Utilities.Common.Data;

namespace Utilities.Billing.Data.Entities;

public class AccountType : DbEntityWithLongKey
{
    public Guid TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Token { get; set; } = null!;
    public string Wallet { get; set; } = null!;
    public virtual ICollection<Account> Accounts { get; set; } = new HashSet<Account>();
    public virtual ICollection<ExchangeRate> ExchangeRates { get; set; } = new HashSet<ExchangeRate>();

}