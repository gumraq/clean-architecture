using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cargo.Infrastructure.Data.Model.Settings
{
    public class CarrierSettings
    {
        public int CarrierId { get; set; }
        public Customer Carrier { get; set; }
        public int ParametersSettingsId { get; set; }
        public ParametersSettings ParametersSettings { get; set; }
        public string Value { get; set; }
    }
}
