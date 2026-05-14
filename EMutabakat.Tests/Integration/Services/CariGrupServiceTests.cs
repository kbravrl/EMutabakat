using EMutabakat.Models;
using EMutabakat.Services;
using EMutabakat.Services.Interfaces;
using EMutabakat.Tests.Testing;
using FluentAssertions;
using Moq;
using Xunit;

namespace EMutabakat.Tests.Integration.Services
{
    public class CariGrupServiceTests
    {
        private readonly Mock<ILogService> _mockLog;

        public CariGrupServiceTests()
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

        private CariGrupService CreateService(string dbName, string? userEmail = null)
        {
            var factory = TestDbContextFactory.CreateMockFactory(dbName);
            var httpAccessor = userEmail != null
                ? FakeHttpContextAccessor.CreateAuthenticated(userEmail)
                : FakeHttpContextAccessor.CreateAnonymous();
            return new CariGrupService(factory.Object, _mockLog.Object, httpAccessor.Object);
        }

        private static async Task SeedFirmaAsync(string dbName, int firmaId = 1)
        {
            await using var ctx = TestDbContextFactory.Create(dbName);
            if (!ctx.Firmalar.Any(f => f.FirmaId == firmaId))
            {
                ctx.Firmalar.Add(new Firma
                {
                    FirmaId = firmaId,
                    FirmaAdi = "Test Firma",
                    FirmaVergiDairesi = "Test VD",
                    FirmaVergiNumarasi = "1234567890",
                    FirmaYetkiliAdiSoyadi = "Test Yetkili",
                    FirmaMail = "firma@test.com",
                    FirmaTelefon = "02121234567",
                    FirmaSmtpHost = "smtp.test.com",
                    FirmaSmtpPort = 587,
                    FirmaSmtpUser = "smtp@test.com",
                    FirmaSmtpPassword = "pass",
                    FirmaSmtpSecure = "true"
                });
                await ctx.SaveChangesAsync();
            }
        }

        // ─── GenerateNextCariGrupIdAsync ─────────────────────────────────────────

        [Fact]
        public async Task GenerateNextCariGrupIdAsync_BosDatabasede_P1Doner()
        {
            var service = CreateService(nameof(GenerateNextCariGrupIdAsync_BosDatabasede_P1Doner));

            var result = await service.GenerateNextCariGrupIdAsync();

            result.Should().Be("P1");
        }

        [Fact]
        public async Task GenerateNextCariGrupIdAsync_MevcutKayitlarla_SonrakiIdDoner()
        {
            var dbName = nameof(GenerateNextCariGrupIdAsync_MevcutKayitlarla_SonrakiIdDoner);
            await SeedFirmaAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.CariGruplar.AddRange(
                    new CariGrup { CariGrupId = "P1", FirmaId = 1, CariGrupAdi = "Grup A", CariGrupAktifPasif = 1 },
                    new CariGrup { CariGrupId = "P5", FirmaId = 1, CariGrupAdi = "Grup B", CariGrupAktifPasif = 1 }
                );
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.GenerateNextCariGrupIdAsync();

            result.Should().Be("P6");
        }

        // ─── AddAsync ────────────────────────────────────────────────────────────

        [Fact]
        public async Task AddAsync_GecerliKayit_EklenirveDoner()
        {
            var dbName = nameof(AddAsync_GecerliKayit_EklenirveDoner);
            await SeedFirmaAsync(dbName);

            var service = CreateService(dbName);
            var grup = new CariGrup
            {
                CariGrupId = "P1",
                FirmaId = 1,
                CariGrupAdi = "Yeni Grup",
                CariGrupAktifPasif = 1
            };

            var result = await service.AddAsync(grup);

            result.Should().NotBeNull();
            result.CariGrupId.Should().Be("P1");
            result.CariGrupAdi.Should().Be("Yeni Grup");
        }

