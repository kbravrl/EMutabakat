using System.ComponentModel.DataAnnotations;

namespace EMutabakat.Models
{
    public class DovizKodu
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "TCMB alanı zorunludur.")]
        [StringLength(10, ErrorMessage = "TCMB en fazla 10 karakter olabilir.")]
        public string TCMB { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;
    }
}