using AutoMapper;
using IDeal.Common.Components;
using IDeal.Common.Components.Messages.ObjectStructures.Fsas;
using IDeal.Common.Messaging.Messages;
using Cargo.Infrastructure.Data;
using Cargo.Infrastructure.Data.Model;
using Cargo.Infrastructure.Data.Model.Settings;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cargo.Application.Services;

namespace Cargo.Application.CommandHandlers
{
    public class ProcessFsuConsumer : IConsumer<ProcessFsu>
    {
        private IMapper mapper;
        private CargoContext dbContext;
        private ILogger<ProcessFsuConsumer> logger;
        private AwbService awbService;


        public ProcessFsuConsumer(ILogger<ProcessFsuConsumer> logger, IMapper mapper, CargoContext dbContext, AwbService awbService)
        {
            this.logger = logger;
            this.dbContext = dbContext;
            this.mapper = mapper;
            this.awbService = awbService;
        }

        public async Task Consume(ConsumeContext<ProcessFsu> context)
        {
            List<ConsignmentStatus> statuses = new List<ConsignmentStatus>();
            Fsu fsu = context.Message.Fsu;
            string acPrefic = fsu.ConsignmentDetail.AwbIdentification.AirlinePrefix;
            string serialNumber = fsu.ConsignmentDetail.AwbIdentification.AwbSerialNumber;
            Customer carrier = this.dbContext.Carriers.FirstOrDefault(c => c.IataCode == "SU");

            if (carrier.AcPrefix != acPrefic)
            {
                logger.LogError($"Нельзя обрабатывать накладные с префиксом {acPrefic}");
                return;
            }

            Awb originAwb = this.dbContext.Awbs.Include(a=>a.Tracking).FirstOrDefault(a => a.AcPrefix == acPrefic && a.SerialNumber == serialNumber && EF.Functions.DateDiffMonth(a.CreatedDate, DateTime.UtcNow) < 6);
            if (originAwb == null)
            {
                try
                {
                    Awb awb = this.mapper.Map<Awb>(fsu);
                    await this.awbService.SaveAwb(awb, StatusAwb.Draft.Value);
                    originAwb = awb;
                }
                catch
                {
                    return;
                }
            }

            if (fsu.StatusDetailsBkd != null)
            {
                List<ConsignmentStatus> sts = this.mapper.Map<List<ConsignmentStatus>>(fsu.StatusDetailsBkd);
                statuses.AddRange(sts);
            }
            if (fsu.StatusDetailsFoh != null)
            {
                List<ConsignmentStatus> sts = this.mapper.Map<List<ConsignmentStatus>>(fsu.StatusDetailsFoh);
                statuses.AddRange(sts);
            }
            if (fsu.StatusDetailsRcs != null)
            {
                List<ConsignmentStatus> sts = this.mapper.Map<List<ConsignmentStatus>>(fsu.StatusDetailsRcs);
                statuses.AddRange(sts);
            }
            if (fsu.StatusDetailsMan != null)
            {
                List<ConsignmentStatus> sts = this.mapper.Map<List<ConsignmentStatus>>(fsu.StatusDetailsMan);
                statuses.AddRange(sts);
            }
            if (fsu.StatusDetailsDep != null)
            {
                List<ConsignmentStatus> sts = this.mapper.Map<List<ConsignmentStatus>>(fsu.StatusDetailsDep);
                statuses.AddRange(sts);
            }
            if (fsu.StatusDetailsArr != null)
            {
                List<ConsignmentStatus> sts = this.mapper.Map<List<ConsignmentStatus>>(fsu.StatusDetailsArr);
                statuses.AddRange(sts);
            }
            if (fsu.StatusDetailsRcf != null)
            {
                List<ConsignmentStatus> sts = this.mapper.Map<List<ConsignmentStatus>>(fsu.StatusDetailsRcf);
                statuses.AddRange(sts);
            }
            if (fsu.StatusDetailsAwr != null)
            {
                List<ConsignmentStatus> sts = this.mapper.Map<List<ConsignmentStatus>>(fsu.StatusDetailsAwr);
                statuses.AddRange(sts);
            }
            if (fsu.StatusDetailsNfd != null)
            {
                List<ConsignmentStatus> sts = this.mapper.Map<List<ConsignmentStatus>>(fsu.StatusDetailsNfd);
                statuses.AddRange(sts);
            }
            if (fsu.StatusDetailsAwd != null)
            {
                List<ConsignmentStatus> sts = this.mapper.Map<List<ConsignmentStatus>>(fsu.StatusDetailsAwd);
                statuses.AddRange(sts);
            }
            if (fsu.StatusDetailsDlv != null)
            {
                List<ConsignmentStatus> sts = this.mapper.Map<List<ConsignmentStatus>>(fsu.StatusDetailsDlv);
                statuses.AddRange(sts);
            }

            foreach (var status in statuses)
            {
                originAwb.Tracking.Add(status);
            }

            this.dbContext.SaveChanges();
        }
    }
}
