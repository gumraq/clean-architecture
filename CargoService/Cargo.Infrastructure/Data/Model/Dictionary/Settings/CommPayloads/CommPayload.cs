namespace Cargo.Infrastructure.Data.Model.Settings.CommPayloads
{
    public class CommPayload
    {
        /// <summary>
        /// Identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Weight
        /// </summary>
        public decimal Weight { get; set; }

        /// <summary>
        /// Volume
        /// </summary>
        public decimal Volume { get; set; }

        public CommPayloadRule4AicraftType CommPayloadRule4AicraftType { get; set; }
        public CommPayloadRule4Carrier CommPayloadRule4Carrier { get; set; }
        public CommPayloadRule4Route CommPayloadRule4Route { get; set; }
        public CommPayloadRule4Flight CommPayloadRule4Flight { get; set; }
    }
}
