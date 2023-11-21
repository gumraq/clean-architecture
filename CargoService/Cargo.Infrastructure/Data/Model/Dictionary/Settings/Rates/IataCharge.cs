namespace Cargo.Infrastructure.Data.Model.Rates
{
    public class IataCharge
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public string Category { get; set; }
        public string DescriptionEng { get; set; }
        public string DescriptionRus { get; set; }
    }
}