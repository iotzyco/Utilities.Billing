using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Common.Data;

namespace Utilities.Billing.Data.Entities
{
    public class Asset : DbEntityWithGuidKey
    {
        public Guid TenantId { get; set; }
        public virtual Tenant Tenant { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public virtual ICollection<EquipmentModel> EquipmentModels { get; set; } = new HashSet<EquipmentModel>();

    }
}
