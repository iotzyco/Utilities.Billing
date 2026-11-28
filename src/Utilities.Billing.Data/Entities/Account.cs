using Utilities.Common.Data;

namespace Utilities.Billing.Data.Entities;

public class Account : DbEntityWithGuidKey
{
    public Guid TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;
    
    //public long AccountHolderId { get; set; }
    //public virtual AccountHolder AccountHolder { get; set; }

    //public long AccountTypeId { get; set; }
    //public virtual AccountType AccountType { get; set; }

    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public string Wallet { get; set; } = null!;

    public string DeviceSerial { get; set; } = null!;
    public string InputCode { get; set; } = null!;

    public AccountState State { get; set; }
    
    public virtual ICollection<Invoice> Invoices { get; set; } = new HashSet<Invoice>();
    public virtual ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
}