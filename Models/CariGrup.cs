using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMutabakat.Models
{
    public class CariGrup
    {
        [Key]
        public int CariGrupId { get; set; }

        [ForeignKey("Firma")]
        public int FirmaId { get; set; }

        public string CariGrupAdi { get; set; }

        public Firma Firma { get; set; }
    }
}