using EMutabakat.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EMutabakat.Data
{
    public static class AppDbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            const string adminMail = "admin@emutabakat.local";
            const string adminPassword = "rp23TE&?";

            var defaultFirma = await context.Firmalar.FirstOrDefaultAsync(f => f.FirmaVergiNumarasi == "1111111111");
            if (defaultFirma == null)
            {
                defaultFirma = new Firma
                {
                    FirmaAdi = "Sistem Firması",
                    FirmaUnvan = "Sistem Firması A.Ş.",
                    FirmaAdres = "Merkez",
                    FirmaIlce = "Çankaya",
                    FirmaIl = "Ankara",
                    FirmaVergiDairesi = "Çankaya",
                    FirmaVergiNumarasi = "1111111111",
                    FirmaMersisNumarasi = "0000000000000000",
                    FirmaWebAdresi = "https://example.com",
                    FirmaYetkiliAdiSoyadi = "Sistem Yöneticisi",
                    FirmaMail = "firma@emutabakat.local",
                    FirmaTelefon = "03120000000",
                    FirmaGsm = "05000000000",
                    FirmaSmtpHost = "smtp.example.com",
                    FirmaSmtpPort = 587,
                    FirmaSmtpUser = "smtp@example.com",
                    FirmaSmtpPassword = "ChangeMe123!",
                    FirmaSmtpSecure = "true",
                    FirmaAktifPasif = 1
                };

                context.Firmalar.Add(defaultFirma);
                await context.SaveChangesAsync();
            }

            var hasher = new PasswordHasher<Kullanici>();
            var admin = await context.Kullanicilar.FirstOrDefaultAsync(k => k.KullaniciMail == adminMail);
            if (admin == null)
            {
                admin = new Kullanici
                {
                    FirmaId = defaultFirma.FirmaId,
                    KullaniciAdi = "Sistem",
                    KullaniciSoyadi = "Yöneticisi",
                    KullaniciMail = adminMail,
                    KullaniciGsm = "05000000001",
                    Sifre = string.Empty,
                    Rol = KullaniciRolleri.Admin,
                    KullaniciAktifPasif = "1"
                };

                admin.Sifre = hasher.HashPassword(admin, adminPassword);

                context.Kullanicilar.Add(admin);
                await context.SaveChangesAsync();
            }
            else
            {
                admin.Rol = KullaniciRolleri.Admin;
                admin.KullaniciAktifPasif = "1";

                var verify = hasher.VerifyHashedPassword(admin, admin.Sifre, adminPassword);
                if (verify == PasswordVerificationResult.Failed)
                {
                    admin.Sifre = hasher.HashPassword(admin, adminPassword);
                }

                await context.SaveChangesAsync();
            }

            if (!await context.KullaniciFirmalari.AnyAsync(kf => kf.KullaniciId == admin.KullaniciId && kf.FirmaId == defaultFirma.FirmaId))
            {
                context.KullaniciFirmalari.Add(new KullaniciFirma
                {
                    KullaniciId = admin.KullaniciId,
                    FirmaId = defaultFirma.FirmaId
                });

                await context.SaveChangesAsync();
            }

            var usersWithoutRole = await context.Kullanicilar
                .Where(k => string.IsNullOrWhiteSpace(k.Rol))
                .ToListAsync();

            foreach (var user in usersWithoutRole)
            {
                user.Rol = KullaniciRolleri.Standart;
            }

            var users = await context.Kullanicilar
                .AsNoTracking()
                .Select(k => new { k.KullaniciId, k.FirmaId })
                .ToListAsync();

            foreach (var user in users)
            {
                if (!await context.KullaniciFirmalari.AnyAsync(kf => kf.KullaniciId == user.KullaniciId && kf.FirmaId == user.FirmaId))
                {
                    context.KullaniciFirmalari.Add(new KullaniciFirma
                    {
                        KullaniciId = user.KullaniciId,
                        FirmaId = user.FirmaId
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
