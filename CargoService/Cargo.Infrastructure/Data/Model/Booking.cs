﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Cargo.Infrastructure.Data.Model
{
    public class Booking
    {
        public int Id { get; set; }
        public int AwbId { get; set; }
        public Awb Awb { get; set; }
        public string QuanDetShipmentDescriptionCode { get; set; }
        public int NumberOfPieces { get; set; }
        public string WeightCode { get; set; }
        public decimal Weight { get; set; }
        public string VolumeCode { get; set; }
        public decimal VolumeAmount { get; set; }
        public DateTime CreatedDate { get; set; }
        /// <summary>
        /// KK, LL, NN
        /// </summary>
        public string SpaceAllocationCode { get; set; }

        public ulong FlightScheduleId { get; set; }
        public FlightShedule FlightSchedule { get; set; }

        /// <summary>
        /// Маршруты
        /// </summary>
        public int? PrevRoutingId { get; set; }
        public Booking PrevRouting { get; set; }
        public ICollection<Booking> NextRoutings { get; set; }
    }
}
