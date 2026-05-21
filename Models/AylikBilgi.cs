using System.ComponentModel.DataAnnotations;

namespace EMutabakat.Models
{
    public class AylikBilgi
    {
        [Key]
        public int Id { get; set; }

        [Range(1, 9999, ErrorMessage = "Yıl zorunludur.")]
        public int Yil { get; set; }

        [Range(1, 12, ErrorMessage = "Ay 1 ile 12 arasında olmalıdır.")]
        public int Ay { get; set; }

        public bool AcikMi { get; set; } = true;
    }
}
