namespace Diplomska.ViewModels
{
    public class FirmaGodinaSporedbaViewModel
    {
        public List<int> Godini { get; set; }    
        public List<FirmaGodinaSporedbaItemViewModel> Items { get; set; } = new();
    }
}
