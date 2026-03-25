using System.ComponentModel.DataAnnotations;

namespace EMutabakat.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Firma seçiniz.")]
        [Range(1, int.MaxValue, ErrorMessage = "Firma seçiniz.")]
        public int FirmaId { get; set; }

        [Required(ErrorMessage = "Ad zorunludur.")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad zorunludur.")]
        public string KullaniciSoyadi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mail zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir mail giriniz.")]
        public string KullaniciMail { get; set; } = string.Empty;

        public string? KullaniciGsm { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        public string Sifre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre tekrar zorunludur.")]
        [Compare(nameof(Sifre), ErrorMessage = "Şifreler eşleşmiyor.")]
        public string SifreTekrar { get; set; } = string.Empty;
    }
}