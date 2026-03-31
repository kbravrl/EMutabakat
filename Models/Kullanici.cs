using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMutabakat.Models
{
    public class Kullanici
    {
        [Key]
        public int KullaniciId { get; set; }

        [Required(ErrorMessage = "Firma seçimi zorunludur.")]
        [ForeignKey("Firma")]
        public int FirmaId { get; set; }

        [Required(ErrorMessage = "Ad zorunludur.")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad zorunludur.")]
        public string KullaniciSoyadi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mail zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir mail adresi giriniz.")]
        public string KullaniciMail { get; set; } = string.Empty;

        public string? KullaniciGsm { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        public string Sifre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Aktif/Pasif bilgisi zorunludur.")]
        public string KullaniciAktifPasif { get; set; }

        public Firma? Firma { get; set; }
    }
}