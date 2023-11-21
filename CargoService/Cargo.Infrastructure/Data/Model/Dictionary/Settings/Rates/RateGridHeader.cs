using System.Collections.Generic;

namespace Cargo.Infrastructure.Data.Model.Rates
{
	public class RateGridHeader
	{
		public ulong Id {get;set;}

		public string Code {get;set;}

		public virtual List<RateGridRank> Ranks {get;set;}

        public virtual List<TariffSolution> TariffSolutions { get; set; }
	}
}
