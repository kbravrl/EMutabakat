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
                new() { TCMB = "GBP", Name = "İngiliz Sterlini", DovizKoduAktifPasif = 1 },
                new() { TCMB = "CHF", Name = "İsviçre Frangı", DovizKoduAktifPasif = 1 },
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
                        MutabakatSilYetki = true,
                        AylikBilgilerYetki = true,
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
                        MutabakatSilYetki = true,
                        AylikBilgilerYetki = true,
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

            // Seed 20 cevaplanmış mutabakat (dönem: Nisan ve Mayıs karışık)
            try
            {
                var year = DateTime.Now.Year;

                var firmaId = defaultFirma.FirmaId;

                // Ensure a cari grup exists
                var cariGrupId = "GRP1";
                var cariGrup = await context.CariGruplar.FirstOrDefaultAsync(cg => cg.CariGrupId == cariGrupId && cg.FirmaId == firmaId);
                if (cariGrup == null)
                {
                    cariGrup = new CariGrup
                    {
                        CariGrupId = cariGrupId,
                        FirmaId = firmaId,
                        CariGrupAdi = "Örnek Cari Grubu",
                        CariGrupAktifPasif = 1
                    };
                    context.CariGruplar.Add(cariGrup);
                    await context.SaveChangesAsync();
                }

                // Create 5 cariler to attach mutabakatlara
                var cariler = new List<Cari>();
                for (int i = 1; i <= 5; i++)
                {
                    var cariId = $"C{i:000}";
                    var existingCari = await context.Cariler.FirstOrDefaultAsync(c => c.CariId == cariId && c.FirmaId == firmaId);
                    if (existingCari == null)
                    {
                        existingCari = new Cari
                        {
                            CariId = cariId,
                            FirmaId = firmaId,
                            CariAdi = $"Cari {i}",
                            CariVergiDairesi = "Vergi",
                            CariVergiNumarasi = (1111111110 + i).ToString(),
                            CariYetkiliMail = $"cari{i}@example.com",
                            CariGrupId = cariGrupId,
                            CariDovizKodu = "TL",
                            CariAktifPasif = 1
                        };

                        context.Cariler.Add(existingCari);
                        await context.SaveChangesAsync();
                    }

                    cariler.Add(existingCari);
                }

                // Prepare 20 distinct mutabakat records
                var mutabakatList = new List<Mutabakat>();
                for (int i = 1; i <= 20; i++)
                {
                    var month = (i % 2 == 0) ? 4 : 5; // even -> Nisan(4), odd -> Mayıs(5)
                    var day = (i % 28) + 1;
                    var date = new DateTime(year, month, day);
                    var cari = cariler[(i - 1) % cariler.Count];

                    var mutabakatId = $"M{year}{i:000}";

                    var status = (i % 2 == 0) ? Mutabakat.MutabakatStatus.Mutabik : Mutabakat.MutabakatStatus.MutabikDegil;

                    var m = new Mutabakat
                    {
                        MutabakatId = mutabakatId,
                        FirmaId = firmaId,
                        CariId = cari.CariId,
                        MutabakatTarihi = date,
                        MutabakatDovizKodu = "TL",
                        MutabakatBakiye = 100m * i,
                        MutabakatBakiyeTipi = (i % 2 == 0) ? "B" : "A",
                        MutabakatAciklama = $"Otomatik seed mutabakat #{i}",
                        MutabakatGonderimTarihSaat = date.AddDays(-1),
                        MutabakatCevapTarihSaat = date.AddDays(2),
                        MutabakatCevapMail = $"cevap{i}@example.com",
                        MutabakatCevapAdSoyad = $"Cevap Sahibi {i}",
                        MutabakatCevapGsm = $"05{(700000000 + i).ToString()[1..]}",
                        MutabakatCevapAciklama = (status == Mutabakat.MutabakatStatus.Mutabik) ? "Mutabık olarak cevaplandı." : "Mutabık değil olarak cevaplandı.",
                        MutabakatToken = Guid.NewGuid().ToString("N"),
                        Status = status
                    };

                    // only add if not exists (by unique index MutabakatId+FirmaId)
                    var exists = await context.Mutabakatlar.AnyAsync(x => x.MutabakatId == m.MutabakatId && x.FirmaId == m.FirmaId);
                    if (!exists)
                    {
                        mutabakatList.Add(m);
                    }
                }

                if (mutabakatList.Any())
                {
                    context.Mutabakatlar.AddRange(mutabakatList);
                    await context.SaveChangesAsync();
                }
            }
            catch
            {
                // Ignore seeding errors to avoid blocking startup
            }
        }
    }
}