namespace EMutabakat.Models
{
    public static class KullaniciRolleri
    {
        public const string Standart = "Standart";
        public const string Admin = "Admin";

        public static bool IsValid(string? rol)
        {
            return rol == Standart || rol == Admin;
        }
    }
}
