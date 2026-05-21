using ClosedXML.Excel;
using Diplomska.Models;
using Diplomska.Services;
using Diplomska.ViewModels;
using Google.Cloud.Firestore.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rotativa.AspNetCore;
using System.Diagnostics;
using System.IO;

namespace Diplomska.Controllers
{
    [Authorize]
    public class IzvestajController : Controller
    {
        private readonly FirestoreService _firestore;

        public IzvestajController(FirestoreService firestore)
        {
            _firestore = firestore;
        }
        public async Task<IActionResult> TopFirmi(string period = "godishno", int? godina = null)
        {
            var fakturi = await _firestore.GetFakturaAsync();
            var firmi = await _firestore.GetFirmaAsync();

            var godini = fakturi
                .Select(f => f.PriemDate.Year)
                .Distinct()
                .OrderByDescending(x => x)
                .ToList();

            int selectedYear = godina ?? DateTime.Now.Year;

            var filtrirani = fakturi
                .Where(f => f.PriemDate.Year == selectedYear);

            IEnumerable<IGrouping<string, VleznaFaktura>> grupirani;

            if (period == "polugodishno")
            {
                grupirani = filtrirani
                    .GroupBy(f => f.FirmaId + "_" + (f.PriemDate.Month <= 6 ? "H1" : "H2"));
            }
            else if (period == "trimesecno")
            {
                grupirani = filtrirani
                    .GroupBy(f => f.FirmaId + "_Q" + ((f.PriemDate.Month - 1) / 3 + 1));
            }
            else // godishno
            {
                grupirani = filtrirani
                    .GroupBy(f => f.FirmaId);
            }


            var report = grupirani
                .Select(g =>
                {
                    var firmaId = g.Key.Split('_')[0];
                    var firma = firmi.FirstOrDefault(x => x.FirmaId == firmaId);

                    return new FirmaIzvestajItemViewModel
                    {
                        FirmaId = firmaId,
                        FirmaName = firma?.Name ?? "Непозната",
                        BrojFakturi = g.Count(),
                        VkupenIznos = g.Sum(x => (decimal)x.Suma)
                    };
                })
                .OrderByDescending(x => x.VkupenIznos)
                .Take(5)
                .ToList();


            var vm = new FirmaIzvestajViewModel
            {
                SelectedYear = selectedYear,
                DostapniGodini = godini,
                Period = period,
                TopFirmi = report
            };

            return View(vm);
        }

        private async Task<List<FirmaIzvestajItemViewModel>> GetTopFirmiReport(string period, int selectedYear)
        {
            var fakturi = await _firestore.GetFakturaAsync();
            var firmi = await _firestore.GetFirmaAsync();

            var filtrirani = fakturi
                .Where(f => f.PriemDate.Year == selectedYear);

            IEnumerable<IGrouping<string, VleznaFaktura>> grupirani;

            if (period == "polugodishno")
            {
                grupirani = filtrirani.GroupBy(f =>
                    f.FirmaId + "_" + (f.PriemDate.Month <= 6 ? "H1" : "H2"));
            }
            else if (period == "trimesecno")
            {
                grupirani = filtrirani.GroupBy(f =>
                    f.FirmaId + "_Q" + ((f.PriemDate.Month - 1) / 3 + 1));
            }
            else
            {
                grupirani = filtrirani.GroupBy(f => f.FirmaId);
            }

            return grupirani
                .Select(g =>
                {
                    var firmaId = g.Key.Split('_')[0];
                    var firma = firmi.FirstOrDefault(x => x.FirmaId == firmaId);

                    return new FirmaIzvestajItemViewModel
                    {
                        FirmaId = firmaId,
                        FirmaName = firma?.Name ?? "Непозната",
                        BrojFakturi = g.Count(),
                        VkupenIznos = g.Sum(x => (decimal)x.Suma)
                    };
                })
                .OrderByDescending(x => x.VkupenIznos)
                .Take(5)
                .ToList();
        }

