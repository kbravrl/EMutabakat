using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace EMutabakat.Models
{
    public class Kullanici
    {
        [Key]
        [Required(ErrorMessage = "Kullanıcı ID zorunludur.")]
        public string KullaniciId { get; set; } = string.Empty;

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

        [Required(ErrorMessage = "Rol seçimi zorunludur.")]
        public string Rol { get; set; } = KullaniciRolleri.Standart;

        [Required(ErrorMessage = "Aktif/Pasif bilgisi zorunludur.")]
        public string KullaniciAktifPasif { get; set; } = "Aktif";

        public Firma? Firma { get; set; }
        public ICollection<Firma> Firmalar { get; set; } = new List<Firma>();

        [NotMapped]
        public List<int> FirmaIds { get; set; } = new();
    }
} 