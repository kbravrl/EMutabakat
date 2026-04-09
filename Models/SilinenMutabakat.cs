using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMutabakat.Models
{
    public class SilinenMutabakat
    {
        [Key]
        public int Id { get; set; }

        public string MutabakatId { get; set; } = string.Empty;

        [Required]
        public DateTime MutabakatTarihi { get; set; }

        [Required]
        [ForeignKey("Firma")]
        public int FirmaId { get; set; }

        [Required]
        public string CariId { get; set; } = string.Empty;

        [Required]
        [ForeignKey("DovizKodu")]
        public string MutabakatDovizKodu { get; set; } = "TL";

        [Required]
        public decimal MutabakatBakiye { get; set; }

        [Required]
        public string MutabakatBakiyeTipi { get; set; } = string.Empty;

        public string? MutabakatAciklama { get; set; }

        public DateTime? MutabakatGonderimTarihSaat { get; set; }

        public int MutabakatGonderimDurumu { get; set; }

        public DateTime? MutabakatCevapTarihSaat { get; set; }

        public string? MutabakatCevapMail { get; set; }

        public string? MutabakatCevapAdSoyad { get; set; }

        public string? MutabakatCevapGsm { get; set; }

        public string? MutabakatCevapAciklama { get; set; }

        public int MutabakatDurum { get; set; }

        public string? MutabakatToken { get; set; }

        public string? MutabakatReceiveStoragePath { get; set; }

        public DateTime SilinmeTarihi { get; set; } = DateTime.UtcNow;

        public Firma? Firma { get; set; }
        public Cari? Cari { get; set; }
        public DovizKodu? DovizKodu { get; set; }
    }
}