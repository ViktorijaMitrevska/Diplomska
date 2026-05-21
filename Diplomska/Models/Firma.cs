using Google.Cloud.Firestore;

namespace Diplomska.Models
{
    [FirestoreData]
    public class Firma
    {
        [FirestoreDocumentId]
        public string? FirmaId { get; set; }

        [FirestoreProperty]
        public string? Name { get; set; }

        [FirestoreProperty]
        public string? TransakciskaSmetka { get; set; }
        public ICollection<VleznaFaktura>? Fakturi { get; set; }

    }
}