        public async Task<IActionResult> ExportTopFirmiExcel(string period = "godishno", int? godina = null)
        {
            int year = godina ?? DateTime.Now.Year;
            var data = await GetTopFirmiReport(period, year);

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Top Firmi");

            ws.Cell(1, 1).Value = "Фирма";
            ws.Cell(1, 2).Value = "Број на фактури";
            ws.Cell(1, 3).Value = "Вкупен износ";

            int row = 2;
            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = item.FirmaName;
                ws.Cell(row, 2).Value = item.BrojFakturi;
                ws.Cell(row, 3).Value = item.VkupenIznos;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"TopFirmi_{period}_{year}.xlsx");
        }

        public async Task<IActionResult> TopFirmiPdf(string period = "godishno", int? godina = null)
        {
            int year = godina ?? DateTime.Now.Year;

            var report = await GetTopFirmiReport(period, year);

            var vm = new FirmaIzvestajViewModel
            {
                SelectedYear = year,
                Period = period,
                TopFirmi = report
            };

            return new Rotativa.AspNetCore.ViewAsPdf("TopFirmiPdf", vm)
            {
                FileName = $"TopFirmi_{period}_{year}.pdf",
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                PageSize = Rotativa.AspNetCore.Options.Size.A4
            };
        }
        public async Task<IActionResult> SporedbaPoGodini(int lastNYears = 2)
        {
            var fakturi = await _firestore.GetFakturaAsync();
            var firmi = await _firestore.GetFirmaAsync();

            var godini = fakturi
                .Select(f => f.Godina)
                .Distinct()
                .OrderByDescending(x => x)
                .Take(lastNYears)
                .ToList();

            var topFirmaIds = fakturi
                .Where(f => f.Godina == godini.First())
                .GroupBy(f => f.FirmaId)
                .OrderByDescending(g => g.Sum(x => (decimal)x.Suma))
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            var items = topFirmaIds.Select(fid =>
            {
                var firma = firmi.FirstOrDefault(f => f.FirmaId == fid);
                var vmItem = new FirmaGodinaSporedbaItemViewModel
                {
                    FirmaId = fid,
                    FirmaName = firma?.Name ?? "Непозната"
                };

                foreach (var g in godini)
                {
                    var data = fakturi.Where(f => f.FirmaId == fid && f.Godina == g);
                    vmItem.GodiniData[g] = (data.Count(), data.Sum(x => (decimal)x.Suma));
                }

                return vmItem;
            }).ToList();

            var vm = new FirmaGodinaSporedbaViewModel
            {
                Godini = godini,
                Items = items
            };
            ViewBag.LastNYears = lastNYears;
            return View(vm);
        }

