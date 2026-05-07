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

            var dovizKodlari = new List<DovizKodu>
            {
                new() { TCMB = "TL", Name = "Türk Lirası", DovizKoduAktifPasif = 1 },
                new() { TCMB = "USD", Name = "Amerikan Doları", DovizKoduAktifPasif = 1 },
                new() { TCMB = "EUR", Name = "Euro", DovizKoduAktifPasif = 1 },
            };

            foreach (var doviz in dovizKodlari)
            {
                var existing = await context.DovizKodlari.FirstOrDefaultAsync(x => x.TCMB == doviz.TCMB);
                if (existing == null)
                {
                    context.DovizKodlari.Add(doviz);
                }
                else if (existing.DovizKoduAktifPasif != 1)
                {
                    existing.DovizKoduAktifPasif = 1;
                    existing.Name = doviz.Name;
                }
            }

            await context.SaveChangesAsync();

            var defaultFirma = await context.Firmalar
                .FirstOrDefaultAsync(f => f.FirmaVergiNumarasi == "1111111111");

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

            var admin = await context.Kullanicilar
                .Include(k => k.Firmalar)
                .FirstOrDefaultAsync(k => k.KullaniciMail == adminMail);

            if (admin == null)
            {
                admin = new Kullanici
                {
                    KullaniciId = "1",
                    KullaniciAdi = "Sistem",
                    KullaniciSoyadi = "Yöneticisi",
                    KullaniciMail = adminMail,
                    KullaniciGsm = "05000000001",
                    Sifre = string.Empty,
                    KullaniciAktifPasif = "1",
                    IsSeedUser = true,
                    Yetkileri = new KullaniciYetki
                    {
                        Cariler = YetkiSeviyesi.TamYetki,
                        CariGruplar = YetkiSeviyesi.TamYetki,
                        DovizKodlari = YetkiSeviyesi.TamYetki,
                        Mutabakatlar = YetkiSeviyesi.TamYetki,
                        Firmalar = YetkiSeviyesi.TamYetki,
                        Kullanicilar = YetkiSeviyesi.TamYetki,
                        LogYetki = true,
                        ImportYetki = true,
                        ExportYetki = true
                    }
                };

                admin.Sifre = hasher.HashPassword(admin, adminPassword);
                admin.Firmalar.Add(defaultFirma);

                context.Kullanicilar.Add(admin);
                await context.SaveChangesAsync();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(admin.KullaniciId))
                {
                    admin.KullaniciId = "1";
                }

                admin.KullaniciAktifPasif = "1";
                admin.IsSeedUser = true;

                if (admin.Yetkileri == null)
                {
                    admin.Yetkileri = new KullaniciYetki
                    {
                        Cariler = YetkiSeviyesi.TamYetki,
                        CariGruplar = YetkiSeviyesi.TamYetki,
                        DovizKodlari = YetkiSeviyesi.TamYetki,
                        Mutabakatlar = YetkiSeviyesi.TamYetki,
                        Firmalar = YetkiSeviyesi.TamYetki,
                        Kullanicilar = YetkiSeviyesi.TamYetki,
                        LogYetki = true,
                        ImportYetki = true,
                        ExportYetki = true
                    };
                }

                var verify = hasher.VerifyHashedPassword(admin, admin.Sifre, adminPassword);
                if (verify == PasswordVerificationResult.Failed)
                {
                    admin.Sifre = hasher.HashPassword(admin, adminPassword);
                }

                if (!admin.Firmalar.Any(f => f.FirmaId == defaultFirma.FirmaId))
                {
                    admin.Firmalar.Add(defaultFirma);
                }

                await context.SaveChangesAsync();
            }

            var usersWithoutPermissions = await context.Kullanicilar
                .Include(k => k.Yetkileri)
                .Where(k => k.Yetkileri == null)
                .ToListAsync();

            foreach (var user in usersWithoutPermissions)
            {
                user.Yetkileri = new KullaniciYetki();
            }

            await context.SaveChangesAsync();
        }
    }
}