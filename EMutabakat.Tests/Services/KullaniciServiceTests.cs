using EMutabakat.Models;
using EMutabakat.Services;
using EMutabakat.Services.Interfaces;
using EMutabakat.Tests.Testing;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace EMutabakat.Tests.Services
{
    public class KullaniciServiceTests
    {
        private readonly Mock<ILogService> _mockLog;

        public KullaniciServiceTests()
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
        }

        private KullaniciService CreateService(string dbName, string? userEmail = "admin@test.com")
        {
            var factory = TestDbContextFactory.CreateMockFactory(dbName);
            var httpAccessor = userEmail != null
                ? FakeHttpContextAccessor.CreateAuthenticated(userEmail)
                : FakeHttpContextAccessor.CreateAnonymous();
            return new KullaniciService(factory.Object, _mockLog.Object, httpAccessor.Object);
        }

        /// <summary>
        /// Test için şifrelenmiş kullanıcı oluşturur.
        /// </summary>
        private static Kullanici CreateHashedKullanici(string mail, string plainPassword, bool aktif = true)
        {
            var kullanici = new Kullanici
            {
                KullaniciId = "P1",
                KullaniciAdi = "Test",
                KullaniciSoyadi = "Kullanıcı",
                KullaniciMail = mail.Trim().ToLower(),
                KullaniciAktifPasif = aktif ? "1" : "0",
                Yetkileri = new KullaniciYetki()
            };
            var hasher = new PasswordHasher<Kullanici>();
            kullanici.Sifre = hasher.HashPassword(kullanici, plainPassword);
            return kullanici;
        }

        // ─── LoginAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task LoginAsync_DogruMailVeSifre_KullaniciDoner()
        {
            var dbName = nameof(LoginAsync_DogruMailVeSifre_KullaniciDoner);
            var kullanici = CreateHashedKullanici("user@test.com", "sifre123");

            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Kullanicilar.Add(kullanici);
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.LoginAsync("user@test.com", "sifre123");

            Assert.NotNull(result);
            Assert.Equal("user@test.com", result!.KullaniciMail);
        }

        [Fact]
        public async Task LoginAsync_YanlisMailVeSifre_NullDoner()
        {
            var dbName = nameof(LoginAsync_YanlisMailVeSifre_NullDoner);
            var kullanici = CreateHashedKullanici("user@test.com", "sifre123");

            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Kullanicilar.Add(kullanici);
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.LoginAsync("user@test.com", "yanlisSifre");

            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_YokMail_NullDoner()
        {
            var service = CreateService(nameof(LoginAsync_YokMail_NullDoner));

            var result = await service.LoginAsync("yok@test.com", "herhangi");

            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_PasifKullanici_NullDoner()
        {
            var dbName = nameof(LoginAsync_PasifKullanici_NullDoner);
            var kullanici = CreateHashedKullanici("pasif@test.com", "sifre123", aktif: false);

            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Kullanicilar.Add(kullanici);
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.LoginAsync("pasif@test.com", "sifre123");

            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_MailBuyukHarfle_NormalizeEdilir()
        {
            var dbName = nameof(LoginAsync_MailBuyukHarfle_NormalizeEdilir);
            var kullanici = CreateHashedKullanici("user@test.com", "sifre123");

            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Kullanicilar.Add(kullanici);
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.LoginAsync("USER@TEST.COM", "sifre123");

            Assert.NotNull(result);
        }

        // ─── RegisterAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task RegisterAsync_YeniKullanici_KaydedilirVeDoner()
        {
            var service = CreateService(nameof(RegisterAsync_YeniKullanici_KaydedilirVeDoner));

            var kullanici = new Kullanici
            {
                KullaniciId = "P1",
                KullaniciAdi = "Ali",
                KullaniciSoyadi = "Veli",
                KullaniciMail = "ali@test.com",
                Sifre = "sifre123",
                KullaniciAktifPasif = "1",
                Yetkileri = new KullaniciYetki()
            };

            var result = await service.RegisterAsync(kullanici);

            Assert.NotNull(result);
            Assert.Equal("ali@test.com", result!.KullaniciMail);
        }

        [Fact]
        public async Task RegisterAsync_MevcutMail_NullDoner()
        {
            var dbName = nameof(RegisterAsync_MevcutMail_NullDoner);
            var kullanici = CreateHashedKullanici("mevcut@test.com", "sifre123");

            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Kullanicilar.Add(kullanici);
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var yeni = new Kullanici
            {
                KullaniciId = "P2",
                KullaniciAdi = "Yeni",
                KullaniciSoyadi = "Kullanıcı",
                KullaniciMail = "mevcut@test.com",
                Sifre = "baskaSifre",
                KullaniciAktifPasif = "1",
                Yetkileri = new KullaniciYetki()
            };

            var result = await service.RegisterAsync(yeni);

            Assert.Null(result);
        }

        // ─── GetByMailAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetByMailAsync_MevcutMail_KullaniciDoner()
        {
            var dbName = nameof(GetByMailAsync_MevcutMail_KullaniciDoner);
            var kullanici = CreateHashedKullanici("bul@test.com", "sifre");

            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Kullanicilar.Add(kullanici);
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.GetByMailAsync("bul@test.com");

            Assert.NotNull(result);
            Assert.Equal("bul@test.com", result!.KullaniciMail);
        }

        [Fact]
        public async Task GetByMailAsync_YokMail_NullDoner()
        {
            var service = CreateService(nameof(GetByMailAsync_YokMail_NullDoner));

            var result = await service.GetByMailAsync("yok@test.com");

            Assert.Null(result);
        }

        // ─── GenerateNextKullaniciIdAsync ────────────────────────────────────────

        [Fact]
        public async Task GenerateNextKullaniciIdAsync_BosDatabasede_P1Doner()
        {
            var service = CreateService(nameof(GenerateNextKullaniciIdAsync_BosDatabasede_P1Doner));

            var result = await service.GenerateNextKullaniciIdAsync();

            Assert.Equal("P1", result);
        }

        [Fact]
        public async Task GenerateNextKullaniciIdAsync_MevcutKayitlarla_SonrakiIdDoner()
        {
            var dbName = nameof(GenerateNextKullaniciIdAsync_MevcutKayitlarla_SonrakiIdDoner);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Kullanicilar.AddRange(
                    new Kullanici { KullaniciId = "P1", KullaniciAdi = "A", KullaniciSoyadi = "B", KullaniciMail = "a@t.com", KullaniciAktifPasif = "1", Yetkileri = new KullaniciYetki() },
                    new Kullanici { KullaniciId = "P3", KullaniciAdi = "C", KullaniciSoyadi = "D", KullaniciMail = "c@t.com", KullaniciAktifPasif = "1", Yetkileri = new KullaniciYetki() }
                );
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.GenerateNextKullaniciIdAsync();

            Assert.Equal("P4", result);
        }

        // ─── IsCurrentUserSeedAsync ──────────────────────────────────────────────

        [Fact]
        public async Task IsCurrentUserSeedAsync_SeedKullanici_TrueDoner()
        {
            var dbName = nameof(IsCurrentUserSeedAsync_SeedKullanici_TrueDoner);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Kullanicilar.Add(new Kullanici
                {
                    KullaniciId = "P1",
                    KullaniciAdi = "Seed",
                    KullaniciSoyadi = "User",
                    KullaniciMail = "seed@test.com",
                    KullaniciAktifPasif = "1",
                    IsSeedUser = true,
                    Yetkileri = new KullaniciYetki()
                });
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName, "seed@test.com");
            var result = await service.IsCurrentUserSeedAsync();

            Assert.True(result);
        }

        [Fact]
        public async Task IsCurrentUserSeedAsync_NormalKullanici_FalseDoner()
        {
            var dbName = nameof(IsCurrentUserSeedAsync_NormalKullanici_FalseDoner);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Kullanicilar.Add(new Kullanici
                {
                    KullaniciId = "P1",
                    KullaniciAdi = "Normal",
                    KullaniciSoyadi = "User",
                    KullaniciMail = "normal@test.com",
                    KullaniciAktifPasif = "1",
                    IsSeedUser = false,
                    Yetkileri = new KullaniciYetki()
                });
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName, "normal@test.com");
            var result = await service.IsCurrentUserSeedAsync();

            Assert.False(result);
        }

        // ─── GetCurrentUserEmail ─────────────────────────────────────────────────

        [Fact]
        public void GetCurrentUserEmail_KimlikDogrulanmis_MailDoner()
        {
            var service = CreateService(nameof(GetCurrentUserEmail_KimlikDogrulanmis_MailDoner), "test@test.com");

            var result = service.GetCurrentUserEmail();

            Assert.Equal("test@test.com", result);
        }

        [Fact]
        public void GetCurrentUserEmail_Anonim_NullDoner()
        {
            var factory = TestDbContextFactory.CreateMockFactory();
            var httpAccessor = FakeHttpContextAccessor.CreateAnonymous();
            var service = new KullaniciService(factory.Object, _mockLog.Object, httpAccessor.Object);

            var result = service.GetCurrentUserEmail();

            Assert.Null(result);
        }
    }
}
