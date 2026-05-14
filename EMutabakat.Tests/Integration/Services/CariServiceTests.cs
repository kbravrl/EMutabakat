using EMutabakat.Models;
using EMutabakat.Services;
using EMutabakat.Services.Interfaces;
using EMutabakat.Tests.Testing;
using FluentAssertions;
using Moq;
using Xunit;

namespace EMutabakat.Tests.Integration.Services
{
    public class CariServiceTests
    {
        private readonly Mock<ILogService> _mockLog;

        public CariServiceTests()
        {
            _mockLog = new Mock<ILogService>();
            _mockLog.Setup(x => x.AddAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
                .Returns(Task.CompletedTask);
            _mockLog.Setup(x => x.AddChangeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(),
                It.IsAny<object>(), It.IsAny<string?>()))
                .Returns(Task.CompletedTask);
            _mockLog.Setup(x => x.AddImportResultAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(),
                It.IsAny<string?>()))
                .Returns(Task.CompletedTask);
        }

        private CariService CreateService(string dbName, string? userEmail = null)
        {
            var factory = TestDbContextFactory.CreateMockFactory(dbName);
            var httpAccessor = userEmail != null
                ? FakeHttpContextAccessor.CreateAuthenticated(userEmail)
                : FakeHttpContextAccessor.CreateAnonymous();
            return new CariService(factory.Object, _mockLog.Object, httpAccessor.Object);
        }

        // ── Seed yardımcıları ────────────────────────────────────────────────────

        private static async Task SeedBaseAsync(string dbName, int firmaId = 1)
        {
            await using var ctx = TestDbContextFactory.Create(dbName);

            if (!ctx.Firmalar.Any(f => f.FirmaId == firmaId))
            {
                ctx.Firmalar.Add(new Firma
                {
                    FirmaId = firmaId,
                    FirmaAdi = $"Test Firma {firmaId}",
                    FirmaVergiDairesi = "Test VD",
                    FirmaVergiNumarasi = "1234567890",
                    FirmaYetkiliAdiSoyadi = "Test Yetkili",
                    FirmaMail = $"firma{firmaId}@test.com",
                    FirmaTelefon = "02121234567",
                    FirmaSmtpHost = "smtp.test.com",
                    FirmaSmtpPort = 587,
                    FirmaSmtpUser = $"smtp{firmaId}@test.com",
                    FirmaSmtpPassword = "pass",
                    FirmaSmtpSecure = "true",
                    FirmaAktifPasif = 1
                });
            }

            if (!ctx.DovizKodlari.Any(d => d.TCMB == "TL"))
            {
                ctx.DovizKodlari.Add(new DovizKodu
                {
                    TCMB = "TL",
                    Name = "Türk Lirası",
                    DovizKoduAktifPasif = 1
                });
            }

            if (!ctx.CariGruplar.Any(g => g.CariGrupId == "P1" && g.FirmaId == firmaId))
            {
                ctx.CariGruplar.Add(new CariGrup
                {
                    CariGrupId = "P1",
                    FirmaId = firmaId,
                    CariGrupAdi = "Test Grup",
                    CariGrupAktifPasif = 1
                });
            }

            await ctx.SaveChangesAsync();
        }

        private static async Task SeedUserAsync(string dbName, string email, bool isSeed = false, List<int>? firmaIds = null)
        {
            await using var ctx = TestDbContextFactory.Create(dbName);

            if (ctx.Kullanicilar.Any(k => k.KullaniciMail == email))
                return;

            var kullanici = new Kullanici
            {
                KullaniciId = $"P{ctx.Kullanicilar.Count() + 1}",
                KullaniciAdi = "Test",
                KullaniciSoyadi = "User",
                KullaniciMail = email,
                KullaniciAktifPasif = "1",
                IsSeedUser = isSeed,
                Yetkileri = new KullaniciYetki { KullaniciId = $"P{ctx.Kullanicilar.Count() + 1}" }
            };

            ctx.Kullanicilar.Add(kullanici);
            await ctx.SaveChangesAsync();

            if (firmaIds != null)
            {
                foreach (var fid in firmaIds)
                {
                    var firma = await ctx.Firmalar.FindAsync(fid);
                    if (firma != null)
                        kullanici.Firmalar.Add(firma);
                }
                await ctx.SaveChangesAsync();
            }
        }

