namespace Diplomska.ViewModels
{
    public class IzvestajDashboardViewModel
    {
        public string Tip { get; set; }   // godishno, polugodishno, kvartal, kvartalNizGodini...
        public int? Godina { get; set; }
        public int? Godina2 { get; set; }
        public int? Kvartal { get; set; }

        public List<int> DostapniGodini { get; set; }

        // Податоци за прикажување
        public Dictionary<string, List<FirmaIzvestajItemViewModel>> Rezultati { get; set; }
    }
}
