﻿using Cargo.Infrastructure.Data.Model.Dictionary;
using System.Collections.Generic;

namespace Cargo.Infrastructure.Data.Model.Rates
{
    public class TransportProduct
    {
        public ulong Id { get; set; }

        public string Code { get; set; }

        public string Description { get; set; }

        public string Trigger { get; set; }

        public ICollection<Shr> ShrCodes { get; set; }

        public ICollection<CarrierCharge> IncludingCarrierCharges { get; set; }

        public ICollection<CarrierCharge> ExcludingCarrierCharges { get; set; }
    }
}