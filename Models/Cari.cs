using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMutabakat.Models
{
    public class Cari
    {
        [Key]
        public int CariId { get; set; }

        [ForeignKey("Firma")]
        public int FirmaId { get; set; }

        public string CariAdi { get; set; }
        public string CariUnvan { get; set; }
        public string CariAdres { get; set; }
        public string CariIlce { get; set; }
        public string CariIl { get; set; }
        public string CariVergiDairesi { get; set; }
        public string CariVergiNumarasi { get; set; }
        public string CariWebAdresi { get; set; }
        public string CariYetkiliAdiSoyadi { get; set; }
        public string CariYetkiliTelefon { get; set; }
        public string CariYetkiliGsm { get; set; }
        public string CariYetkiliMail { get; set; }

        [ForeignKey("CariGrup")]
        public int CariGrupId { get; set; }

        public int CariDovizKodu { get; set; }
        public int CariAktifPasif { get; set; }

        public Firma Firma { get; set; }
        public CariGrup CariGrup { get; set; }
    }
}