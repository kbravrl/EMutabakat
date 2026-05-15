using EMutabakat.Models;
using EMutabakat.Services;
using EMutabakat.Services.Interfaces;
using EMutabakat.Tests.Testing;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace EMutabakat.Tests.Integration.Services
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

        private static Kullanici CreateHashedKullanici(string mail, string plainPassword, bool aktif = true)
        {
            var kullanici = new Kullanici
            {
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

            result.Should().NotBeNull();
            result!.KullaniciMail.Should().Be("user@test.com");
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

            result.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_YokMail_NullDoner()
        {
            var service = CreateService(nameof(LoginAsync_YokMail_NullDoner));

            var result = await service.LoginAsync("yok@test.com", "herhangi");

            result.Should().BeNull();
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

            result.Should().BeNull();
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

            result.Should().NotBeNull();
        }

        // ─── RegisterAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task RegisterAsync_YeniKullanici_KaydedilirVeDoner()
        {
            var service = CreateService(nameof(RegisterAsync_YeniKullanici_KaydedilirVeDoner));

            var kullanici = new Kullanici
            {
                KullaniciAdi = "Ali",
                KullaniciSoyadi = "Veli",
                KullaniciMail = "ali@test.com",
                Sifre = "sifre123",
                KullaniciAktifPasif = "1",
                Yetkileri = new KullaniciYetki()
            };

            var result = await service.RegisterAsync(kullanici);

            result.Should().NotBeNull();
            result!.KullaniciMail.Should().Be("ali@test.com");
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
                KullaniciAdi = "Yeni",
                KullaniciSoyadi = "Kullanıcı",
                KullaniciMail = "mevcut@test.com",
                Sifre = "baskaSifre",
                KullaniciAktifPasif = "1",
                Yetkileri = new KullaniciYetki()
            };

            var result = await service.RegisterAsync(yeni);

            result.Should().BeNull();
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

            result.Should().NotBeNull();
            result!.KullaniciMail.Should().Be("bul@test.com");
        }

        [Fact]
        public async Task GetByMailAsync_YokMail_NullDoner()
        {
            var service = CreateService(nameof(GetByMailAsync_YokMail_NullDoner));

            var result = await service.GetByMailAsync("yok@test.com");

            result.Should().BeNull();
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

            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsCurrentUserSeedAsync_NormalKullanici_FalseDoner()
        {
            var dbName = nameof(IsCurrentUserSeedAsync_NormalKullanici_FalseDoner);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Kullanicilar.Add(new Kullanici
                {
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

            result.Should().BeFalse();
        }

        // ─── GetCurrentUserEmail ─────────────────────────────────────────────────

        [Fact]
        public void GetCurrentUserEmail_KimlikDogrulanmis_MailDoner()
        {
            var service = CreateService(nameof(GetCurrentUserEmail_KimlikDogrulanmis_MailDoner), "test@test.com");

            var result = service.GetCurrentUserEmail();

            result.Should().Be("test@test.com");
        }

        [Fact]
        public void GetCurrentUserEmail_Anonim_NullDoner()
        {
            var factory = TestDbContextFactory.CreateMockFactory();
            var httpAccessor = FakeHttpContextAccessor.CreateAnonymous();
            var service = new KullaniciService(factory.Object, _mockLog.Object, httpAccessor.Object);

            var result = service.GetCurrentUserEmail();

            result.Should().BeNull();
        }
    }
}
