using System.ComponentModel.DataAnnotations;

namespace EMutabakat.Models
{
    public class DovizKodu
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string TCMB { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
