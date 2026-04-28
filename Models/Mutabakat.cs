using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMutabakat.Models
{
    public class Mutabakat
    {


        [Required(ErrorMessage = "Mutabakat ID zorunludur.")]
        public string MutabakatId { get; set; } = string.Empty;

        [NotMapped]
        public string OriginalMutabakatId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mutabakat tarihi zorunludur.")]
        public DateTime MutabakatTarihi { get; set; }

        [ForeignKey("Firma")]
        [Range(1, int.MaxValue, ErrorMessage = "Firma seçimi zorunludur.")]
        public int FirmaId { get; set; }

        [ForeignKey("Cari")]
        [Required(ErrorMessage = "Cari seçimi zorunludur.")]
        public string CariId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Döviz kodu zorunludur.")]
        [ForeignKey("DovizKodu")]
        public string MutabakatDovizKodu { get; set; } = "TL";

        [Required(ErrorMessage = "Bakiye zorunludur.")]
        [Range(0, double.MaxValue, ErrorMessage = "Bakiye negatif olamaz.")]
        public decimal MutabakatBakiye { get; set; }

        [Required(ErrorMessage = "Bakiye tipi zorunludur.")]
        public string MutabakatBakiyeTipi { get; set; } = string.Empty;

        public string? MutabakatAciklama { get; set; }

        public DateTime? MutabakatGonderimTarihSaat { get; set; }

        [Required]
        public MutabakatStatus Status { get; set; } = MutabakatStatus.Kaydedildi;

        public DateTime? MutabakatCevapTarihSaat { get; set; }

        public string? MutabakatCevapMail { get; set; }

        public string? MutabakatCevapAdSoyad { get; set; }

        public string? MutabakatCevapGsm { get; set; }

        public string? MutabakatCevapAciklama { get; set; }

        public string MutabakatToken { get; set; } = string.Empty;

        public string? MutabakatReceiveStoragePath { get; set; }

        public Firma? Firma { get; set; }
        public Cari? Cari { get; set; }
        public DovizKodu? DovizKodu { get; set; }

        public enum MutabakatStatus
        {
            Kaydedildi = 1,
            Gonderildi = 2,
            Hatirlatma = 3,
            Mutabik = 4,
            MutabikDegil = 5
        }
    }
}