        private static Cari CreateCari(
            string cariId = "P1",
            int firmaId = 1,
            string grupId = "P1",
            string doviz = "TL",
            string adi = "Test Cari")
        {
            return new Cari
            {
                CariId = cariId,
                FirmaId = firmaId,
                CariAdi = adi,
                CariUnvan = "Test Unvan",
                CariVergiDairesi = "Test VD",
                CariVergiNumarasi = "1234567890",
                CariYetkiliAdiSoyadi = "Test Yetkili",
                CariYetkiliMail = "yetkili@test.com",
                CariYetkiliGsm = "05551234567",
                CariGrupId = grupId,
                CariDovizKodu = doviz,
                CariAktifPasif = 1
            };
        }

        // ─── GenerateNextCariIdAsync ─────────────────────────────────────────────

        [Fact]
        public async Task GenerateNextCariIdAsync_BosDatabasede_P1Doner()
        {
            var service = CreateService(nameof(GenerateNextCariIdAsync_BosDatabasede_P1Doner));

            var result = await service.GenerateNextCariIdAsync();

            result.Should().Be("P1");
        }

        [Fact]
        public async Task GenerateNextCariIdAsync_MevcutKayitlarla_SonrakiIdDoner()
        {
            var dbName = nameof(GenerateNextCariIdAsync_MevcutKayitlarla_SonrakiIdDoner);
            await SeedBaseAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Cariler.AddRange(
                    CreateCari("P1", adi: "Cari Bir"),
                    CreateCari("P5", adi: "Cari Bes")
                );
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.GenerateNextCariIdAsync();

            result.Should().Be("P6");
        }

        // ─── AddAsync ────────────────────────────────────────────────────────────

        [Fact]
        public async Task AddAsync_GecerliCari_EklenirveDoner()
        {
            var dbName = nameof(AddAsync_GecerliCari_EklenirveDoner);
            await SeedBaseAsync(dbName);
            var service = CreateService(dbName);

            var result = await service.AddAsync(CreateCari("P1"));

            result.Should().NotBeNull();
            result.CariId.Should().Be("P1");
            result.CariAdi.Should().Be("Test Cari");
        }

        [Fact]
        public async Task AddAsync_MailBuyukHarfle_KucukHarfeDonusur()
        {
            var dbName = nameof(AddAsync_MailBuyukHarfle_KucukHarfeDonusur);
            await SeedBaseAsync(dbName);
            var service = CreateService(dbName);

            var cari = CreateCari("P1", adi: "Mail Test Cari");
            cari.CariYetkiliMail = "TEST@EXAMPLE.COM";

            var result = await service.AddAsync(cari);

            result.CariYetkiliMail.Should().Be("test@example.com");
        }

        [Fact]
        public async Task AddAsync_DovizKoduBuyukHarfeDonusur()
        {
            var dbName = nameof(AddAsync_DovizKoduBuyukHarfeDonusur);
            await SeedBaseAsync(dbName);
            var service = CreateService(dbName);

            var cari = CreateCari("P1", adi: "Doviz Test Cari");
            cari.CariDovizKodu = "tl";

            var result = await service.AddAsync(cari);

            result.CariDovizKodu.Should().Be("TL");
        }

