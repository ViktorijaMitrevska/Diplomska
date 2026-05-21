using Diplomska.Models;
using Diplomska.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diplomska.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly FirestoreService _firestore;

        public NotificationController(FirestoreService firestore)
        {
            _firestore = firestore;
        }
        public async Task<IActionResult> Test()
        {
            await _firestore.AddNotificationAsync(new Notification
            {
                Title = "Месечен извештаj",
                Message = "Имате неплатени фактури од предходниот месец.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });

            return Content("OK");
        }
       

        public async Task<IActionResult> TestYearlyNotification()
        {
            await _firestore.AddNotificationAsync(new Notification
            {
                Title = "Годишен извештај",
                Message = "Имате неплатени фактури од предходната година.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
  
            });

            return Content("OK");
        }


        public async Task<IActionResult> Index()
        {
            var notifications = await _firestore.GetNotificationsAsync();
            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            await _firestore.MarkNotificationAsRead(id);
            return RedirectToAction("Index");
        }
    }
}
