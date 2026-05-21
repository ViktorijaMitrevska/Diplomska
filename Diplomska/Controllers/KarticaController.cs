using ClosedXML.Excel;
using Diplomska.Services;
using Diplomska.ViewModels;
using Google.Cloud.Firestore.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Diplomska.Controllers
{
    [Authorize]
    public class KarticaController : Controller
    {
        private readonly FirestoreService _firestore;

        public KarticaController(FirestoreService firestore)
        {
            _firestore = firestore;
        }
       
        private async Task<MakeNewKarticaViewModel> BuildKartica(string firmaId, DateTime Od, DateTime Do)
            {
                var firma = await _firestore.GetFirmaByIdAsync(firmaId);

                var fakturi = await _firestore.GetFakturaByFirmaAsync(firmaId);
                var fakturiPeriod = fakturi
                        .Where(f => f.PriemDate >= Od && f.PriemDate <= Do)
                        .OrderBy(f => f.PriemDate)
                        .ToList();

                double pocetnoSaldo = fakturi
                        .Where(f => f.PriemDate < Od)
                        .Sum(f => f.Suma)
                        -
                        fakturi
                        .Where(f => f.PriemDate < Od && f.DaliEPlateno)
                        .Sum(f => f.Suma);

                var vm = new MakeNewKarticaViewModel
                    {
                        Firma = firma,
                        FirmaId = firmaId,
                        Od = Od,
                        Do = Do
                    };

                double saldo = pocetnoSaldo;

                if (pocetnoSaldo != 0)
                {
                    vm.Stavki.Add(new KarticaStavkaViewModel
                    {
                        Datum = Od,
                        Opis = "Салдо од претходен период",
                        Dolzi = 0,
                        Plateno = 0,
                        Saldo = saldo
                    });
                }

           

            foreach (var f in fakturiPeriod)
                {
                    saldo += f.Suma;

                    vm.Stavki.Add(new KarticaStavkaViewModel
                    {
                        Datum = f.PriemDate,
                        Opis = $"Фактура бр. {f.BrojNaDokument}",
                        Dolzi = f.Suma,
                        Plateno = 0,
                        Saldo = saldo
                    });

                    if (f.DaliEPlateno && f.PaymentDate.HasValue)
                    {
                        saldo -= f.Suma;

                        vm.Stavki.Add(new KarticaStavkaViewModel
                        {
                            Datum = f.PaymentDate.Value,
                            Opis = $"Плаќање за фактура {f.BrojNaDokument}",
                            Dolzi = 0,
                            Plateno = f.Suma,
                            Saldo = saldo
                        });
                    }
                }

                var saldoPeriod = saldo - pocetnoSaldo;
                vm.SaldoPeriod = saldoPeriod;
                vm.PeriodDolzi = fakturiPeriod.Sum(f => f.Suma);
                vm. PeriodPlateno = fakturiPeriod.Where(f => f.DaliEPlateno).Sum(f => f.Suma);

                vm.VkupnoDolzi = fakturi.Sum(f => f.Suma);
                vm.VkupnoPlateno = fakturi.Where(f => f.DaliEPlateno).Sum(f => f.Suma);

            return vm;
        }
        public async Task<IActionResult> Index(MakeNewKarticaViewModel vm)
        {
            vm.Firmi = await _firestore.GetFirmaAsync();

            if (!string.IsNullOrEmpty(vm.FirmaId) && vm.Od.HasValue && vm.Do.HasValue)
            {
                var karticaVm = await BuildKartica(vm.FirmaId, vm.Od.Value, vm.Do.Value);

                karticaVm.Firmi = vm.Firmi; 
                return View(karticaVm);
            }

            return View(vm);
        }

        public async Task<IActionResult> KarticaPdf(string firmaId, DateTime Od, DateTime Do)
        {
            var vm = await BuildKartica(firmaId, Od, Do);

            return new Rotativa.AspNetCore.ViewAsPdf("KarticaPdf", vm)
            {
                FileName = $"Kartica_{vm.Firma.Name}_{Od:yyyyMMdd}_{Do:yyyyMMdd}.pdf"
            };
        }

        public async Task<IActionResult> KarticaExcel(string firmaId, DateTime od, DateTime doD)
        {
            var vm = await BuildKartica(firmaId, od, doD);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Картица");

            // Заглавија
            ws.Cell(1, 1).Value = "Датум";
            ws.Cell(1, 2).Value = "Опис";
            ws.Cell(1, 3).Value = "Должи";
            ws.Cell(1, 4).Value = "Платено";
            ws.Cell(1, 5).Value = "Салдо";

            ws.Range(1, 1, 1, 5).Style.Font.Bold = true;

            int row = 2;

            foreach (var s in vm.Stavki)
            {
                ws.Cell(row, 1).Value = s.Datum;
                ws.Cell(row, 1).Style.DateFormat.Format = "dd.MM.yyyy";

                ws.Cell(row, 2).Value = s.Opis;
                ws.Cell(row, 3).Value = s.Dolzi;
                ws.Cell(row, 4).Value = s.Plateno;
                ws.Cell(row, 5).Value = s.Saldo;

                row++;
            }

            // Тотали
            row++;
            ws.Cell(row, 2).Value = "Вкупно:";
            ws.Cell(row, 3).Value = vm.VkupnoDolzi;
            ws.Cell(row, 4).Value = vm.VkupnoPlateno;
            ws.Cell(row, 2).Style.Font.Bold = true;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Kartica_{vm.Firma.Name}_{od:yyyyMMdd}_{doD:yyyyMMdd}.xlsx"
            );
        }

    }
}
