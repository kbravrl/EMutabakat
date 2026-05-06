using System.ComponentModel.DataAnnotations;

namespace EMutabakat.Models
{
    public class KullaniciYetki
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string KullaniciId { get; set; } = string.Empty;

        public YetkiSeviyesi Cariler { get; set; } = YetkiSeviyesi.Giris;
        public YetkiSeviyesi CariGruplar { get; set; } = YetkiSeviyesi.Giris;
        public YetkiSeviyesi DovizKodlari { get; set; } = YetkiSeviyesi.Giris;
        public YetkiSeviyesi Mutabakatlar { get; set; } = YetkiSeviyesi.Giris;
        public YetkiSeviyesi Firmalar { get; set; } = YetkiSeviyesi.Giris;
        public YetkiSeviyesi Kullanicilar { get; set; } = YetkiSeviyesi.Giris;
        public bool LogYetki { get; set; }
        public bool ImportYetki { get; set; }
        public bool ExportYetki { get; set; }
        public Kullanici? Kullanici { get; set; }
    }
}
