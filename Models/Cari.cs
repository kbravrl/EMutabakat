using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMutabakat.Models
{
    public class Cari
    {
        [Required(ErrorMessage = "Cari ID zorunludur.")]
        public string CariId { get; set; } = string.Empty;

        [ForeignKey("Firma")]
        [Required(ErrorMessage = "Firma seçimi zorunludur.")]
        public int FirmaId { get; set; }

        [Required(ErrorMessage = "Cari adı zorunludur.")]
        public string CariAdi { get; set; } = string.Empty;

        public string? CariUnvan { get; set; }
        public string? CariAdres { get; set; }
        public string? CariIlce { get; set; }
        public string? CariIl { get; set; }

        [Required(ErrorMessage = "Vergi dairesi zorunludur.")]
        public string CariVergiDairesi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vergi numarası zorunludur.")]
        public string CariVergiNumarasi { get; set; } = string.Empty;

        public string? CariWebAdresi { get; set; }
        public string? CariYetkiliAdiSoyadi { get; set; }
        public string? CariYetkiliTelefon { get; set; }
        public string? CariYetkiliGsm { get; set; }

        [Required(ErrorMessage = "Yetkili mail zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir mail adresi giriniz.")]
        public string CariYetkiliMail { get; set; } = string.Empty;

        [ForeignKey("CariGrup")]
        [Required(ErrorMessage = "Cari grup seçimi zorunludur.")]
        public string CariGrupId { get; set; } = string.Empty;

        [ForeignKey("DovizKodu")]
        public string CariDovizKodu { get; set; } = "TL";

        [Required(ErrorMessage = "Aktif/Pasif bilgisi zorunludur.")]
        public int CariAktifPasif { get; set; } = 1;

        public Firma? Firma { get; set; }
        public CariGrup? CariGrup { get; set; }
        public DovizKodu? DovizKodu { get; set; }

        [NotMapped]
        public string OriginalCariId { get; set; } = string.Empty;

        [NotMapped]
        public int OriginalFirmaId { get; set; }
    }
}