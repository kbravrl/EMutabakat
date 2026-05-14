using EMutabakat.Models;
using EMutabakat.Services;
using EMutabakat.Services.Interfaces;
using EMutabakat.Tests.Testing;
using FluentAssertions;
using Moq;
using Xunit;

namespace EMutabakat.Tests.Integration.Services
{
    public class FirmaServiceTests
    {
        private readonly Mock<ILogService> _mockLog;

        public FirmaServiceTests()
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

        private FirmaService CreateService(string dbName, string? userEmail = null)
        {
            var factory = TestDbContextFactory.CreateMockFactory(dbName);
            var httpAccessor = userEmail != null
                ? FakeHttpContextAccessor.CreateAuthenticated(userEmail)
                : FakeHttpContextAccessor.CreateAnonymous();
            return new FirmaService(factory.Object, _mockLog.Object, httpAccessor.Object);
        }

        private static Firma CreateSampleFirma(int id = 1)
        {
            return new Firma
            {
                FirmaId = id,
                FirmaAdi = $"Test Firma {id}",
                FirmaVergiDairesi = "Test VD",
                FirmaVergiNumarasi = "1234567890",
                FirmaYetkiliAdiSoyadi = "Test Yetkili",
                FirmaMail = $"firma{id}@test.com",
                FirmaTelefon = "02121234567",
                FirmaSmtpHost = "smtp.test.com",
                FirmaSmtpPort = 587,
                FirmaSmtpUser = $"smtp{id}@test.com",
                FirmaSmtpPassword = "pass",
                FirmaSmtpSecure = "true",
                FirmaAktifPasif = 1
            };
        }

