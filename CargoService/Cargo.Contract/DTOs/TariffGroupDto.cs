using System.Collections.Generic;

namespace Cargo.Contract.DTOs
{
    /// <summary>
    /// Справочник тарифных групп
    /// </summary>
    public class TariffGroupDto
    {
        public int Id { get; set; }

        /// <summary>
        /// Код тарифной группы 
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// Описание (рус.)
        /// </summary>
        public string DescriptionRus { get; set; }
        /// <summary>
        /// Описание (англ.)
        /// </summary>
        public string DescriptionEng { get; set; }

        /// <summary>
        /// Аэропорты
        /// </summary>
        public List<IataLocationDto> Airports { get; set; }
    }
}
