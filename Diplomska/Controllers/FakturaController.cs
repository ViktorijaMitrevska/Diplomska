using Diplomska.Models;
using Diplomska.Services;
using Diplomska.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.Diagnostics;
using System.Linq;

namespace Diplomska.Controllers
{
    [Authorize]
    public class FakturaController : Controller
    {
        private readonly FirestoreService _firestore;

        public FakturaController(FirestoreService firestore)
        {
            _firestore = firestore;
        }

        public async Task<IActionResult> Index(string searchString, int? godina, string? firmaId, int page = 1)
        {
            int selectedYear = godina ?? DateTime.Now.Year;
            const int pageSize = 30;

            var fakturi = await _firestore.GetFakturaAsync();
            var firmi = await _firestore.GetFirmaAsync();

            foreach (var faktura in fakturi)
            {
                if (!string.IsNullOrEmpty(faktura.FirmaId))
                {
                    faktura.Firma = firmi.FirstOrDefault(f => f.FirmaId == faktura.FirmaId);
                }
            }

            var saldo = fakturi
                    .Where(f => !f.DaliEPlateno && f.Godina < DateTime.Now.Year)
                    .ToList();

            decimal saldoIznos = saldo.Sum(f => (decimal)f.Suma);

            var filtered = fakturi
                .Where(f => f.Godina == selectedYear);

            if (!string.IsNullOrEmpty(firmaId))
            {
                filtered = filtered.Where(f => f.FirmaId == firmaId);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                filtered = filtered.Where(f =>
                    f.BrojNaDokument.ToString().Contains(searchString) ||
                    (f.FirmaId != null && f.FirmaId.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (f.Firma != null && f.Firma.Name != null && f.Firma.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                );
            }


            filtered = filtered
                .OrderBy(f => f.Market ?? int.MaxValue)   
                .ThenByDescending(f => f.PriemDate);     


            var totalItems = filtered.Count();


            var items = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new PagedResultViewModel<VleznaFaktura>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            ViewBag.FirmiList = firmi;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedFirmaId = firmaId;
            ViewBag.Firmi = new SelectList(firmi, "FirmaId", "Name", firmaId);
            
            ViewBag.Saldo = saldoIznos;
            return View(vm);
        }
        public async Task<IActionResult> Neplateni(string searchString, int? godina, string? firmaId, int page = 1)
        {
            int selectedYear = godina ?? DateTime.Now.Year;
            const int pageSize = 30;

            var neplateni = await _firestore.GetNeplateniAsync();
            var firmi = await _firestore.GetFirmaAsync();

            foreach (var faktura in neplateni)
            {
                if (faktura != null && !string.IsNullOrEmpty(faktura.FirmaId))
                {
                    faktura.Firma = firmi.FirstOrDefault(f => f.FirmaId == faktura.FirmaId);
                }
            }

            var saldo = neplateni
                    .Where(f => !f.DaliEPlateno && f.Godina < DateTime.Now.Year)
                    .ToList();

            decimal saldoIznos = saldo.Sum(f => (decimal)f.Suma);

            var filtered = neplateni
                .Where(f => f != null && !f.DaliEPlateno && f.Godina == selectedYear);

            if (!string.IsNullOrEmpty(firmaId))
            {
                filtered = filtered.Where(f => f.FirmaId == firmaId);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                filtered = filtered.Where(f =>
                    f.BrojNaDokument.ToString().Contains(searchString) ||
                    (f.FirmaId != null && f.FirmaId.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (f.FakturaId != null && f.FakturaId.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (f.Firma != null && f.Firma.Name != null && f.Firma.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                );
            }


            filtered = filtered
                .OrderBy(f => f.Market ?? int.MaxValue)
                .ThenByDescending(f => f.PriemDate);

            var totalItems = filtered.Count();


            var items = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new PagedResultViewModel<VleznaFaktura>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };


            ViewBag.SelectedYear = selectedYear;
            ViewBag.FirmiList = firmi;
            ViewBag.SelectedFirmaId = firmaId;
            ViewBag.Firmi = new SelectList(firmi, "FirmaId", "Name", firmaId);
            ViewBag.Saldo = saldoIznos;
            return View("Index", vm);
        }
        public async Task<IActionResult> Saldo()
        {
            var fakturi = await _firestore.GetFakturaAsync();
            var firmi = await _firestore.GetFirmaAsync();

            foreach (var faktura in fakturi)
            {
                if (!string.IsNullOrEmpty(faktura.FirmaId))
                {
                    faktura.Firma = firmi.FirstOrDefault(f => f.FirmaId == faktura.FirmaId);
                }
            }

            var saldo = fakturi
                .Where(f => !f.DaliEPlateno && f.Godina < DateTime.Now.Year)
                .ToList();

            return View(saldo);
        }
        
        

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var firmi = await _firestore.GetFirmaAsync();
            ViewBag.FirmaId = new SelectList(firmi, "FirmaId", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(VleznaFaktura faktura)
        {
            foreach (var entry in ModelState)
            {
                foreach (var error in entry.Value.Errors)
                {
                    System.Diagnostics.Debug.WriteLine($"Key: {entry.Key}, Error: {error.ErrorMessage}");
                }
            }

            if (!ModelState.IsValid)
            {
                var firmi = await _firestore.GetFirmaAsync();
                ViewBag.FirmaId = new SelectList(firmi, "FirmaId", "Name");
                return View(faktura);
            }

            faktura.PriemDate = DateTime.SpecifyKind(
                DateTime.Now,
                DateTimeKind.Utc
            );

            faktura.Godina = faktura.PriemDate.Year;

            await _firestore.AddFakturaAsync(faktura);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var faktura = await _firestore.GetFakturaByIdAsync(id);
            if (faktura == null)
            {
                return NotFound();
            }

            var firmi = await _firestore.GetFirmaAsync();
            ViewBag.FirmaId = new SelectList(firmi, "FirmaId", "Name", faktura.FirmaId);
            
            return View(faktura);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, VleznaFaktura faktura)
        {
            if (id != faktura.FakturaId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var firmi = await _firestore.GetFirmaAsync();
                ViewBag.FirmaId = new SelectList(firmi, "FirmaId", "Name", faktura.FirmaId);
                return View(faktura);
            }

            try
            {
                var existingFaktura = await _firestore.GetFakturaByIdAsync(id);
                if (existingFaktura == null)
                {
                    return NotFound();
                }

                faktura.PriemDate = DateTime.SpecifyKind(existingFaktura.PriemDate, DateTimeKind.Utc);

                faktura.Godina = existingFaktura.Godina;

                if (faktura.PaymentDate.HasValue)
                {
                    faktura.PaymentDate = DateTime.SpecifyKind(faktura.PaymentDate.Value, DateTimeKind.Utc);
                }

                await _firestore.UpdateFakturaAsync(faktura);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Грешка при ажурирање: {ex.Message}");
                var firmi = await _firestore.GetFirmaAsync();
                ViewBag.FirmaId = new SelectList(firmi, "FirmaId", "Name", faktura.FirmaId);
                return View(faktura);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePlateno(string fakturaId, bool daliEPlateno)
        {
            if (string.IsNullOrEmpty(fakturaId))
            {
                return Json(new { success = false, error = "Faktura ID is required" });
            }

            try
            {
                await _firestore.UpdateFakturaPlatenoAsync(fakturaId, daliEPlateno);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

    }
}
