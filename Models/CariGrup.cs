using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMutabakat.Models
{
    public class CariGrup
    {
        [Key]
        public int CariGrupId { get; set; }

        [Required(ErrorMessage = "Firma seçimi zorunludur.")]
        [ForeignKey("Firma")]
        public int FirmaId { get; set; }

        [Required(ErrorMessage = "Cari grup adı zorunludur.")]
        public string CariGrupAdi { get; set; } = string.Empty;

        public Firma? Firma { get; set; }
    }
}