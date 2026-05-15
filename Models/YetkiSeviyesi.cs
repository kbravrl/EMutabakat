using System.ComponentModel;

namespace EMutabakat.Models
{
    public enum YetkiSeviyesi
    {
        [Description("Yetkisiz")]
        Yetkisiz = 0,

        [Description("Giriş")]
        Giris = 1,

        [Description("Giriş + Düzeltme")]
        GirisDuzeltme = 2,

        [Description("Tam Yetki")]
        TamYetki = 3
    }
}
