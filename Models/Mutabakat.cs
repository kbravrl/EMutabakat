using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMutabakat.Models
{
    public class Mutabakat
    {
        [Key]
        public int MutabakatId { get; set; }

        public DateTime MutabakatDonemi { get; set; }
        public int MutabakatTipi { get; set; }

        [ForeignKey("Firma")]
        public int FirmaId { get; set; }

        [ForeignKey("Cari")]
        public int CariId { get; set; }

        public int MutabakatDovizKodu { get; set; }
        public decimal MutabakatBakiye { get; set; }
        public string MutabakatBakiyeTipi { get; set; }
        public string MutabakatAciklama { get; set; }

        public DateTime? MutabakatGonderimTarihSaat { get; set; }
        public int MutabakatGonderimDurumu { get; set; }

        public DateTime? MutabakatCevapTarihSaat { get; set; }
        public string? MutabakatCevapMail { get; set; }
        public string? MutabakatCevapAdSoyad { get; set; }
        public string? MutabakatCevapGsm { get; set; }

        public int MutabakatDurum { get; set; }
        public string MutabakatToken { get; set; }
        public string? MutabakatReceiveStoragePath { get; set; }
        public Firma Firma { get; set; }
        public Cari Cari { get; set; }
    }
}