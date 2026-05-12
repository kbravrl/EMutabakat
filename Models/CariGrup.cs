using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMutabakat.Models
{
    public class CariGrup
    {
        [Key]
        [Required(ErrorMessage = "Cari grup ID zorunludur.")]
        public string CariGrupId { get; set; } = string.Empty;

        [NotMapped]
        public string OriginalCariGrupId { get; set; } = string.Empty;

        [NotMapped]
        public int OriginalFirmaId { get; set; }

        [ForeignKey("Firma")]
        [Range(1, int.MaxValue, ErrorMessage = "Firma seçimi zorunludur.")]
        public int FirmaId { get; set; }

        [Required(ErrorMessage = "Cari grup adı zorunludur.")]
        public string CariGrupAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Aktif/Pasif bilgisi zorunludur.")]
        public int CariGrupAktifPasif { get; set; } = 1;

        public Firma? Firma { get; set; }
    }
}