using Google.Cloud.Firestore;

namespace Diplomska.Models
{
    [FirestoreData]
    public class Notification
    {
        [FirestoreDocumentId]
        public string NotificationId { get; set; }
        [FirestoreProperty]
        public string Title { get; set; }
        [FirestoreProperty]
        public string Message { get; set; }
        [FirestoreProperty]

        public DateTime CreatedAt { get; set; }
        [FirestoreProperty]
        public bool IsRead { get; set; }
        [FirestoreProperty]
        public string? RelatedFirmaId { get; set; }
    }
}