        [Fact]
        public async Task AddAsync_BosCariId_ExceptionFirlatir()
        {
            var dbName = nameof(AddAsync_BosCariId_ExceptionFirlatir);
            await SeedBaseAsync(dbName);
            var service = CreateService(dbName);

            var cari = CreateCari("  ");

            await FluentActions.Invoking(() => service.AddAsync(cari)).Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task AddAsync_FirmaIdSifir_ExceptionFirlatir()
        {
            var service = CreateService(nameof(AddAsync_FirmaIdSifir_ExceptionFirlatir));

            var cari = CreateCari(firmaId: 0);

            await FluentActions.Invoking(() => service.AddAsync(cari)).Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task AddAsync_YokFirma_ExceptionFirlatir()
        {
            var service = CreateService(nameof(AddAsync_YokFirma_ExceptionFirlatir));

            var cari = CreateCari(firmaId: 999);

            await FluentActions.Invoking(() => service.AddAsync(cari)).Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task AddAsync_YokCariGrup_ExceptionFirlatir()
        {
            var dbName = nameof(AddAsync_YokCariGrup_ExceptionFirlatir);
            await SeedBaseAsync(dbName);
            var service = CreateService(dbName);

            var cari = CreateCari(grupId: "P999");

            await FluentActions.Invoking(() => service.AddAsync(cari)).Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task AddAsync_GecersizDovizKodu_ExceptionFirlatir()
        {
            var dbName = nameof(AddAsync_GecersizDovizKodu_ExceptionFirlatir);
            await SeedBaseAsync(dbName);
            var service = CreateService(dbName);

            var cari = CreateCari(doviz: "XXX");

            await FluentActions.Invoking(() => service.AddAsync(cari)).Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task AddAsync_MukerrerCariId_ExceptionFirlatir()
        {
            var dbName = nameof(AddAsync_MukerrerCariId_ExceptionFirlatir);
            await SeedBaseAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Cariler.Add(CreateCari("P1"));
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);

            await FluentActions.Invoking(() => service.AddAsync(CreateCari("P1"))).Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task AddAsync_AyniAdFarklıId_ExceptionFirlatir()
        {
            var dbName = nameof(AddAsync_AyniAdFarklıId_ExceptionFirlatir);
            await SeedBaseAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Cariler.Add(CreateCari("P1", adi: "Mevcut Cari"));
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);

            await FluentActions.Invoking(() => service.AddAsync(CreateCari("P2", adi: "Mevcut Cari")))
                .Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task AddAsync_GecersizAktifPasif_ExceptionFirlatir()
        {
            var dbName = nameof(AddAsync_GecersizAktifPasif_ExceptionFirlatir);
            await SeedBaseAsync(dbName);
            var service = CreateService(dbName);

            var cari = CreateCari("P1", adi: "AktifPasif Test Cari");
            cari.CariAktifPasif = 5;

            await FluentActions.Invoking(() => service.AddAsync(cari)).Should().ThrowAsync<Exception>();
        }

        // ─── GetByIdAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_MevcutCari_CariDoner()
        {
            var dbName = nameof(GetByIdAsync_MevcutCari_CariDoner);
            await SeedBaseAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Cariler.Add(CreateCari("P1"));
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.GetByIdAsync("P1", 1);

            result.Should().NotBeNull();
            result!.CariId.Should().Be("P1");
        }

        [Fact]
        public async Task GetByIdAsync_YokCari_NullDoner()
        {
            var service = CreateService(nameof(GetByIdAsync_YokCari_NullDoner));

            var result = await service.GetByIdAsync("P999", 1);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_YetkisizFirma_NullDoner()
        {
            var dbName = nameof(GetByIdAsync_YetkisizFirma_NullDoner);
            await SeedBaseAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Cariler.Add(CreateCari("P1", firmaId: 1));
                await ctx.SaveChangesAsync();
            }
            await SeedUserAsync(dbName, "user@test.com", isSeed: false, firmaIds: new List<int>());

            var service = CreateService(dbName, "user@test.com");
            var result = await service.GetByIdAsync("P1", 1);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_IdBoslukluGelirse_TrimEdilir()
        {
            var dbName = nameof(GetByIdAsync_IdBoslukluGelirse_TrimEdilir);
            await SeedBaseAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Cariler.Add(CreateCari("P1"));
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.GetByIdAsync("  P1  ", 1);

            result.Should().NotBeNull();
        }

        // ─── GetAllAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_AnonimKullanici_TumKayitlariDoner()
        {
            var dbName = nameof(GetAllAsync_AnonimKullanici_TumKayitlariDoner);
            await SeedBaseAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Cariler.AddRange(
                    CreateCari("P1", adi: "Cari A"),
                    CreateCari("P2", adi: "Cari B")
                );
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName, userEmail: null);
            var result = await service.GetAllAsync();

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllAsync_NormalKullanici_SadeceYetkiliOlduguFirmalariGorur()
        {
            var dbName = nameof(GetAllAsync_NormalKullanici_SadeceYetkiliOlduguFirmalariGorur);
            await SeedBaseAsync(dbName, firmaId: 1);
            await SeedBaseAsync(dbName, firmaId: 2);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Cariler.AddRange(
                    CreateCari("P1", firmaId: 1, adi: "Firma1 Cari"),
                    CreateCari("P2", firmaId: 2, grupId: "P1", adi: "Firma2 Cari")
                );
                await ctx.SaveChangesAsync();
            }
            await SeedUserAsync(dbName, "user@test.com", isSeed: false, firmaIds: new List<int> { 1 });

            var service = CreateService(dbName, "user@test.com");
            var result = await service.GetAllAsync();

            result.Should().HaveCount(1);
            result[0].FirmaId.Should().Be(1);
        }

        [Fact]
        public async Task GetAllAsync_SeedKullanici_TumFirmalariGorur()
        {
            var dbName = nameof(GetAllAsync_SeedKullanici_TumFirmalariGorur);
            await SeedBaseAsync(dbName, firmaId: 1);
            await SeedBaseAsync(dbName, firmaId: 2);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Cariler.AddRange(
                    CreateCari("P1", firmaId: 1, adi: "Firma1 Cari"),
                    CreateCari("P2", firmaId: 2, grupId: "P1", adi: "Firma2 Cari")
                );
                await ctx.SaveChangesAsync();
            }
            await SeedUserAsync(dbName, "seed@test.com", isSeed: true, firmaIds: new List<int> { 1, 2 });

            var service = CreateService(dbName, "seed@test.com");
            var result = await service.GetAllAsync();

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllAsync_KullaniciFirmasiYok_BosListeDoner()
        {
            var dbName = nameof(GetAllAsync_KullaniciFirmasiYok_BosListeDoner);
            await SeedBaseAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Cariler.Add(CreateCari("P1"));
                await ctx.SaveChangesAsync();
            }
            await SeedUserAsync(dbName, "user@test.com", isSeed: false, firmaIds: new List<int>());

            var service = CreateService(dbName, "user@test.com");
            var result = await service.GetAllAsync();

            result.Should().BeEmpty();
        }

        // ─── UpdateAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_MevcutCari_GuncellenirVeDoner()
        {
            var dbName = nameof(UpdateAsync_MevcutCari_GuncellenirVeDoner);
            await SeedBaseAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Cariler.Add(CreateCari("P1", adi: "Eski Ad"));
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var updated = CreateCari("P1", adi: "Yeni Ad");
            updated.OriginalCariId = "P1";
            updated.OriginalFirmaId = 1;

            var result = await service.UpdateAsync(updated);

            result.Should().NotBeNull();
            result!.CariAdi.Should().Be("Yeni Ad");
        }

        [Fact]
        public async Task UpdateAsync_YokCari_NullDoner()
        {
            var dbName = nameof(UpdateAsync_YokCari_NullDoner);
            await SeedBaseAsync(dbName);
            var service = CreateService(dbName);

            var updated = CreateCari("P999");
            updated.OriginalCariId = "P999";
            updated.OriginalFirmaId = 1;

            var result = await service.UpdateAsync(updated);

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_AnahtarDegisirse_EskiSilinirYeniEklenir()
        {
            var dbName = nameof(UpdateAsync_AnahtarDegisirse_EskiSilinirYeniEklenir);
            await SeedBaseAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Cariler.Add(CreateCari("P1", adi: "Eski Cari Adi"));
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            // CariId değişiyor, CariAdi de farklı olmalı (unique constraint: FirmaId+CariAdi)
            var updated = CreateCari("P2", adi: "Yeni Cari Adi");
            updated.OriginalCariId = "P1";
            updated.OriginalFirmaId = 1;

            var result = await service.UpdateAsync(updated);

            result.Should().NotBeNull();
            result!.CariId.Should().Be("P2");

            await using var assertCtx = TestDbContextFactory.Create(dbName);
            assertCtx.Cariler.Any(c => c.CariId == "P1" && c.FirmaId == 1).Should().BeFalse();
            assertCtx.Cariler.Any(c => c.CariId == "P2" && c.FirmaId == 1).Should().BeTrue();
        }

        [Fact]
        public async Task UpdateAsync_MailNormalize_KucukHarfeDonusur()
        {
            var dbName = nameof(UpdateAsync_MailNormalize_KucukHarfeDonusur);
            await SeedBaseAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Cariler.Add(CreateCari("P1", adi: "Mail Normalize Cari"));
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var updated = CreateCari("P1", adi: "Mail Normalize Cari");
            updated.OriginalCariId = "P1";
            updated.OriginalFirmaId = 1;
            updated.CariYetkiliMail = "TEST@EXAMPLE.COM";

            var result = await service.UpdateAsync(updated);

            result!.CariYetkiliMail.Should().Be("test@example.com");
        }

        // ─── DeleteAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_MevcutCari_TrueDoner()
        {
            var dbName = nameof(DeleteAsync_MevcutCari_TrueDoner);
            await SeedBaseAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Cariler.Add(CreateCari("P1"));
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.DeleteAsync("P1", 1);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_YokCari_FalseDoner()
        {
            var service = CreateService(nameof(DeleteAsync_YokCari_FalseDoner));

            var result = await service.DeleteAsync("P999", 1);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_SilindiktenSonra_GetByIdNullDoner()
        {
            var dbName = nameof(DeleteAsync_SilindiktenSonra_GetByIdNullDoner);
            await SeedBaseAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Cariler.Add(CreateCari("P1"));
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            await service.DeleteAsync("P1", 1);

            var result = await service.GetByIdAsync("P1", 1);
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_IdBoslukluGelirse_TrimEdilir()
        {
            var dbName = nameof(DeleteAsync_IdBoslukluGelirse_TrimEdilir);
            await SeedBaseAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Cariler.Add(CreateCari("P1"));
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.DeleteAsync("  P1  ", 1);

            result.Should().BeTrue();
        }
    }
}
