using Cargo.Contract.DTOs;
using Cargo.Infrastructure.Data;
using Cargo.Infrastructure.Data.Model;
using Cargo.Infrastructure.Data.Model.Settings.CommPayloads;
using Cargo.Infrastructure.Data.Model.Settings.MyFlights;
using IdealResults;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cargo.Application.Services
{
    public class ScheduleService
    {

        CargoContext dbContext;
        public ScheduleService(CargoContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<Result<FlightShedule>> Flight(string flightNumber, string day, string month = null)
        {
            try
            {
                if (string.IsNullOrEmpty(flightNumber) || string.IsNullOrEmpty(day))
                {
                    return Result.Invalid("Отсутствует обязательные к заполнению flightNumber, day");
                }

                DateTime flightDate = ParseFlightDate(day, month);

                FlightShedule flight = await this.dbContext.FlightShedules
                 .AsNoTracking()
                 .FirstOrDefaultAsync(f => f.Number == flightNumber && f.FlightDate.Date == flightDate);

                if (flight == null)
                {
                    return Result.Invalid($"Рейс не найден: {flightNumber}/{day}{month}");
                }

                return Result.Ok(flight);
            }
            catch (Exception ex)
            {
                return Result.Fail(new Error($"Не удалось извлечь рейс {flightNumber}/{day}{month}").CausedBy(ex));
            }
        }

        public async Task<Result<FlightShedule>> Flight(string flightNumber, DateTime flightDate)
        {
            try
            {
                if (string.IsNullOrEmpty(flightNumber))
                {
                    return Result.Invalid("Отсутствует обязательные к заполнению flightNumber");
                }

                FlightShedule flight = await this.dbContext.FlightShedules
                 .AsNoTracking()
                 .FirstOrDefaultAsync(f => f.Number == flightNumber && f.FlightDate.Date == flightDate);

                if (flight == null)
                {
                    return Result.Invalid($"Рейс не найден: {flightNumber}/{flightDate.ToString("MMM", new CultureInfo("en-US")).ToUpper()}");
                }

                return Result.Ok(flight);
            }
            catch (Exception ex)
            {
                return Result.Fail(new Error($"Не удалось извлечь рейс {flightNumber}/{flightDate.ToString("MMM", new CultureInfo("en-US")).ToUpper()}").CausedBy(ex));
            }
        }

        public static DateTime ParseFlightDate(string sDay, string sMonth = null)
        {
            int day = int.Parse(sDay);
            DateTime d = DateTime.Today;
            if (!string.IsNullOrEmpty(sMonth))
            {
                d = DateTime.ParseExact($"{day}{sMonth}", "dMMM", new CultureInfo("en-US"));
                DateTime today = DateTime.Now;
                int m = Math.Abs(((today.Year - d.Year) * 12) + today.Month - d.Month);
                if (m > 5)
                {
                    d = d.AddYears(1);
                }
            }

            if (d == DateTime.Today)
            {
                int month = DateTime.Today.Month;
                int year = DateTime.Today.Year;
                if (Math.Abs(day - DateTime.Today.Day) > 15)
                {
                    month++;
                    if (month > 12)
                    {
                        month = 1;
                        year++;
                    }
                }
                d = new DateTime(year, month, day);
            }

            return d;
        }
    }
}
