using Cargo.Contract.DTOs.Bookings;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Cargo.Infrastructure.Report
{

    public class AirportName
    {
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
    }

    public static class AwbDtoExtension
    {

        /// <summary>
        /// Экспортирует накладную в PDF
        /// </summary>
        /// <returns></returns>
        //TODO: сделать отдельный класс AwbPrintOptions и засунуть все параметры туда
        public static byte[] ToPdf(this
            AwbDto awb,
            bool contract = false,
            int pageCount = 1,
            AirportName airportName = null,
            bool isBlank = false,
            bool asGreed = false)
        {
            const string airWaybillPdf = "FwbBlank/air_waybill.pdf";
            const string contractPdf = "FwbBlank/awb_contract.pdf";

            PdfPage contractPage = null;
            if (contract)
            {
                var doc = PdfReader.Open(contractPdf, PdfDocumentOpenMode.Import);
                contractPage = doc.Pages[0];
            }
            var pdfDoc = PdfReader.Open(airWaybillPdf, PdfDocumentOpenMode.Import);
            var pdfNewDoc = new PdfDocument();

            foreach (var page in pdfDoc.Pages)
            {
                DrawPage(awb, pdfNewDoc, page, contractPage, "ORIGINAL 1 (FOR ISSUING CARRIER)", airportName, true, isBlank, asGreed);
                if (pageCount < 9)
                    continue;
                DrawPage(awb, pdfNewDoc, page, contractPage, "ORIGINAL 2 (FOR CONSIGNEE)", airportName, true, isBlank, asGreed);
                DrawPage(awb, pdfNewDoc, page, contractPage, "ORIGINAL 3 (FOR CONSIGNOR)", airportName, true, isBlank, asGreed);
                DrawPage(awb, pdfNewDoc, page, contractPage, "COPY 4 (DELIVERY RECEPT)", airportName, false, isBlank, asGreed);
                DrawPage(awb, pdfNewDoc, page, contractPage, "COPY 5 (EXTRA COPY)", airportName, false, isBlank, asGreed);
                DrawPage(awb, pdfNewDoc, page, contractPage, "COPY 6 (EXTRA COPY)", airportName, false, isBlank, asGreed);
                DrawPage(awb, pdfNewDoc, page, contractPage, "COPY 7 (EXTRA COPY)", airportName, false, isBlank, asGreed);
                DrawPage(awb, pdfNewDoc, page, contractPage, "COPY 8 (EXTRA COPY)", airportName, false, isBlank, asGreed);
                DrawPage(awb, pdfNewDoc, page, contractPage, "COPY 9 (EXTRA COPY)", airportName, false, isBlank, asGreed);
                if (pageCount > 9)
                {
                    DrawPage(awb, pdfNewDoc, page, contractPage, "COPY 10 (EXTRA COPY)", airportName, false, isBlank, asGreed);
                    DrawPage(awb, pdfNewDoc, page, contractPage, "COPY 11 (EXTRA COPY)", airportName, false, isBlank, asGreed);
                    DrawPage(awb, pdfNewDoc, page, contractPage, "COPY 12 (EXTRA COPY)", airportName, false, isBlank, asGreed);
                }
            }

            return GetByteArray(pdfNewDoc);
        }

        private static void DrawPage(AwbDto awb,
            PdfDocument document,
            PdfPage templatePage,
            PdfPage? contractPage,
            string to,
            AirportName airportName = null,
            bool addContractPage = false,
            bool isBlank = false,
            bool asGreed = false)
        {
            var pp = isBlank ? document.AddPage() : document.AddPage(templatePage);
            Draw(awb, pp, to, airportName, isBlank, asGreed);
            if (contractPage != null && addContractPage)
                document.AddPage(contractPage);
            if (!addContractPage)
                document.AddPage();
        }

        private static XGraphics Draw(AwbDto awb, PdfPage pp, string to, AirportName airportName = null, bool isBlank = false, bool asGreed = false)
        {
            var gfx = XGraphics.FromPdfPage(pp);


            XFont font12 = new("Verdana", 12, XFontStyle.Regular, XPdfFontOptions.UnicodeDefault);
            XFont font10 = new("Verdana", 10, XFontStyle.Regular, XPdfFontOptions.UnicodeDefault);
            XFont font8 = new("Verdana", 8, XFontStyle.Regular, XPdfFontOptions.UnicodeDefault);

            gfx.DrawStringMM($"{awb.AcPrefix}", font12, 9, 9, 18, 14, XStringFormats.BottomCenter);
            gfx.DrawStringMM($"{awb.Origin}", font12, 19, 9, 31, 14, XStringFormats.BottomCenter);
            gfx.DrawStringMM($"{awb.SerialNumber}", font12, 32, 9, 60, 14);

            gfx.DrawStringMM($"{awb.AcPrefix}-{awb.SerialNumber}", font12, 157, 9, 190, 14);

            gfx.DrawStringMM($"{awb.Destination}", font12, 9, 101, 18, 106);

            gfx.DrawStringMM($"{awb.Currency}", font12, 100, 101, 108, 106);

            gfx.DrawStringMM($"{awb.NDV}", font12, 144, 101, 167, 106);
            gfx.DrawStringMM($"{awb.NCV}", font12, 169, 101, 195, 106);

            gfx.DrawStringMM($"{awb.Consignor?.NameEn}", font8, 9, 21, 98, 26);
            gfx.DrawStringMM($"{awb.Consignor?.AddressEn}", font8, 9, 24, 98, 29);
            gfx.DrawStringMM($"{awb.Consignor?.CityEn} {awb.Consignor?.RegionEn} {awb.Consignor?.ZipCode}", font8, 9, 27, 98, 42);
            gfx.DrawStringMM($"{awb.Consignor?.Phone} {awb.Consignor?.Fax}", font8, 9, 30, 98, 45);

            gfx.DrawStringMM($"{awb.Consignee.NameEn}", font8, 9, 47, 98, 52);
            gfx.DrawStringMM($"{awb.Consignee.AddressEn}", font8, 9, 50, 98, 58);
            gfx.DrawStringMM($"{awb.Consignee.CityEn} {awb.Consignee.RegionEn} {awb.Consignee.ZipCode}", font8, 9, 53, 98, 63);
            gfx.DrawStringMM($"{awb.Consignee.Phone} {awb.Consignee.Fax}", font8, 9, 56, 98, 63);

            gfx.DrawStringMM($"{awb.Agent.NameEn}", font8, 9, 69, 98, 74);
            gfx.DrawStringMM($"{awb.Agent.CityEn} {awb.Consignor?.RegionEn}", font8, 9, 72, 98, 80);

            gfx.DrawStringMM($"{awb.Agent.IataCode} {awb.Consignor?.RegionEn}", font12, 9, 86, 53, 89);

            if (airportName != null)
            {
                gfx.DrawStringMM($"{airportName.Origin}", font12, 9, 93, 99, 97);
                gfx.DrawStringMM($"{airportName.Destination}", font12, 9, 110, 53, 114);
            }



            gfx.DrawStringMM($"{awb.SpecialServiceRequest}", font8, 9, 118, 195, 123);



            var firstBooking = awb.Bookings?.FirstOrDefault(b => b.FlightSchedule != null && b.FlightSchedule?.Origin == awb.Origin);
            if (firstBooking?.FlightSchedule != null)
            {
                var car = firstBooking.FlightSchedule?.Number[..2];

                if (!isBlank)
                {
                    if (awb.AcPrefix == "555")
                    {
                        XImage image = XImage.FromFile("FwbBlank/afl_logo.jpg");
                        gfx.DrawImage(image, XUnit.FromMillimeter(125).Point, XUnit.FromMillimeter(16).Point, XUnit.FromMillimeter(60).Point, XUnit.FromMillimeter(14).Point);
                    } /*else if (car == "FV")
                {
                    XImage image = XImage.FromFile("wwwroot/assets/RussiaLogo.jpg");
                    gfx.DrawImage(image, XUnit.FromMillimeter(125).Point, XUnit.FromMillimeter(16).Point, XUnit.FromMillimeter(60).Point, XUnit.FromMillimeter(14).Point);
                } else if (car == "HZ")
                {
                    XImage image = XImage.FromFile("wwwroot/assets/hz_logo.jpg");
                    gfx.DrawImage(image, XUnit.FromMillimeter(125).Point, XUnit.FromMillimeter(16).Point, XUnit.FromMillimeter(60).Point, XUnit.FromMillimeter(14).Point);
                }*/
                }

                gfx.DrawStringMM(car, font12, 19, 101, 33, 106, XStringFormats.BottomCenter);
                gfx.DrawStringMM($"{firstBooking.FlightSchedule?.Destination}", font12, 34, 101, 64, 106, XStringFormats.BottomCenter);
                gfx.DrawStringMM($"{firstBooking.FlightSchedule?.Number}", font12, 55, 109, 76, 114, XStringFormats.BottomCenter);
                gfx.DrawStringMM($"{firstBooking.FlightSchedule?.FlightDate?.ToString("ddMMM", CultureInfo.CreateSpecificCulture("en-US")).ToUpper() ?? ""}", font12, 77, 110, 100, 114, XStringFormats.BottomCenter);
            }

            gfx.DrawStringMM($"{awb.NumberOfPieces}", font10, 9, 141, 18, 146);
            gfx.DrawStringMM($"{awb.Weight:F}", font10, 19, 141, 36, 146);
            gfx.DrawStringMM($"{awb.WeightCode}", font10, 36.7, 141, 38, 146);
            gfx.DrawStringMM($"{awb.TariffClass}", font10, 41.9, 141, 44, 146);
            gfx.DrawStringMM($"{awb.ChargeWeight:F}", font10, 65, 141, 81, 146);
            if (asGreed)
            {
                gfx.DrawStringMM("AS AGREED", font10, 108, 141, 137, 146);
                gfx.DrawStringMM("AS AGREED", font10, 108, 189, 137, 194);
            }
            else
            {
                gfx.DrawStringMM($"{awb.BaseTariffRate:F}", font10, 45, 141, 64, 146);


                gfx.DrawStringMM($"{awb.TariffRate:F}", font10, 85, 141, 104, 146);
                gfx.DrawStringMM($"{awb.Total:F}", font10, 108, 141, 137, 146);


                gfx.DrawStringMM($"{awb.TariffClass}", font10, 41.9, 189, 44, 194);
                gfx.DrawStringMM($"{awb.Total:F}", font10, 108, 189, 137, 194);

            }
            gfx.DrawStringMM($"{awb.ManifestDescriptionOfGoods}", font10, 141, 141, 196, 146);
            gfx.DrawStringMM($"VOL {awb.VolumeAmount:F} {awb.VolumeCode}", font10, 141, 148, 196, 152);

            gfx.DrawStringMM($"{awb.NumberOfPieces}", font10, 9, 189, 18, 194);
            gfx.DrawStringMM($"{awb.Weight:F}", font10, 19, 189, 36, 194);
            gfx.DrawStringMM($"{awb.WeightCode}", font10, 36.7, 189, 38, 194);

            if (asGreed)
            {
                gfx.DrawStringMM("AS AGREED", font10, 9, 198, 41, 202);
                gfx.DrawStringMM("AS AGREED", font10, 9, 246, 41, 250);
            }
            else
            {
                //TAX
                if (awb.Charge?.Prepaid?.Charge > 0)
                    gfx.DrawStringMM($"{awb.Charge?.Prepaid?.Charge:F}", font10, 9, 198, 41, 202);
                if (awb.Charge?.Collect?.Charge > 0)
                    gfx.DrawStringMM($"{awb.Charge?.Collect?.Charge:F}", font10, 44, 198, 78, 202);

                if (awb.Charge?.Prepaid?.ValuationCharge > 0)
                    gfx.DrawStringMM($"{awb.Charge?.Prepaid?.ValuationCharge:F}", font10, 9, 208, 41, 212);
                if (awb.Charge?.Collect?.ValuationCharge > 0)
                    gfx.DrawStringMM($"{awb.Charge?.Collect?.ValuationCharge:F}", font10, 44, 208, 78, 212);

                if (awb.Charge?.Prepaid?.Tax > 0)
                    gfx.DrawStringMM($"{awb.Charge?.Prepaid?.Tax:F}", font10, 9, 216, 41, 220);
                if (awb.Charge?.Collect?.Tax > 0)
                    gfx.DrawStringMM($"{awb.Charge?.Collect?.Tax:F}", font10, 44, 216, 78, 220);

                if (awb.Charge?.Prepaid?.TotalOtherChargesDueAgent > 0)
                    gfx.DrawStringMM($"{awb.Charge?.Prepaid?.TotalOtherChargesDueAgent:F}", font10, 9, 224, 41, 228);
                if (awb.Charge?.Collect?.TotalOtherChargesDueAgent > 0)
                    gfx.DrawStringMM($"{awb.Charge?.Collect?.TotalOtherChargesDueAgent:F}", font10, 44, 224, 78, 228);

                if (awb.Charge?.Prepaid?.TotalOtherChargesDueCarrier > 0)
                    gfx.DrawStringMM($"{awb.Charge?.Prepaid?.TotalOtherChargesDueCarrier:F}", font10, 9, 233, 41, 236);
                if (awb.Charge?.Collect?.TotalOtherChargesDueCarrier > 0)
                    gfx.DrawStringMM($"{awb.Charge?.Collect?.TotalOtherChargesDueCarrier:F}", font10, 44, 233, 78, 236);

                if (awb.Charge?.Prepaid?.Total > 0)
                    gfx.DrawStringMM($"{awb.Charge?.Prepaid?.Total:F}", font10, 9, 246, 41, 250);
                if (awb.Charge?.Collect?.Total > 0)
                    gfx.DrawStringMM($"{awb.Charge?.Collect?.Total:F}", font10, 44, 246, 78, 250);
            }




            gfx.DrawStringMM($"{awb.AcPrefix}-{awb.SerialNumber}", font12, 157, 260, 190, 266);
            gfx.DrawStringMM(to, font8, 118, 268, 196, 270);
            return gfx;
        }

        private static void DrawStringMM(this XGraphics gfx, string text, XFont font, double x1, double y1, double x2, double y2)
        {

            var rec = new XRect(
                XUnit.FromMillimeter(x1).Point,
                XUnit.FromMillimeter(y1).Point,
                XUnit.FromMillimeter(x2 - x1).Point,
                XUnit.FromMillimeter(y2 - y1).Point);

            gfx.DrawString(text, font, XBrushes.Black, rec, XStringFormats.TopLeft);
        }

        private static void DrawStringMM(this XGraphics gfx, string text, XFont font, double x1, double y1, double x2, double y2, XStringFormat format)
        {

            var rec = new XRect(
                XUnit.FromMillimeter(x1).Point,
                XUnit.FromMillimeter(y1).Point,
                XUnit.FromMillimeter(x2 - x1).Point,
                XUnit.FromMillimeter(y2 - y1).Point);

            gfx.DrawString(text, font, XBrushes.Black, rec, format);
        }

        private static byte[] GetByteArray(PdfDocument pdfNewDoc)
        {
            using var ms = new MemoryStream();
            pdfNewDoc.Save(ms, false);
            byte[] buffer = new byte[ms.Length];
            ms.Seek(0, SeekOrigin.Begin);
            ms.Flush();
            ms.Read(buffer, 0, (int)ms.Length);
            return buffer;
        }
    }
}
