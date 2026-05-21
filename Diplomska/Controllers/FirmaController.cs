using Diplomska.Models;
using Diplomska.Services;
using Diplomska.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Diplomska.Controllers
{
    [Authorize]
    public class FirmaController : Controller
    {
        private readonly FirestoreService _firestore;

        public FirmaController( FirestoreService firestore)
        {
            _firestore = firestore;
        }


        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            const int pageSize = 10;

            var firmi = await _firestore.GetFirmaAsync();

            if (!string.IsNullOrEmpty(searchString))
            {
                firmi = firmi
                    .Where(f => f.Name != null &&
                                f.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // 1️⃣ Прво креираме листа со долгови за СИТЕ фирми
            var allItems = new List<FirmaListItemViewModel>();

            foreach (var firma in firmi)
            {
                var fakturi = await _firestore.GetFakturaByFirmaAsync(firma.FirmaId);

                decimal vkupenDolg = PresmetajVkupenDolg(fakturi);

                allItems.Add(new FirmaListItemViewModel
                {
                    FirmaId = firma.FirmaId,
                    Name = firma.Name,
                    Smetka = firma.TransakciskaSmetka,
                    VkupenDolg = vkupenDolg
                });
            }

            // 2️⃣ ГЛОБАЛНО сортирање
            allItems = allItems
                .OrderByDescending(f => f.VkupenDolg > 0)
                .ThenByDescending(f => f.VkupenDolg)
                .ThenBy(f => f.Name)
                .ToList();

            var totalItems = allItems.Count;

            // 3️⃣ Потоа пагинација
            var pagedItems = allItems
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new PagedResultViewModel<FirmaListItemViewModel>
            {
                Items = pagedItems,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return View(vm);
        }



        [HttpGet]
        public async Task<IActionResult> Create()
        {

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Firma firma)
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
                return View(firma);
            }
            

            await _firestore.AddFirmaAsync(firma);
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> FakturaByFirma(string id, int page = 1)
        {
            var firma = await _firestore.GetFirmaByIdAsync(id);

            if (firma == null)
            {
                return NotFound();
            }

            const int pageSize = 20;

            var fakturi = await _firestore.GetFakturaByFirmaAsync(id);

            fakturi = fakturi
                .OrderByDescending(f => f.PriemDate)
                .ToList();

            var totalItems = fakturi.Count;

            var items = fakturi
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new FakturiByFirmaViewModel
            {
                Firma = firma,

                VkupenDolg = PresmetajVkupenDolg(fakturi), 

                Fakturi = new PagedResultViewModel<VleznaFaktura>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
                }
            };

            return View(vm);
        }
        
        private decimal PresmetajVkupenDolg(List<VleznaFaktura> fakturi)
        {
            return fakturi
                .Where(f => !f.DaliEPlateno)
                .Sum(f => (decimal)f.Suma);
        }
    }
}
