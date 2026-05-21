namespace Diplomska.ViewModels
{
    public class FirmaIzvestajViewModel
    {
        public int SelectedYear { get; set; }        
        public List<int> DostapniGodini { get; set; }
        public string Period { get; set; } 
        public List<FirmaIzvestajItemViewModel> TopFirmi { get; set; }
    }
}
