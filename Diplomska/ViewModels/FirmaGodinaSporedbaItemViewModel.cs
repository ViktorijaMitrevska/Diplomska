namespace Diplomska.ViewModels
{
    public class FirmaGodinaSporedbaItemViewModel
    {
        public string FirmaId { get; set; }
        public string FirmaName { get; set; }
        public Dictionary<int, (int BrojFakturi, decimal VkupenIznos)> GodiniData { get; set; } = new();
    }
}
