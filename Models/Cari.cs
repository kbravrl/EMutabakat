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
        [StringLength(200, ErrorMessage = "Cari adı en fazla 200 karakter olabilir.")]
        public string CariAdi { get; set; } = string.Empty;

        public string? CariUnvan { get; set; }
        public string? CariAdres { get; set; }
        public string? CariIlce { get; set; }
        public string? CariIl { get; set; }

        [Required(ErrorMessage = "Vergi dairesi zorunludur.")]
        [StringLength(150, ErrorMessage = "Vergi dairesi en fazla 150 karakter olabilir.")]
        public string CariVergiDairesi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vergi numarası zorunludur.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Vergi numarası 10 haneli olmalıdır.")]
        public string CariVergiNumarasi { get; set; } = string.Empty;

        [Url(ErrorMessage = "Geçerli bir web adresi giriniz.")]
        [StringLength(250, ErrorMessage = "Web adresi en fazla 250 karakter olabilir.")]
        public string? CariWebAdresi { get; set; }

        [StringLength(200, ErrorMessage = "Yetkili adı soyadı en fazla 200 karakter olabilir.")]
        public string? CariYetkiliAdiSoyadi { get; set; }

        [RegularExpression(@"^0\d{10}$", ErrorMessage = "Telefon 0 ile başlamalı ve 11 haneli olmalıdır.")]
        public string? CariYetkiliTelefon { get; set; }

        [RegularExpression(@"^05\d{9}$", ErrorMessage = "GSM numarası 05 ile başlamalı ve 11 haneli olmalıdır.")]
        public string? CariYetkiliGsm { get; set; }

        [Required(ErrorMessage = "Yetkili mail zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir mail adresi giriniz.")]
        [StringLength(200, ErrorMessage = "Mail adresi en fazla 200 karakter olabilir.")]
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