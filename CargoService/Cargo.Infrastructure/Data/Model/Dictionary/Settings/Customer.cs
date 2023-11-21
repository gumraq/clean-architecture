using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cargo.Infrastructure.Data.Model.Settings
{
    public class Customer
    {
        public int Id { get; set; }
        public string IataCode { get; set; }
        public string AcPrefix { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public virtual ICollection<CarrierSettings> CarrierSettings { get; set; }
    }
}