        private static async Task SeedUserAsync(string dbName, string email, bool isSeed = false, List<int>? firmaIds = null)
        {
            await using var ctx = TestDbContextFactory.Create(dbName);
            var kullanici = new Kullanici
            {
                KullaniciId = "P1",
                KullaniciAdi = "Test",
                KullaniciSoyadi = "User",
                KullaniciMail = email,
                KullaniciAktifPasif = "1",
                IsSeedUser = isSeed,
                Yetkileri = new KullaniciYetki { KullaniciId = "P1" }
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

        // ─── GetAllAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_AnonimKullanici_BosListeDoner()
        {
            var service = CreateService(nameof(GetAllAsync_AnonimKullanici_BosListeDoner), userEmail: null);

            var result = await service.GetAllAsync();

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_SeedKullanici_TumFirmalariDoner()
        {
            var dbName = nameof(GetAllAsync_SeedKullanici_TumFirmalariDoner);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Firmalar.AddRange(CreateSampleFirma(1), CreateSampleFirma(2));
                await ctx.SaveChangesAsync();
            }
            await SeedUserAsync(dbName, "seed@test.com", isSeed: true, firmaIds: new List<int> { 1, 2 });

            var service = CreateService(dbName, "seed@test.com");
            var result = await service.GetAllAsync();

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllAsync_NormalKullanici_SadeceKendiFirmalari()
        {
            var dbName = nameof(GetAllAsync_NormalKullanici_SadeceKendiFirmalari);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Firmalar.AddRange(CreateSampleFirma(1), CreateSampleFirma(2), CreateSampleFirma(3));
                await ctx.SaveChangesAsync();
            }
            await SeedUserAsync(dbName, "user@test.com", isSeed: false, firmaIds: new List<int> { 1, 2 });

            var service = CreateService(dbName, "user@test.com");
            var result = await service.GetAllAsync();

            result.Should().HaveCount(2);
            result.Should().OnlyContain(f => new[] { 1, 2 }.Contains(f.FirmaId));
        }

        // ─── GetByIdAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_MevcutFirma_FirmaDoner()
        {
            var dbName = nameof(GetByIdAsync_MevcutFirma_FirmaDoner);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Firmalar.Add(CreateSampleFirma(1));
                await ctx.SaveChangesAsync();
            }
            await SeedUserAsync(dbName, "user@test.com", isSeed: true, firmaIds: new List<int> { 1 });

            var service = CreateService(dbName, "user@test.com");
            var result = await service.GetByIdAsync(1);

            result.Should().NotBeNull();
            result!.FirmaId.Should().Be(1);
        }

        [Fact]
        public async Task GetByIdAsync_YokFirma_NullDoner()
        {
            var dbName = nameof(GetByIdAsync_YokFirma_NullDoner);
            await SeedUserAsync(dbName, "user@test.com", isSeed: true);

            var service = CreateService(dbName, "user@test.com");
            var result = await service.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_KullaniciYok_NullDoner()
        {
            var dbName = nameof(GetByIdAsync_KullaniciYok_NullDoner);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Firmalar.Add(CreateSampleFirma(1));
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName, "yok@test.com");
            var result = await service.GetByIdAsync(1);

            result.Should().BeNull();
        }

        // ─── AddAsync ────────────────────────────────────────────────────────────

        [Fact]
        public async Task AddAsync_GecerliFirma_EklenirveDoner()
        {
            var dbName = nameof(AddAsync_GecerliFirma_EklenirveDoner);
            await SeedUserAsync(dbName, "user@test.com", isSeed: false);

            var service = CreateService(dbName, "user@test.com");
            var firma = CreateSampleFirma(1);

            var result = await service.AddAsync(firma);

            result.Should().NotBeNull();
            result.FirmaAdi.Should().Be("Test Firma 1");
        }

        [Fact]
        public async Task AddAsync_KullaniciYok_UnauthorizedExceptionFirlatir()
        {
            var service = CreateService(nameof(AddAsync_KullaniciYok_UnauthorizedExceptionFirlatir), "yok@test.com");

            var firma = CreateSampleFirma(1);

            await FluentActions.Invoking(() => service.AddAsync(firma)).Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task AddAsync_MailNormalize_KucukHarfeDonusur()
        {
            var dbName = nameof(AddAsync_MailNormalize_KucukHarfeDonusur);
            await SeedUserAsync(dbName, "user@test.com", isSeed: false);

            var service = CreateService(dbName, "user@test.com");
            var firma = CreateSampleFirma(1);
            firma.FirmaMail = "FIRMA@TEST.COM";
            firma.FirmaSmtpUser = "SMTP@TEST.COM";

            var result = await service.AddAsync(firma);

            result.Should().NotBeNull();
            result.FirmaMail.Should().Be(result.FirmaMail!.ToLower());
            result.FirmaSmtpUser.Should().Be(result.FirmaSmtpUser!.ToLower());
        }

        // ─── UpdateAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_MevcutFirma_GuncellenirVeDoner()
        {
            var dbName = nameof(UpdateAsync_MevcutFirma_GuncellenirVeDoner);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Firmalar.Add(CreateSampleFirma(1));
                await ctx.SaveChangesAsync();
            }
            await SeedUserAsync(dbName, "user@test.com", isSeed: true, firmaIds: new List<int> { 1 });

            var service = CreateService(dbName, "user@test.com");
            var updated = CreateSampleFirma(1);
            updated.FirmaAdi = "Güncellenmiş Firma";

            var result = await service.UpdateAsync(updated);

            result.Should().NotBeNull();
            result!.FirmaAdi.Should().Be("Güncellenmiş Firma");
        }

        [Fact]
        public async Task UpdateAsync_YokFirma_NullDoner()
        {
            var dbName = nameof(UpdateAsync_YokFirma_NullDoner);
            await SeedUserAsync(dbName, "user@test.com", isSeed: true);

            var service = CreateService(dbName, "user@test.com");
            var updated = CreateSampleFirma(999);

            var result = await service.UpdateAsync(updated);

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_KullaniciYetkisiYok_NullDoner()
        {
            var dbName = nameof(UpdateAsync_KullaniciYetkisiYok_NullDoner);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Firmalar.Add(CreateSampleFirma(1));
                await ctx.SaveChangesAsync();
            }
            await SeedUserAsync(dbName, "user@test.com", isSeed: false, firmaIds: new List<int> { 2 });

            var service = CreateService(dbName, "user@test.com");
            var updated = CreateSampleFirma(1);
            updated.FirmaAdi = "Güncellenmiş";

            var result = await service.UpdateAsync(updated);

            result.Should().BeNull();
        }

        // ─── DeleteAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_MevcutFirma_TrueDoner()
        {
            var dbName = nameof(DeleteAsync_MevcutFirma_TrueDoner);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Firmalar.Add(CreateSampleFirma(1));
                await ctx.SaveChangesAsync();
            }
            await SeedUserAsync(dbName, "user@test.com", isSeed: true);

            var service = CreateService(dbName, "user@test.com");
            var result = await service.DeleteAsync(1);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_YokFirma_FalseDoner()
        {
            var dbName = nameof(DeleteAsync_YokFirma_FalseDoner);
            await SeedUserAsync(dbName, "user@test.com", isSeed: true);

            var service = CreateService(dbName, "user@test.com");
            var result = await service.DeleteAsync(999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_KullaniciYok_FalseDoner()
        {
            var dbName = nameof(DeleteAsync_KullaniciYok_FalseDoner);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Firmalar.Add(CreateSampleFirma(1));
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName, "yok@test.com");
            var result = await service.DeleteAsync(1);

            result.Should().BeFalse();
        }
    }
}
