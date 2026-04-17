using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace EMutabakat.Models
{
    public class Firma
    {
        [Key]
        public int FirmaId { get; set; }

        [Required(ErrorMessage = "Firma adı zorunludur.")]
        [StringLength(200, ErrorMessage = "Firma adı en fazla 200 karakter olabilir.")]
        public string FirmaAdi { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "Firma unvanı en fazla 300 karakter olabilir.")]
        public string? FirmaUnvan { get; set; }

        [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
        public string? FirmaAdres { get; set; }

        [StringLength(100, ErrorMessage = "İlçe en fazla 100 karakter olabilir.")]
        public string? FirmaIlce { get; set; }

        [StringLength(100, ErrorMessage = "İl en fazla 100 karakter olabilir.")]
        public string? FirmaIl { get; set; }

        [Required(ErrorMessage = "Vergi dairesi zorunludur.")]
        [StringLength(150, ErrorMessage = "Vergi dairesi en fazla 150 karakter olabilir.")]
        public string FirmaVergiDairesi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vergi numarası zorunludur.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Vergi numarası 10 haneli olmalıdır.")]
        public string FirmaVergiNumarasi { get; set; } = string.Empty;

        [RegularExpression(@"^\d{16}$", ErrorMessage = "MERSİS numarası 16 haneli olmalıdır.")]
        public string? FirmaMersisNumarasi { get; set; }

        [Url(ErrorMessage = "Geçerli bir web adresi giriniz.")]
        [StringLength(250, ErrorMessage = "Web adresi en fazla 250 karakter olabilir.")]
        public string? FirmaWebAdresi { get; set; }

        [Required(ErrorMessage = "Yetkili adı soyadı zorunludur.")]
        [StringLength(200, ErrorMessage = "Yetkili adı soyadı en fazla 200 karakter olabilir.")]
        public string FirmaYetkiliAdiSoyadi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mail adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir mail adresi giriniz.")]
        [StringLength(200, ErrorMessage = "Mail adresi en fazla 200 karakter olabilir.")]
        public string FirmaMail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon zorunludur.")]
        [RegularExpression(@"^0\d{10}$", ErrorMessage = "Telefon 0 ile başlamalı ve 11 haneli olmalıdır.")]
        public string FirmaTelefon { get; set; } = string.Empty;

        [RegularExpression(@"^05\d{9}$", ErrorMessage = "GSM numarası 05 ile başlamalı ve 11 haneli olmalıdır.")]
        public string? FirmaGsm { get; set; }

        [Required(ErrorMessage = "SMTP Host zorunludur.")]
        [StringLength(200, ErrorMessage = "SMTP Host en fazla 200 karakter olabilir.")]
        public string FirmaSmtpHost { get; set; } = string.Empty;

        [Range(1, 65535, ErrorMessage = "SMTP Port 1 ile 65535 arasında olmalıdır.")]
        public int FirmaSmtpPort { get; set; }

        [Required(ErrorMessage = "SMTP kullanıcı adı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir mail adresi giriniz.")]
        [StringLength(200, ErrorMessage = "SMTP kullanıcı adı en fazla 200 karakter olabilir.")]
        public string FirmaSmtpUser { get; set; } = string.Empty;

        [Required(ErrorMessage = "SMTP şifresi zorunludur.")]
        [StringLength(200, ErrorMessage = "SMTP şifresi en fazla 200 karakter olabilir.")]
        public string FirmaSmtpPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "SMTP Secure bilgisi zorunludur.")]
        [RegularExpression(@"^(true|false)$", ErrorMessage = "SMTP Secure değeri true veya false olmalıdır.")]
        public string FirmaSmtpSecure { get; set; } = string.Empty;

        [Range(0, 1, ErrorMessage = "Aktif/Pasif değeri 0 veya 1 olmalıdır.")]
        public int FirmaAktifPasif { get; set; } = 1;

        public ICollection<Kullanici> Kullanicilar { get; set; } = new List<Kullanici>();
    }
}