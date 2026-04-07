using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMutabakat.Models
{
    public class Mutabakat
    {
        [Key]
        public int MutabakatId { get; set; }

        [Required(ErrorMessage = "Mutabakat dönemi zorunludur.")]
        public DateTime MutabakatDonemi { get; set; }

        [Required(ErrorMessage = "Mutabakat tipi zorunludur.")]
        public int MutabakatTipi { get; set; }

        [ForeignKey("Firma")]
        [Required(ErrorMessage = "Firma seçimi zorunludur.")]
        public int FirmaId { get; set; }

        [ForeignKey("Cari")]
        [Required(ErrorMessage = "Cari seçimi zorunludur.")]
        public string CariId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Döviz kodu zorunludur.")]
        public int MutabakatDovizKodu { get; set; }

        [Required(ErrorMessage = "Bakiye zorunludur.")]
        public decimal MutabakatBakiye { get; set; }

        [Required(ErrorMessage = "Bakiye tipi zorunludur.")]
        public string MutabakatBakiyeTipi { get; set; } = string.Empty;

        public string? MutabakatAciklama { get; set; }

        [Required(ErrorMessage = "Gönderim Taih saati  zorunludur.")]
        public DateTime MutabakatGonderimTarihSaat { get; set; }

        [Required(ErrorMessage = "Gönderim durumu zorunludur.")]
        public int MutabakatGonderimDurumu { get; set; }

        public DateTime? MutabakatCevapTarihSaat { get; set; }

        public string? MutabakatCevapMail { get; set; }

        public string? MutabakatCevapAdSoyad { get; set; }

        public string? MutabakatCevapGsm { get; set; }

        public string? MutabakatCevapAciklama { get; set; }

        [Required(ErrorMessage = "Durum zorunludur.")]
        public int MutabakatDurum { get; set; }

        public string MutabakatToken { get; set; } = string.Empty;

        public string? MutabakatReceiveStoragePath { get; set; }

        public Firma? Firma { get; set; }
        public Cari? Cari { get; set; }
    }
}