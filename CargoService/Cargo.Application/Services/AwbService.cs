using AutoMapper;
using IdealResults;
using IDeal.Common.Components;
using IDeal.Common.Components.Messages.ObjectStructures.Fsas;
using IDeal.Common.Components.Messages.ObjectStructures.Fwbs.Ver17;
using IDeal.Common.Messaging.Histories;
using IDeal.Common.Messaging.Messages;
using Cargo.Application;
using Cargo.Infrastructure.Data;
using Cargo.Infrastructure.Data.Model;
using Cargo.Infrastructure.Data.Model.Rates;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cargo.Application.Services
{
    public class AwbService
    {
        Regex awbPattern;
        CargoContext dbContext;
        IMapper mapper;
        IPublishEndpoint endpoint;
        ILogger<AwbService> logger;
        SettingsService commPayloadService;


        public AwbService(CargoContext CargoContext, IMapper mapper, IPublishEndpoint endpoint, ILogger<AwbService> logger, SettingsService commPayloaderService)
        {
            awbPattern = new Regex(@"\b(?<acPrefix>\d{3})-(?<serialNumber>\d{8})\b");

            this.dbContext = CargoContext;
            this.mapper = mapper;
            this.endpoint = endpoint;
            this.logger = logger;
            commPayloadService = commPayloaderService;
        }

        public async Task<Result<Awb>> Awb(int? awbId = null, string awbIdentifier = null)
        {
            if (!string.IsNullOrEmpty(awbIdentifier))
            {
                Match m = AwbIdentifierParse(awbIdentifier);
                if (m.Success)
                {
                    int id = this.dbContext.Awbs
                        .AsNoTracking()
                        .Where(il => il.AcPrefix == m.Groups["acPrefix"].Value && il.SerialNumber == m.Groups["serialNumber"].Value)
                        .Select(awb => awb.Id)
                        .FirstOrDefault();
                    if (id == 0)
                    {
                        return Result.Invalid($"Накладная не найдена: awbIdentifier = {awbIdentifier}");
                    }
                    awbId ??= id;
                }
            }

            Awb awb = this.dbContext.Awbs
             .AsNoTracking()
             .Include(a => a.Bookings)
             .ThenInclude(b => b.FlightSchedule)
             .Include(a => a.BookingRcs)
             //.ThenInclude(b => b.FlightSchedule)
             .Include(a => a.Agent)
             .ThenInclude(c => c.SalesAgent)
             .Include(a => a.Consignee)
             .Include(a => a.Consignor)
             .Include(a => a.SizeGroups)
             .Include(a => a.Prepaid)
             .Include(a => a.Collect)
             .Include(a => a.Messages)
             .Include(a => a.OtherCharges)
             //.OrderByDescending(m=>m.)
             .FirstOrDefault(il => awbId.HasValue ? il.Id == awbId : true);
            ;
            if (awb == null)
            {
                return Result.Invalid($"Накладная не найдена: awbId = {awbId}");
            }
            awb.Messages = awb.Messages.OrderByDescending(m => m.DateCreated).ToList();

            return await Task.FromResult(Result.Ok(awb));
        }

        public Result<Awb> TrackedAwb(int? awbId=null)
        {
            try
            {
                if (awbId == null)
                {
                    Awb awb = new Awb();
                    this.dbContext.Add(awb);
                    return Result.Ok(awb);
                }
                else
                {
                    Awb awb = this.dbContext.Awbs
                     .Include(a => a.Bookings)
                     .Include(a => a.Consignee)
                     .Include(a => a.Consignor)
                     .Include(a => a.SizeGroups)
                     .Include(a => a.Prepaid)
                     .Include(a => a.Collect)
                     .FirstOrDefault(a => a.Id == awbId);

                    if (awb == null)
                    {
                        return Result.Invalid($"Накладная не найдена awbId = {awbId}");
                    }

                    return Result.Ok(awb);
                }
            }
            catch (Exception ex)
            {
                return Result.Fail(new Error("Не удалось извлечь накладную для отслеживания").CausedBy(ex));
            }

        }

        public async Task<Result<AgentContractPoolAwbNums>> ReserveAsync(int? contragentId, string awbIdentifier = null)
        {
            try
            {
                string carrierCode = "SU";

                int serialNumber = 0;
                string acPrefix = null;
                Match m = AwbIdentifierParse(awbIdentifier);

                if (m.Success)
                {
                    serialNumber = Convert.ToInt32(m.Groups["serialNumber"].Value);
                    acPrefix = m.Groups["acPrefix"].Value;
                }

                #region Проверки
                if (string.IsNullOrEmpty(carrierCode))
                {
                    return Result.Invalid("Не заполнен перевозчик");
                }

                if (!string.IsNullOrEmpty(awbIdentifier) && !m.Success)
                {
                    return Result.Invalid($"Некорректный формат накладной: {awbIdentifier}");
                }

                if (!string.IsNullOrEmpty(awbIdentifier) && serialNumber % 10 != CheckDigit(serialNumber / 10))
                {
                    return Result.Invalid($"Неверная контрольная цифра накладной: {awbIdentifier}");
                }
                serialNumber = serialNumber / 10;

                if (!contragentId.HasValue && !m.Success)
                {
                    return Result.Invalid("Не заполнен код контрагента и корректный номер накладной");
                }

                if (!string.IsNullOrEmpty(acPrefix) && !this.dbContext.Carriers.AsNoTracking().Any(c => c.IataCode == carrierCode && c.AcPrefix == acPrefix))
                {
                    return Result.Invalid($"Префикс накладной {acPrefix} не соответсвует перевозчику {carrierCode}");
                }
                #endregion

                var poolsQuery = this.dbContext
                    .PoolAwbs
                    .Include(p => p.UsedAwbNumbers)
                    .Include(p => p.Contract)
                    .ThenInclude(c => c.SaleAgent)
                    .ThenInclude(sa => sa.Carrier)
                    .Include(p => p.Contract)
                    .ThenInclude(c => c.SaleAgent)
                    .ThenInclude(sa => sa.Contragent)
                    .Where(p => p.Contract.SaleAgent.Carrier.IataCode == carrierCode)
                    .Where(p => contragentId.HasValue ? p.Contract.SaleAgent.ContragentId == contragentId : true)
                    .Where(p => p.Contract.DateAt <= DateTime.UtcNow.Date && (p.Contract.DateTo.HasValue ? p.Contract.DateTo.Value >= DateTime.UtcNow.Date : true))
                    .AsNoTracking()
                    .AsEnumerable();

                var poolWithFreeNumber = poolsQuery
                        .FirstOrDefault(p =>
                        {
                            var query = Enumerable.Range(p.StartNumber, p.PoolLenght)
                            .Except(p.UsedAwbNumbers.Select(num => num.SerialNumber));
                            if (serialNumber > 0)
                            {
                                return query.Any(num => num == serialNumber);
                            }
                            else
                            {
                                serialNumber = query.FirstOrDefault();
                                return serialNumber > 0;
                            }
                        });

                if (poolWithFreeNumber != null)
                {
                    this.dbContext.Attach(poolWithFreeNumber);
                    var poolAwbNum = new AgentContractPoolAwbNums
                    {
                        SerialNumber = serialNumber,
                        AwbPool = poolWithFreeNumber,
                        AwbPoolId = poolWithFreeNumber.Id,
                    };

                    poolWithFreeNumber.UsedAwbNumbers.Add(poolAwbNum);
                    await this.dbContext.SaveChangesAsync();

                    return Result.Ok(poolAwbNum);
                }

                return Result.Invalid("Не удалось подобрать подходящий пул номеров накладных");
            }
            catch (Exception ex)
            {
                return Result.Fail(new Error("Ошибка резервирования номера для накладной").CausedBy(ex));
            }
        }

        private Result<Awb> BlankAwb(AgentContractPoolAwbNums poolAwbNum = null, string awbIdentifier = null)
        {
            try
            {
                string acPrefix = null;
                string serialNumber = null;

                if (poolAwbNum != null)
                {
                    acPrefix = poolAwbNum.AwbPool?.Contract?.SaleAgent?.Carrier?.AcPrefix;
                    serialNumber = (poolAwbNum.SerialNumber * 10 + CheckDigit(poolAwbNum.SerialNumber)).ToString("D8");
                }

                if ((string.IsNullOrEmpty(acPrefix) || string.IsNullOrEmpty(serialNumber)) && !string.IsNullOrEmpty(awbIdentifier))
                {
                    Match m = AwbIdentifierParse(awbIdentifier);
                    if (m.Success)
                    {
                        acPrefix = m.Groups["acPrefix"].Value;
                        serialNumber = m.Groups["serialNumber"].Value;
                    }
                }

                if (string.IsNullOrEmpty(acPrefix) || string.IsNullOrEmpty(serialNumber))
                {
                    return Result.Fail("Некорректный формат номера накладной");
                }

                Awb newAwb = new Awb
                {
                    AcPrefix = acPrefix,
                    SerialNumber = serialNumber,

                    Destination = string.Empty,
                    Origin = string.Empty,

                    AgentId = poolAwbNum?.AwbPool?.Contract?.SaleAgentId,
                    VolumeCode = "MC",
                    VolumeAmount = 0,
                    WeightCode = "K",
                    Weight = 0,
                    ManifestDescriptionOfGoods = string.Empty,
                    ManifestDescriptionOfGoodsRu = string.Empty,
                    QuanDetShipmentDescriptionCode = "T",
                    NumberOfPieces = 0,
                    PoolAwbId = poolAwbNum?.AwbPoolId,
                    CreatedDate = DateTime.UtcNow
                };

                this.dbContext.Add(newAwb);
                return Result.Ok(newAwb);
            }
            catch (Exception ex)
            {
                return Result.Fail(new Error("не удалось получить бланк накладной по номеру").CausedBy(ex));
            }
        }

        public async Task<Result<int>> SaveAwbAsync(AgentContractPoolAwbNums poolAwbNumber)
        {
            Result<Awb> blankAwb = this.BlankAwb(poolAwbNumber);
            if (blankAwb.IsSuccess)
            {
                return await this.SaveAwb(blankAwb.Value, StatusAwb.Draft.Value);
            }

            return blankAwb.ToResult<int>();
        }

        public async Task<Result<int>> SaveAwb(Awb awb, string status)
        {
            try
            {
                CalculateTariff(awb);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при расчете тарифа");
            }

            if (status == StatusAwb.Draft.Value || string.IsNullOrEmpty(status))
            {
                return await SaveAwbDraft(awb);
            }
            else if (status == StatusAwb.Booking.Value)
            {
                return await SaveAwbBooked(awb);
            }
            else if (status == StatusAwb.Cargo.Value)
            {
                return await SaveAwbRcs(awb);
            }
            else if (status == StatusAwb.Verified.Value)
            {
                return await SaveAwbVerified(awb);
            }

            return Result.Fail(new Error($"Неподдерживаемый статус накладной: {status}").CausedBy(new NotSupportedException()));
        }

        private async Task<Result<int>> SaveAwbDraft(Awb awb)
        {
            try
            {
                awb.Status = StatusAwb.Draft.Value;

                await this.dbContext.SaveChangesAsync();
                await ToHistory(awb.Id);

                return Result.Ok(awb.Id);
            }
            catch (Exception ex)
            {
                return Result.Fail(new Error($"Не удалось сохранить накладную в статусе 'Проект' awb = {awb?.Id}").CausedBy(ex));
            }
        }

        private async Task<Result<int>> SaveAwbBooked(Awb awb)
        {
            try
            {
                ulong[] fids = awb.Bookings.Select(b => b.FlightScheduleId).ToArray();
                var flts = this.dbContext.FlightShedules.AsNoTracking().Where(f => fids.Contains(f.Id)).Select(f => new { f.AircraftType, f.FlightDate }).ToArray();
                var pkzs = flts.Select(b =>
                {
                    var t = commPayloadService.FindPayload(b.AircraftType, b.FlightDate).Result.Value;
                    return t;
                }).ToList();

                var contragent = this.dbContext.Contragents
                    .AsNoTracking()
                    .Include(c => c.Carrier)
                    .Select(c => new { c.Id, IsCarrier = c.Carrier != null }).FirstOrDefault(c => c.Id == awb.AgentId);

                Awb origAwb = this.dbContext.Awbs.AsNoTracking()
                    .Include(a => a.Bookings)
                    .FirstOrDefault(a => a.Id == awb.Id) ?? new Awb();

                bool isChanged = origAwb.Bookings.FullJoin(awb.Bookings, b => b.Id, b => false, b => false, (b1, b2) => Math.Abs(b1.Weight - b2.Weight) < 0.001M && Math.Abs(b1.VolumeAmount - b2.VolumeAmount) < 0.001M && b1.NumberOfPieces == b2.NumberOfPieces && b1.SpaceAllocationCode == b2.SpaceAllocationCode).Any(it => !it);
                awb.Status = StatusAwb.Booking.Value;

                if (!contragent.IsCarrier)
                {
                    this.dbContext
                        .FlightShedules
                        .AsNoTracking()
                        .Include(f => f.Bookings)
                        .Select(f => new
                        {
                            f.Id,
                            f.AircraftType,
                            Weigth = f.Bookings.Where(b => awb.Id == b.AwbId).Sum(b => b.Weight),
                            Volume = f.Bookings.Where(b => awb.Id == b.AwbId).Sum(b => b.VolumeAmount),
                            Sac = f.Bookings.Where(b => awb.Id == b.AwbId).Select(b => b.SpaceAllocationCode).FirstOrDefault(),
                            FlightWeigth = f.Bookings.Where(b => b.SpaceAllocationCode == "KK" && awb.Id != b.AwbId).Sum(b => b.Weight),
                            FlightVolume = f.Bookings.Where(b => b.SpaceAllocationCode == "KK" && awb.Id != b.AwbId).Sum(b => b.VolumeAmount)
                        })
                        .Where(f => awb.Bookings.Select(b => b.FlightScheduleId).Contains(f.Id))
                        .AsEnumerable()
                        .Join(awb.Bookings, f => f.Id, b => b.FlightScheduleId, async (old, b) =>
                        {
                            if (!(old.Sac == "KK" && b.Weight <= old.Weigth && b.VolumeAmount <= old.Volume))
                            {
                                decimal weight = old.FlightWeigth + b.Weight;
                                decimal volume = old.FlightVolume + b.VolumeAmount;
                                CommPayloadFlatDetail pkz = pkzs.FirstOrDefault(p => p.AircraftType == old.AircraftType);
                                if (weight <= pkz.Weight && volume <= pkz.Volume)
                                {
                                    b.SpaceAllocationCode = "KK";
                                }
                                else
                                {
                                    b.SpaceAllocationCode = "NN";
                                }
                            }

                            return b;
                        })
                        .ToList();
                }

                await this.dbContext.SaveChangesAsync();
                await ToHistory(awb.Id);
                if (this.dbContext.ChangeLog.Any() && awb.Bookings.All(b => b.SpaceAllocationCode == "KK"))
                {
                    awb = this.dbContext.Awbs
                        .AsNoTracking()
                        .Include(a => a.Bookings)
                        .ThenInclude(b => b.FlightSchedule)
                        .Single(a => a.Id == awb.Id);


                    Fsa fsa = mapper.Map<Fsa>(awb);
                    await endpoint.Publish<BuildMessage>(new { __CorrelationId = Guid.NewGuid(), Fsa = fsa, LinkedObjectId = awb.Id, CustomerId =55 });
                }

                return Result.Ok(awb.Id);
            }
            catch (Exception ex)
            {
                return Result.Fail(new Error($"Не удалось сохранить накладную в статусе 'Бронирование' awb = {awb?.Id}").CausedBy(ex));
            }
        }

        private async Task<Result<int>> SaveAwbRcs(Awb awb)
        {
            try
            {
                if (awb.Status != StatusAwb.Cargo.Value)
                {
                    awb.Status = StatusAwb.Cargo.Value;
                    this.dbContext.BookingRcs.AddRange(mapper.Map<ICollection<BookingRcs>>(awb.Bookings));
                }
                await this.dbContext.SaveChangesAsync();
                await ToHistory(awb.Id);

                return Result.Ok(awb.Id);
            }
            catch (Exception ex)
            {
                return Result.Fail(new Error($"Не удалось сохранить накладную в статусе 'Груз сдан' awb = {awb?.Id}").CausedBy(ex));
            }
        }

        private async Task<Result<int>> SaveAwbVerified(Awb awb)
        {
            try
            {
                if (awb.Status != StatusAwb.Verified.Value)
                {
                    awb.Status = StatusAwb.Verified.Value;
                    await this.dbContext.SaveChangesAsync();
                    await ToHistory(awb.Id);

                    awb = this.dbContext.Awbs
                        .AsNoTracking()
                        .Include(a => a.Bookings)
                        .ThenInclude(b => b.FlightSchedule)
                        .Include(a => a.Agent)
                        .ThenInclude(c => c.SalesAgent)
                        .FirstOrDefault(w => w.Id == awb.Id);

                    if (awb.Agent?.SalesAgent != null)
                    {
                        awb.Agent.SalesAgent = awb.Agent.SalesAgent.Where(sa => sa.CarrierId == awb.CarrierId).ToList();
                    }

                    var fwb = mapper.Map<Fwb>(awb);

                    await endpoint.Publish<BuildMessage>(new { __CorrelationId = Guid.NewGuid(), Fwb = fwb, LinkedObjectId = awb.Id, CustomerId =55 });
                }
                else
                {
                    await this.dbContext.SaveChangesAsync();
                    await ToHistory(awb.Id);
                }

                return Result.Ok(awb.Id);
            }
            catch (Exception ex)
            {
                return Result.Fail(new Error($"Не удалось сохранить накладную в статусе 'Валидация' awb = {awb?.Id}").CausedBy(ex));
            }
        }

        private async Task ToHistory(int awbId)
        {
            try
            {
                await endpoint.Publish<SendHistory>(new
                {
                    __CorrelationId = Guid.NewGuid(),
                    HistoryCode = "SAVE_AWB",
                    DateModifed = DateTime.UtcNow,
                    Description = "Сохранение накладной",
                    DescriptionEng = "Save awb",
                    LinkedAwbId = awbId,
                    ChangeLog = mapper.Map<List<Change>>(this.dbContext.ChangeLog),
                    CustomerId = 55
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Не удалось отправить изменения в History");
            }
        }

        private int CheckDigit(int awbSerialNumber)
        {
            int step1 = awbSerialNumber / 7;
            int step2 = step1 * 7;
            int expectedCheckDigit = awbSerialNumber - step2;
            return expectedCheckDigit;
        }

        Match AwbIdentifierParse(string awbIdentifier)
        {
            return awbPattern.Match(awbIdentifier ?? string.Empty);
        }

        #region Tariffs
        private async void CalculateTariff(Awb awb)
        {
            var origin = awb.Origin;
            var destination = awb.Destination;
            var product = awb.Product;

            var tariffSolution = this.dbContext.TariffSolutions
                .AsNoTracking()
                .Include(x => x.AwbOriginAirport)
                .Include(x => x.AwbDestinationAirport)
                .Include(x => x.AwbOriginTariffGroup)
                .Include(x => x.AwbDestinationTariffGroup)
                .Include(x => x.TransitAirport)
                .Include(x => x.RateGridRankValues)
                .Include(x => x.Addons)
                .ThenInclude(x => x.ShrCodes)
                .FirstOrDefault(x =>
                    (x.AwbOriginAirport.Code == origin || x.AwbOriginTariffGroup.Airports.Any(y => y.Code == origin))
                    && (x.AwbDestinationAirport.Code == destination || x.AwbDestinationTariffGroup.Airports.Any(y => y.Code == destination))
                    );

            if (tariffSolution == null)
            {
                return;
            }

            awb.TariffsSolutionCode = tariffSolution.Code;
            awb.AllIn = tariffSolution.IsAllIn;
            awb.PaymentProcedure = tariffSolution.PaymentTerms;
            awb.WeightCharge = tariffSolution.WeightCharge;
            awb.SalesChannel = tariffSolution.SalesChannel;

            decimal weightToCharge = 0;

            if (tariffSolution.WeightCharge == "CHARGEABLE")   //  CHARGEABLE / ACTUAL
                weightToCharge = awb.ChargeWeight;
            else
                weightToCharge = awb.Weight;

            var total = 0m;
            var baseTariffRate = 0m;

            var tariffNet = tariffSolution.RateGridRankValues.OrderBy(x => x.Rank).ToArray();
            if (tariffNet.Length > 0)
            {
                RateGridRankValue selectedRank = null;

                for (int i = 0; i < tariffNet.Length; i++)
                {
                    if (tariffNet[i].Rank <= weightToCharge)
                        selectedRank = tariffNet[i];
                    else
                        break;
                }

                if (selectedRank != null)
                {
                    baseTariffRate = selectedRank.Value;
                    total = weightToCharge * baseTariffRate;
                }
            }

            var minTariff = tariffSolution.MinTariff;
            var tariffClass = "Q";
            string addOnCode = null;

            bool isMinimum = false;
            if (total < minTariff)
            {
                isMinimum = true;
                tariffClass = "M";
                baseTariffRate = minTariff;
                total = minTariff;
            }

            var finalTariffRate = baseTariffRate;

            TariffAddon addon = FindAddon(awb, tariffSolution, isMinimum);

            if (addon != null)
            {
                decimal addonValue = 0m;

                if (isMinimum)
                {
                    addonValue = addon.MinimumAddon;
                    addOnCode = $"M{addonValue}";
                }
                else
                {
                    addonValue = addon.WeightAddon;
                    addOnCode = $"Q{addonValue}";
                }

                if (addonValue > 100)
                {
                    tariffClass = "S";
                }
                else
                {
                    tariffClass = "R";
                }

                finalTariffRate = finalTariffRate * addonValue / 100;
                total = total * addonValue / 100;
            }

            awb.TariffClass = tariffClass;
            awb.BaseTariffRate = baseTariffRate;
            awb.AddOn = addOnCode;
            awb.TariffRate = finalTariffRate;
            awb.Total = total;

            FillPrepaidCollectFields(awb, tariffSolution, total);

            if (awb.OtherCharges == null)
                awb.OtherCharges = new List<OtherCharge>();

            var charges = FindCarrierCharges(awb);

            if (charges != null && charges.Count > 0)
            {

                foreach (var charge in charges)
                {
                    var binding = charge.CarrierChargeBindings.FirstOrDefault(x => x.Airports.Any(a => a.Code == origin));

                    if (binding != null)
                    {
                        var value = 0.0M;

                        switch (binding.Parameter)
                        {
                            case "AWB":
                                value = binding.Value;
                                break;
                            case "ACTUAL_WEIGHT":
                                value = awb.Weight * binding.Value;
                                break;
                            case "CHARGE_WEIGHT":
                                value = awb.ChargeWeight * binding.Value;
                                break;
                            case "PERCENT":
                                value = total * binding.Value / 100;
                                break;
                        }

                        var otherCharge = awb.OtherCharges.FirstOrDefault(x => x.TypeCharge == charge.Code && x.CA == charge.Recepient);

                        if (otherCharge == null)
                        {
                            otherCharge = new OtherCharge
                            {
                                TypeCharge = charge.Code,
                                CA = charge.Recepient,
                            };

                            awb.OtherCharges.Add(otherCharge);
                        }

                        otherCharge.Prepaid = value;
                    }
                }
            }
        }

        private List<CarrierCharge> FindCarrierCharges(Awb awb)
        {
            var origin = awb.Origin;

            var allChargedForOriginAirport = this.dbContext.CarrierCharges
                .AsNoTracking()
                .Include(x => x.IncludedShrCodes)
                .Include(x => x.ExcludedShrCodes)
                .Include(x => x.IncludedProducts)
                .Include(x => x.ExcludedProducts)
                .Include(x => x.CarrierChargeBindings)
                .ThenInclude(x => x.Currency)
                .Include(x => x.CarrierChargeBindings)
                .ThenInclude(x => x.Country)
                .Include(x => x.CarrierChargeBindings)
                .ThenInclude(x => x.Airports)
                .Where(x => x.CarrierChargeBindings.Any(b => b.Airports.Any(a => a.Code == origin)))
                .ToList();

            // Если доп сборов нет
            if (allChargedForOriginAirport.Count == 0)
            {
                return allChargedForOriginAirport;
            }

            // Если все доп сборы обязательные
            if (allChargedForOriginAirport.All(x => x.ApplicationType == "M"))
            {
                return allChargedForOriginAirport;
            }

            List<CarrierCharge> matchedCharges = new List<CarrierCharge>(allChargedForOriginAirport.Count);

            // Все обязательные доп сборы
            matchedCharges.AddRange(allChargedForOriginAirport
                .Where(x => x.ApplicationType == "M"));

            /*
            matchedCharges.AddRange(allChargedForOriginAirport
                .Where(x => x.ApplicationType == "СM" 
                        && x.IncludedProducts.Any(p => p.Trigger == awb.Product) 
                        && x.ExcludedProducts.All(p => p.Trigger != awb.Product)
                        && x.ExcludedProducts.All(p => p.Trigger != awb.Product)));
            */

            return matchedCharges;
        }

        private static void FillPrepaidCollectFields(Awb awb, TariffSolution tariffSolution, decimal total)
        {
            if (tariffSolution.PaymentTerms == "PREPAID")
            {
                // Prepaid
                if (awb.Prepaid == null)
                    awb.Prepaid = new TaxCharge();

                awb.Prepaid.Charge = total;
                awb.Prepaid.ValuationCharge = 0;
                awb.Prepaid.Tax = 0;
                awb.Prepaid.TotalOtherChargesDueAgent = 0;
                awb.Prepaid.TotalOtherChargesDueCarrier = 0;
                awb.Prepaid.Total = total;

                // Collect = 0
                if (awb.Collect != null)
                {
                    awb.Collect.Charge = 0;
                    awb.Collect.ValuationCharge = 0;
                    awb.Collect.Tax = 0;
                    awb.Collect.TotalOtherChargesDueAgent = 0;
                    awb.Collect.TotalOtherChargesDueCarrier = 0;
                    awb.Collect.Total = 0;
                }
            }
            else
            {
                // Collect
                if (awb.Collect == null)
                    awb.Collect = new TaxCharge();

                awb.Collect.Charge = total;
                awb.Collect.ValuationCharge = 0;
                awb.Collect.Tax = 0;
                awb.Collect.TotalOtherChargesDueAgent = 0;
                awb.Collect.TotalOtherChargesDueCarrier = 0;
                awb.Collect.Total = total;

                // Prepaid = 0
                if (awb.Prepaid != null)
                {
                    awb.Prepaid.Charge = 0;
                    awb.Prepaid.ValuationCharge = 0;
                    awb.Prepaid.Tax = 0;
                    awb.Prepaid.TotalOtherChargesDueAgent = 0;
                    awb.Prepaid.TotalOtherChargesDueCarrier = 0;
                    awb.Prepaid.Total = 0;
                }
            }
        }

        private TariffAddon FindAddon(Awb awb, TariffSolution tariffSolution, bool isMinimum)
        {
            TariffAddon result = null;

            // Если есть надбавки в тарифе и SHR в накладной
            if (tariffSolution.Addons != null && tariffSolution.Addons.Any() && !string.IsNullOrEmpty(awb.SpecialHandlingRequirements))
            {

                var awbShrs = awb.SpecialHandlingRequirements.Split("/", StringSplitOptions.RemoveEmptyEntries);
                if (awbShrs.Length > 0)
                {
                    var addons = tariffSolution.Addons
                        .Where(x => x.ShrCodes.Any(y => awbShrs.Contains(y.Code)))
                        .ToList();

                    if (addons.Any())
                    {
                        if (addons.Count == 1)
                        {
                            result = addons[0];
                        }
                        else
                        {
                            result = addons.OrderByDescending(x => isMinimum ? x.MinimumAddon : x.WeightAddon).First();
                        }
                    }
                }
            }

            return result;
        }
        #endregion
    }
}
