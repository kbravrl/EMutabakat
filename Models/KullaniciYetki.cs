using System.ComponentModel.DataAnnotations;

namespace EMutabakat.Models
{
    public class KullaniciYetki
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int KullaniciId { get; set; }

        public YetkiSeviyesi Cariler { get; set; } = YetkiSeviyesi.Yetkisiz;
        public YetkiSeviyesi CariGruplar { get; set; } = YetkiSeviyesi.Yetkisiz;
        public YetkiSeviyesi DovizKodlari { get; set; } = YetkiSeviyesi.Yetkisiz;
        public YetkiSeviyesi Mutabakatlar { get; set; } = YetkiSeviyesi.Yetkisiz;
        public YetkiSeviyesi Firmalar { get; set; } = YetkiSeviyesi.Yetkisiz;
        public YetkiSeviyesi Kullanicilar { get; set; } = YetkiSeviyesi.Yetkisiz;
        public bool LogYetki { get; set; }
        public bool MutabakatMailYetki { get; set; }
        public bool ImportYetki { get; set; }
        public bool ExportYetki { get; set; }
        public Kullanici? Kullanici { get; set; }
    }
}
