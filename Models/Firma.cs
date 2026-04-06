using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace EMutabakat.Models
{
    public class Firma
    {
        [Key]
        public int FirmaId { get; set; }

        [Required(ErrorMessage = "Firma adı zorunludur.")]
        public string FirmaAdi { get; set; } = string.Empty;

        public string? FirmaUnvan { get; set; }
        public string? FirmaAdres { get; set; }
        public string? FirmaIlce { get; set; }
        public string? FirmaIl { get; set; }

        [Required(ErrorMessage = "Vergi dairesi zorunludur.")]
        public string FirmaVergiDairesi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vergi numarası zorunludur.")]
        public string FirmaVergiNumarasi { get; set; } = string.Empty;

        public string? FirmaMersisNumarasi { get; set; }
        public string? FirmaWebAdresi { get; set; }

        [Required(ErrorMessage = "Yetkili adı soyadı zorunludur.")]
        public string FirmaYetkiliAdiSoyadi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mail adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir mail adresi giriniz.")]
        public string FirmaMail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon zorunludur.")]
        public string FirmaTelefon { get; set; } = string.Empty;

        public string? FirmaGsm { get; set; }

        [Required(ErrorMessage = "SMTP Host zorunludur.")]
        public string FirmaSmtpHost { get; set; } = string.Empty;

        [Required(ErrorMessage = "SMTP Port zorunludur.")]
        public int FirmaSmtpPort { get; set; }

        [Required(ErrorMessage = "SMTP kullanıcı adı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir mail adresi giriniz.")]
        public string FirmaSmtpUser { get; set; } = string.Empty;

        [Required(ErrorMessage = "SMTP şifresi zorunludur.")]
        public string FirmaSmtpPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "SMTP Secure bilgisi zorunludur.")]
        public string FirmaSmtpSecure { get; set; } = string.Empty;

        [Required(ErrorMessage = "Aktif/Pasif bilgisi zorunludur.")]
        public int FirmaAktifPasif { get; set; }

        public ICollection<KullaniciFirma> KullaniciFirmalari { get; set; } = new List<KullaniciFirma>();
    }
}