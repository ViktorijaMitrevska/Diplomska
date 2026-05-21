using Google.Cloud.Firestore;
using Diplomska.Models;

namespace Diplomska.Services
{ 
    public class FirestoreService
    {
        private readonly FirestoreDb _db;
        public FirestoreService()
        {
            _db = FirestoreDb.Create("diplomska-b564a");
        }

        public async Task AddFirmaAsync(Firma firma)
        {
            var docRef = _db.Collection("Firmi").Document();
            firma.FirmaId = docRef.Id;
            await docRef.SetAsync(firma);
        }

        public async Task<List<Firma>> GetFirmaAsync()
        {
            QuerySnapshot snapshot = await _db.Collection("Firmi").GetSnapshotAsync();
            return snapshot.Documents.Select(d => d.ConvertTo<Firma>()).ToList();
        }

        public async Task<Firma> GetFirmaByIdAsync(string id)
        {
            DocumentReference docRef = _db.Collection("Firmi").Document(id);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<Firma>() : null;
        }

        public async Task AddFakturaAsync(VleznaFaktura faktura)
        {
            var docRef = _db.Collection("Fakturi").Document();
            faktura.FakturaId = docRef.Id;
            await docRef.SetAsync(faktura);
        }

        public async Task<List<VleznaFaktura>> GetFakturaAsync()
        {
            QuerySnapshot snapshot = await _db.Collection("Fakturi").GetSnapshotAsync();
            return snapshot.Documents.Select(d => d.ConvertTo<VleznaFaktura>()).ToList();
        }

        public async Task<List<VleznaFaktura>> GetFakturaByFirmaAsync(string firmaId)
        {
            QuerySnapshot snapshot = await _db.Collection("Fakturi").WhereEqualTo("FirmaId", firmaId).GetSnapshotAsync();
            return snapshot.Documents.Select(d => d.ConvertTo<VleznaFaktura>()).ToList();
        }

        public async Task<VleznaFaktura> GetFakturaByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            DocumentReference docRef = _db.Collection("Fakturi").Document(id);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<VleznaFaktura>() : null;
        }

        public async Task<List<VleznaFaktura>> GetNeplateniAsync()
        {
            QuerySnapshot snapshot = await _db.Collection("Fakturi").WhereEqualTo("DaliEPlateno", false).GetSnapshotAsync();

            return snapshot.Documents.Select(d => d.ConvertTo<VleznaFaktura>()).ToList();
        }

        public async Task UpdateFakturaAsync(VleznaFaktura faktura)
        {
            if (string.IsNullOrEmpty(faktura.FakturaId))
            {
                throw new ArgumentException("FakturaId cannot be null or empty", nameof(faktura));
            }

            var docRef = _db.Collection("Fakturi").Document(faktura.FakturaId);
            
            var snapshot = await docRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                throw new InvalidOperationException($"Faktura with ID {faktura.FakturaId} does not exist");
            }

            await docRef.SetAsync(faktura);
        }

        public async Task UpdateFakturaPlatenoAsync(string fakturaId, bool daliEPlateno, DateTime? paymentDate = null)
        {
            if (string.IsNullOrEmpty(fakturaId))
            {
                throw new ArgumentException("FakturaId cannot be null or empty", nameof(fakturaId));
            }

            var docRef = _db.Collection("Fakturi").Document(fakturaId);
            
            var snapshot = await docRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                throw new InvalidOperationException($"Faktura with ID {fakturaId} does not exist");
            }

            var updates = new Dictionary<string, object>
            {
                { "DaliEPlateno", daliEPlateno }
            };
            
            if (paymentDate.HasValue)
            {
                updates["PaymentDate"] = paymentDate.Value;
            }
            else if (daliEPlateno)
            {
                updates["PaymentDate"] = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            }

            await docRef.UpdateAsync(updates);
        }

        public async Task AddNotificationAsync(Notification notification)
        {
            var docRef = await _db.Collection("Notifications").AddAsync(notification);
        }

        public async Task<List<Notification>> GetNotificationsAsync()
        {
            var snapshot = await _db.Collection("Notifications")
                .OrderByDescending("CreatedAt")
                .GetSnapshotAsync();

            return snapshot.Documents
                .Select(d => d.ConvertTo<Notification>())
                .ToList();
        }

        public async Task MarkNotificationAsRead(string id)
        {
            await _db.Collection("Notifications")
                .Document(id)
                .UpdateAsync("IsRead", true);
        }

        public async Task<int> GetUnreadNotificationsCountAsync()
        {
            var snapshot = await _db.Collection("Notifications")
                .WhereEqualTo("IsRead", false)
                .GetSnapshotAsync();

            return snapshot.Count;
        }

        


    }
}
