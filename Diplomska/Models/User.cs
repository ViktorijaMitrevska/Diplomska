using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace Diplomska.Models
{
    [FirestoreData]
    public class AppUser
    {
        [FirestoreDocumentId]
        public string UserId { get; set; }  

        [FirestoreProperty]
        public string Name { get; set; }

        [FirestoreProperty]
        public string Email { get; set; }

        [FirestoreProperty]
        public string Role { get; set; }    

        [FirestoreProperty]
        public string OwnerId { get; set; }  

        [FirestoreProperty]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty]
        public bool IsActive { get; set; }
    }
}
