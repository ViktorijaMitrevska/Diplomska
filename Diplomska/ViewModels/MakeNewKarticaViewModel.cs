using Diplomska.Models;
namespace Diplomska.ViewModels
{
    public class MakeNewKarticaViewModel
    {
        public string FirmaId { get; set; }
        public Firma Firma { get; set; }
        public DateTime? Od { get; set; }
        public DateTime? Do { get; set; }

        public List<Firma> Firmi { get; set; }

        public List<KarticaStavkaViewModel> Stavki { get; set; } = new();

        public double VkupnoDolzi { get; set; }
        public double VkupnoPlateno { get; set; }

        public double PeriodDolzi { get; set; }
        public double PeriodPlateno { get; set; }
        public double Saldo => VkupnoDolzi - VkupnoPlateno;
        public double SaldoPeriod { get; set; }
    }

}
