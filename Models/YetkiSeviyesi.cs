using System.ComponentModel;

namespace EMutabakat.Models
{
    public enum YetkiSeviyesi
    {
        [Description("Giriş")]
        Giris = 1,

        [Description("Giriş + Düzeltme")]
        GirisDuzeltme = 2,

        [Description("Tam Yetki")]
        TamYetki = 3
    }
}
