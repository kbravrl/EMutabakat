using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMutabakat.Models
{
    public class Kullanici
    {
        [Key]
        public int KullaniciId { get; set; }

        [ForeignKey("Firma")]
        public int FirmaId { get; set; }

        public string KullaniciAdi { get; set; }
        public string KullaniciSoyadi { get; set; }
        public string KullaniciMail { get; set; }
        public string KullaniciGsm { get; set; }
        public string KullaniciAktifPasif { get; set; }

        public Firma Firma { get; set; }
    }
}