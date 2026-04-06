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

        [Required(ErrorMessage = "Firma seçimi zorunludur.")]
        [ForeignKey("Firma")]
        public int FirmaId { get; set; }

        [Required(ErrorMessage = "Cari grup adı zorunludur.")]
        public string CariGrupAdi { get; set; } = string.Empty;

        public Firma? Firma { get; set; }
    }
}