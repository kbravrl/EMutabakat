using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMutabakat.Models
{
    public class Kullanici
    {
        [Key]
        [Required(ErrorMessage = "Kullanıcı ID zorunludur.")]
        public string KullaniciId { get; set; } = string.Empty;

        [NotMapped]
        public string OriginalKullaniciId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad zorunludur.")]
        [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir.")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad zorunludur.")]
        [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir.")]
        public string KullaniciSoyadi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mail zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir mail adresi giriniz.")]
        [StringLength(100, ErrorMessage = "Mail en fazla 100 karakter olabilir.")]
        public string KullaniciMail { get; set; } = string.Empty;

        [RegularExpression(@"^05\d{9}$", ErrorMessage = "GSM numarası 05XXXXXXXXX formatında olmalıdır.")]
        public string? KullaniciGsm { get; set; }

        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string? Sifre { get; set; }

        [Required(ErrorMessage = "Aktif/Pasif bilgisi zorunludur.")]
        [RegularExpression("^[01]$", ErrorMessage = "Durum değeri yalnızca 0 veya 1 olabilir.")]
        public string KullaniciAktifPasif { get; set; } = "1";

        public bool IsSeedUser { get; set; }

        public KullaniciYetki Yetkileri { get; set; } = new();

        public ICollection<Firma> Firmalar { get; set; } = new List<Firma>();

        [NotMapped]
        [MinLength(1, ErrorMessage = "En az bir firma seçilmelidir.")]
        public List<int> FirmaIds { get; set; } = new();
    }
}