        [Fact]
        public async Task AddAsync_FirmaIdSifir_ExceptionFirlatir()
        {
            var service = CreateService(nameof(AddAsync_FirmaIdSifir_ExceptionFirlatir));

            var grup = new CariGrup { CariGrupId = "P1", FirmaId = 0, CariGrupAdi = "Grup", CariGrupAktifPasif = 1 };

            await FluentActions.Invoking(() => service.AddAsync(grup)).Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task AddAsync_BosGrupId_ExceptionFirlatir()
        {
            var dbName = nameof(AddAsync_BosGrupId_ExceptionFirlatir);
            await SeedFirmaAsync(dbName);

            var service = CreateService(dbName);
            var grup = new CariGrup { CariGrupId = "  ", FirmaId = 1, CariGrupAdi = "Grup", CariGrupAktifPasif = 1 };

            await FluentActions.Invoking(() => service.AddAsync(grup)).Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task AddAsync_BosGrupAdi_ExceptionFirlatir()
        {
            var dbName = nameof(AddAsync_BosGrupAdi_ExceptionFirlatir);
            await SeedFirmaAsync(dbName);

            var service = CreateService(dbName);
            var grup = new CariGrup { CariGrupId = "P1", FirmaId = 1, CariGrupAdi = "", CariGrupAktifPasif = 1 };

            await FluentActions.Invoking(() => service.AddAsync(grup)).Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task AddAsync_MukerrerGrupId_ExceptionFirlatir()
        {
            var dbName = nameof(AddAsync_MukerrerGrupId_ExceptionFirlatir);
            await SeedFirmaAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.CariGruplar.Add(new CariGrup { CariGrupId = "P1", FirmaId = 1, CariGrupAdi = "Mevcut", CariGrupAktifPasif = 1 });
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var grup = new CariGrup { CariGrupId = "P1", FirmaId = 1, CariGrupAdi = "Yeni", CariGrupAktifPasif = 1 };

            await FluentActions.Invoking(() => service.AddAsync(grup)).Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task AddAsync_YokFirma_ExceptionFirlatir()
        {
            var service = CreateService(nameof(AddAsync_YokFirma_ExceptionFirlatir));

            var grup = new CariGrup { CariGrupId = "P1", FirmaId = 999, CariGrupAdi = "Grup", CariGrupAktifPasif = 1 };

            await FluentActions.Invoking(() => service.AddAsync(grup)).Should().ThrowAsync<Exception>();
        }

        // ─── GetByIdAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_MevcutKayit_GrupDoner()
        {
            var dbName = nameof(GetByIdAsync_MevcutKayit_GrupDoner);
            await SeedFirmaAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.CariGruplar.Add(new CariGrup { CariGrupId = "P1", FirmaId = 1, CariGrupAdi = "Grup A", CariGrupAktifPasif = 1 });
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName, userEmail: null);
            var result = await service.GetByIdAsync("P1", 1);

            result.Should().NotBeNull();
            result!.CariGrupAdi.Should().Be("Grup A");
        }

        [Fact]
        public async Task GetByIdAsync_YokKayit_NullDoner()
        {
            var service = CreateService(nameof(GetByIdAsync_YokKayit_NullDoner), userEmail: null);

            var result = await service.GetByIdAsync("YOK", 1);

            result.Should().BeNull();
        }

        // ─── UpdateAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_MevcutKayit_GuncellenirVeDoner()
        {
            var dbName = nameof(UpdateAsync_MevcutKayit_GuncellenirVeDoner);
            await SeedFirmaAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.CariGruplar.Add(new CariGrup { CariGrupId = "P1", FirmaId = 1, CariGrupAdi = "Eski Ad", CariGrupAktifPasif = 1 });
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var updated = new CariGrup
            {
                CariGrupId = "P1",
                OriginalCariGrupId = "P1",
                FirmaId = 1,
                OriginalFirmaId = 1,
                CariGrupAdi = "Yeni Ad",
                CariGrupAktifPasif = 0
            };

            var result = await service.UpdateAsync(updated);

            result.Should().NotBeNull();
            result!.CariGrupAdi.Should().Be("Yeni Ad");
            result.CariGrupAktifPasif.Should().Be(0);
        }

        [Fact]
        public async Task UpdateAsync_YokKayit_NullDoner()
        {
            var dbName = nameof(UpdateAsync_YokKayit_NullDoner);
            await SeedFirmaAsync(dbName);

            var service = CreateService(dbName);
            var updated = new CariGrup
            {
                CariGrupId = "YOK",
                OriginalCariGrupId = "YOK",
                FirmaId = 1,
                OriginalFirmaId = 1,
                CariGrupAdi = "Ad",
                CariGrupAktifPasif = 1
            };

            var result = await service.UpdateAsync(updated);

            result.Should().BeNull();
        }

        // ─── DeleteAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_MevcutKayit_TrueDoner()
        {
            var dbName = nameof(DeleteAsync_MevcutKayit_TrueDoner);
            await SeedFirmaAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.CariGruplar.Add(new CariGrup { CariGrupId = "P1", FirmaId = 1, CariGrupAdi = "Silinecek", CariGrupAktifPasif = 1 });
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.DeleteAsync("P1", 1);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_YokKayit_FalseDoner()
        {
            var service = CreateService(nameof(DeleteAsync_YokKayit_FalseDoner));

            var result = await service.DeleteAsync("YOK", 1);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_SilindiktenSonra_GetByIdNullDoner()
        {
            var dbName = nameof(DeleteAsync_SilindiktenSonra_GetByIdNullDoner);
            await SeedFirmaAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.CariGruplar.Add(new CariGrup { CariGrupId = "P1", FirmaId = 1, CariGrupAdi = "Silinecek", CariGrupAktifPasif = 1 });
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName, userEmail: null);
            await service.DeleteAsync("P1", 1);

            var result = await service.GetByIdAsync("P1", 1);
            result.Should().BeNull();
        }

        // ─── GetAllAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_AnonimKullanici_TumKayitlariDoner()
        {
            var dbName = nameof(GetAllAsync_AnonimKullanici_TumKayitlariDoner);
            await SeedFirmaAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.CariGruplar.AddRange(
                    new CariGrup { CariGrupId = "P1", FirmaId = 1, CariGrupAdi = "Grup A", CariGrupAktifPasif = 1 },
                    new CariGrup { CariGrupId = "P2", FirmaId = 1, CariGrupAdi = "Grup B", CariGrupAktifPasif = 0 }
                );
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName, userEmail: null);
            var result = await service.GetAllAsync();

            result.Should().HaveCount(2);
        }
    }
}