        public async Task<IActionResult> ExportMultiGodiniExcel(int lastNYears = 5)
        {
            var fakturi = await _firestore.GetFakturaAsync();
            var firmi = await _firestore.GetFirmaAsync();

            var godini = fakturi.Select(f => f.Godina).Distinct().OrderByDescending(x => x).Take(lastNYears).ToList();

            var topFirmaIds = fakturi
                .Where(f => f.Godina == godini.First())
                .GroupBy(f => f.FirmaId)
                .OrderByDescending(g => g.Sum(x => (decimal)x.Suma))
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            var items = topFirmaIds.Select(fid =>
            {
                var firma = firmi.FirstOrDefault(f => f.FirmaId == fid);
                var dict = new Dictionary<int, (int Broj, decimal Vkupno)>();
                foreach (var g in godini)
                {
                    var data = fakturi.Where(f => f.FirmaId == fid && f.Godina == g);
                    dict[g] = (data.Count(), data.Sum(x => (decimal)x.Suma));
                }

                return new { Firma = firma?.Name ?? "Непозната", GodiniData = dict };
            }).ToList();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Sporedba");

                ws.Cell(1, 1).Value = "Фирма";
                int col = 2;
                foreach (var g in godini)
                {
                    ws.Cell(1, col++).Value = $"{g} (Број / Сума)";
                }

                int row = 2;
                foreach (var item in items)
                {
                    ws.Cell(row, 1).Value = item.Firma;
                    col = 2;
                    foreach (var g in godini)
                    {
                        var d = item.GodiniData[g];
                        ws.Cell(row, col++).Value = $"{d.Broj} / {d.Vkupno:N2}";
                    }
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SporedbaGodini.xlsx");
                }
            }
        }

        public async Task<IActionResult> MultiGodiniPdf(int lastNYears = 5)
    {
        var fakturi = await _firestore.GetFakturaAsync();
        var firmi = await _firestore.GetFirmaAsync();

        var godini = fakturi
            .Select(f => f.Godina)
            .Distinct()
            .OrderByDescending(x => x)
            .Take(lastNYears)
            .ToList();

        var topFirmaIds = fakturi
            .Where(f => f.Godina == godini.First())
            .GroupBy(f => f.FirmaId)
            .OrderByDescending(g => g.Sum(x => (decimal)x.Suma))
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        var items = topFirmaIds.Select(fid =>
        {
            var firma = firmi.FirstOrDefault(f => f.FirmaId == fid);
            var dict = new Dictionary<int, (int Broj, decimal Vkupno)>();
            foreach (var g in godini)
            {
                var data = fakturi.Where(f => f.FirmaId == fid && f.Godina == g);
                dict[g] = (data.Count(), data.Sum(x => (decimal)x.Suma));
            }
            return new { Firma = firma?.Name ?? "Непозната", GodiniData = dict };
        }).ToList();

        var vm = new FirmaGodinaSporedbaViewModel
        {
            Godini = godini,
            Items = items.Select(x => new FirmaGodinaSporedbaItemViewModel
            {
                FirmaName = x.Firma,
                GodiniData = x.GodiniData
            }).ToList()
        };

        return new ViewAsPdf("MultiGodiniPdf", vm)
        {
            FileName = "SporedbaGodini.pdf",
            PageSize = Rotativa.AspNetCore.Options.Size.A4,
            PageOrientation = Rotativa.AspNetCore.Options.Orientation.Landscape,
            PageMargins = new Rotativa.AspNetCore.Options.Margins(10, 10, 10, 10)
        };
    }
        public async Task<IActionResult> FirmaGodini(string? firmaId, int lastYears = 2)
        {
            var fakturi = await _firestore.GetFakturaAsync();
            var firmi = await _firestore.GetFirmaAsync();

            var maxYear = fakturi.Max(f => f.PriemDate.Year);
            var godini = Enumerable.Range(maxYear - lastYears + 1, lastYears)
                                    .OrderBy(y => y)
                                    .ToList();

            if (!string.IsNullOrEmpty(firmaId))
            {
                fakturi = fakturi.Where(f => f.FirmaId == firmaId).ToList();
            }

            var rezultat = godini.Select(y => new FirmaGodiniViewModel
            {
                Godina = y,
                BrojFakturi = fakturi.Count(f => f.PriemDate.Year == y),
                VkupenIznos = fakturi.Where(f => f.PriemDate.Year == y).Sum(f => (decimal)f.Suma)
            }).ToList();

            ViewBag.FirmiList = new SelectList(firmi, "FirmaId", "Name", firmaId);
            ViewBag.LastYears = lastYears;
            ViewBag.SelectedFirmaId = firmaId;

            return View(rezultat);
        }

        public async Task<IActionResult> FirmaGodiniExcel(string? firmaId, int lastYears = 2)
        {
            var fakturi = await _firestore.GetFakturaAsync();
            var firmi = await _firestore.GetFirmaAsync();

            var maxYear = fakturi.Max(f => f.PriemDate.Year);
            var godini = Enumerable.Range(maxYear - lastYears + 1, lastYears)
                                    .OrderBy(y => y)
                                    .ToList();

            if (!string.IsNullOrEmpty(firmaId))
            {
                fakturi = fakturi.Where(f => f.FirmaId == firmaId).ToList();
            }

            var rezultat = godini.Select(y => new FirmaGodiniViewModel
            {
                Godina = y,
                BrojFakturi = fakturi.Count(f => f.PriemDate.Year == y),
                VkupenIznos = fakturi.Where(f => f.PriemDate.Year == y).Sum(f => (decimal)f.Suma)
            }).ToList();

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Извештај");
                ws.Cell(1, 1).Value = "Година";
                ws.Cell(1, 2).Value = "Број на фактури";
                ws.Cell(1, 3).Value = "Вкупен износ";

                for (int i = 0; i < rezultat.Count; i++)
                {
                    ws.Cell(i + 2, 1).Value = rezultat[i].Godina;
                    ws.Cell(i + 2, 2).Value = rezultat[i].BrojFakturi;
                    ws.Cell(i + 2, 3).Value = rezultat[i].VkupenIznos;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "FirmaGodini.xlsx");
                }
            }
        }

        public async Task<IActionResult> FirmaGodiniPdf(string? firmaId, int lastYears = 2)
        {
            var fakturi = await _firestore.GetFakturaAsync();
            var firmi = await _firestore.GetFirmaAsync();

            var maxYear = fakturi.Max(f => f.PriemDate.Year);
            var godini = Enumerable.Range(maxYear - lastYears + 1, lastYears)
                                    .OrderBy(y => y)
                                    .ToList();

            if (!string.IsNullOrEmpty(firmaId))
            {
                fakturi = fakturi.Where(f => f.FirmaId == firmaId).ToList();
            }

            var rezultat = godini.Select(y => new FirmaGodiniViewModel
            {
                Godina = y,
                BrojFakturi = fakturi.Count(f => f.PriemDate.Year == y),
                VkupenIznos = fakturi.Where(f => f.PriemDate.Year == y).Sum(f => (decimal)f.Suma)
            }).ToList();

            return new Rotativa.AspNetCore.ViewAsPdf("FirmaGodiniPdf", rezultat)
            {
                FileName = "FirmaGodini.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Landscape
            };
        }

        private List<FirmaIzvestajItemViewModel> GetDataZaPeriod(DateTime od,DateTime doD,List<VleznaFaktura> fakturi,List<Firma> firmi)
        {
            return fakturi
                .Where(f => f.PriemDate >= od && f.PriemDate < doD)
                .GroupBy(f => f.FirmaId)
                .Select(g =>
                {
                    var firma = firmi.FirstOrDefault(x => x.FirmaId == g.Key);

                    return new FirmaIzvestajItemViewModel
                    {
                        FirmaId = g.Key,
                        FirmaName = firma?.Name ?? "Непозната",
                        BrojFakturi = g.Count(),                  
                        VkupenIznos = g.Sum(x => (decimal)x.Suma) 
                    };
                })
                .OrderByDescending(x => x.VkupenIznos) 
                .Take(5)
                .ToList();
        }

 
        private (DateTime od, DateTime doD) GetKvartalPeriod(int godina, int kvartal)
        {
            int startMonth = (kvartal - 1) * 3 + 1;

            var od = new DateTime(godina, startMonth, 1);
            var doD = od.AddMonths(3);

            return (od, doD);
        }

        public async Task<IActionResult> Dashboard(
    string tip = "kvartaliVoGodina",
    int? godina = null,
    int? kvartal = null,
    string sortirajPo = "suma")
        {
            var fakturi = await _firestore.GetFakturaAsync();
            var firmi = await _firestore.GetFirmaAsync();
            var now = DateTime.Now;

            var godini = fakturi
                .Select(f => f.PriemDate.Year)
                .Distinct()
                .OrderByDescending(x => x)
                .ToList();

            var rezultati = new Dictionary<string, List<FirmaIzvestajItemViewModel>>();

            switch (tip)
            {
                // ✅ КВАРТАЛИ ВО ЕДНА ГОДИНА
                case "kvartaliVoGodina":
                    {
                        int g = godina ?? now.Year;

                        for (int q = 1; q <= 4; q++)
                        {
                            var (od, doD) = GetKvartalPeriod(g, q);
                            rezultati[$"Q{q} {g}"] =
                                SortList(GetDataZaPeriod(od, doD, fakturi, firmi), sortirajPo);
                        }
                        break;
                    }

                // ✅ ИСТ КВАРТАЛ НИЗ ГОДИНИ
                case "kvartalNizGodini":
                    {
                        int q = kvartal ?? 1;

                        foreach (var year in godini.Take(5))
                        {
                            var (od, doD) = GetKvartalPeriod(year, q);
                            rezultati[$"{year} Q{q}"] =
                                SortList(GetDataZaPeriod(od, doD, fakturi, firmi), sortirajPo);
                        }
                        break;
                    }
            }

            var vm = new IzvestajDashboardViewModel
            {
                Tip = tip,
                Godina = godina,
                Kvartal = kvartal,
                DostapniGodini = godini,
                Rezultati = rezultati
            };

            return View(vm);
        }


        private List<FirmaIzvestajItemViewModel> SortList(
    List<FirmaIzvestajItemViewModel> lista, string sortirajPo)
        {
            return sortirajPo switch
            {
                "broj" => lista.OrderByDescending(x => x.BrojFakturi).Take(5).ToList(),
                _ => lista.OrderByDescending(x => x.VkupenIznos).Take(5).ToList(),
            };
        }

        private async Task<Dictionary<string, List<FirmaIzvestajItemViewModel>>>
            GetKvartalniRezultati(string tip, int? godina, int? kvartal, string sortirajPo)
        {
            var fakturi = await _firestore.GetFakturaAsync();
            var firmi = await _firestore.GetFirmaAsync();
            var now = DateTime.Now;

            var godini = fakturi
                .Select(f => f.PriemDate.Year)
                .Distinct()
                .OrderByDescending(x => x)
                .ToList();

            var rezultati = new Dictionary<string, List<FirmaIzvestajItemViewModel>>();

            switch (tip)
            {
                case "kvartaliVoGodina":
                    {
                        int g = godina ?? now.Year;

                        for (int q = 1; q <= 4; q++)
                        {
                            var (od, doD) = GetKvartalPeriod(g, q);
                            rezultati[$"Q{q} {g}"] =
                                SortList(GetDataZaPeriod(od, doD, fakturi, firmi), sortirajPo);
                        }
                        break;
                    }

                case "kvartalNizGodini":
                    {
                        int q = kvartal ?? 1;

                        foreach (var year in godini.Take(5))
                        {
                            var (od, doD) = GetKvartalPeriod(year, q);
                            rezultati[$"{year} Q{q}"] =
                                SortList(GetDataZaPeriod(od, doD, fakturi, firmi), sortirajPo);
                        }
                        break;
                    }
            }

            return rezultati;
        }

        public async Task<IActionResult> ExportPdf(
    string tip,
    int? godina,
    int? kvartal,
    string sortirajPo = "suma")
        {
            var rezultati = await GetKvartalniRezultati(tip, godina, kvartal, sortirajPo);

            var vm = new IzvestajDashboardViewModel
            {
                Tip = tip,
                Godina = godina,
                Kvartal = kvartal,
                Rezultati = rezultati
            };

            return new ViewAsPdf("DashboardPdf", vm)
            {
                FileName = "Kvartalen_Izvestaj.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Landscape
            };
        }

        public async Task<IActionResult> ExportExcel(
    string tip,
    int? godina,
    int? kvartal,
    string sortirajPo = "suma")
        {
            var rezultati = await GetKvartalniRezultati(tip, godina, kvartal, sortirajPo);

            using var wb = new ClosedXML.Excel.XLWorkbook();

            foreach (var sekcija in rezultati)
            {
                var ws = wb.Worksheets.Add(sekcija.Key.Replace(" ", "_"));

                ws.Cell(1, 1).Value = "Фирма";
                ws.Cell(1, 2).Value = "Број на фактури";
                ws.Cell(1, 3).Value = "Вкупен износ";

                int row = 2;
                foreach (var f in sekcija.Value)
                {
                    ws.Cell(row, 1).Value = f.FirmaName;
                    ws.Cell(row, 2).Value = f.BrojFakturi;
                    ws.Cell(row, 3).Value = f.VkupenIznos;
                    row++;
                }

                ws.Columns().AdjustToContents();
            }

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Kvartalen_Izvestaj.xlsx"
            );
        }

    }
}
