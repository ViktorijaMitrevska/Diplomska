using Diplomska.Services;
using Microsoft.AspNetCore.Mvc;

namespace Diplomska.ViewComponents
{
    public class NotificationBadgeViewComponent : ViewComponent
    {
        private readonly FirestoreService _firestore;

        public NotificationBadgeViewComponent(FirestoreService firestore)
        {
            _firestore = firestore;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int unreadCount = await _firestore.GetUnreadNotificationsCountAsync();
            return View(unreadCount);
        }
    }
}
