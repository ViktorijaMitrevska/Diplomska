using Google.Cloud.Firestore;

namespace Diplomska.Models
{
    [FirestoreData]
    public class VleznaFaktura
    {
        [FirestoreDocumentId]
        public string? FakturaId { get; set; }
        [FirestoreProperty]
        public int? Market { get; set; }
        [FirestoreProperty]
        public int BrojNaDokument { get; set; }
        [FirestoreProperty]
        public double Suma { get; set; }
        [FirestoreProperty]
        public bool DaliEPlateno { get; set; }
        [FirestoreProperty]
        public DateTime? PaymentDate { get; set; }
        [FirestoreProperty]
        public string? FirmaId { get; set; }
        public Firma? Firma { get; set; }
        [FirestoreProperty]
        public DateTime PriemDate { get; set; } // koga e primena fakturata
        [FirestoreProperty]
        public int Godina { get; set; } 
    }
}
