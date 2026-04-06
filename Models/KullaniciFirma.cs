using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMutabakat.Models
{
    public class KullaniciFirma
    {
        [Key]
        public int KullaniciFirmaId { get; set; }

        [ForeignKey(nameof(Kullanici))]
        public int KullaniciId { get; set; }

        [ForeignKey(nameof(Firma))]
        public int FirmaId { get; set; }

        public Kullanici? Kullanici { get; set; }
        public Firma? Firma { get; set; }
    }
}
