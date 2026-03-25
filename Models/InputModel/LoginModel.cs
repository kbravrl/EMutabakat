using System.ComponentModel.DataAnnotations;

namespace EMutabakat.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Mail zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir mail giriniz.")]
        public string Mail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        public string Sifre { get; set; } = string.Empty;
    }
}