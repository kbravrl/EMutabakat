using System.ComponentModel.DataAnnotations;

namespace EMutabakat.Models
{
    public class AppLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string Level { get; set; } = "Info"; // Info, Warning, Error

        [Required]
        public string Source { get; set; } = string.Empty;

        public string? UserEmail { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public string? Details { get; set; }
    }
}