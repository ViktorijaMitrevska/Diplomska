using Diplomska.Models;

namespace Diplomska.ViewModels
{
    public class FakturiByFirmaViewModel
    {
        public Firma Firma { get; set; }
        public PagedResultViewModel<VleznaFaktura> Fakturi {  get; set; }
        public decimal VkupenDolg {  get; set; }
    }
}
