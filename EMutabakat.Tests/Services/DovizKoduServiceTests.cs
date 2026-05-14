using EMutabakat.Models;
using EMutabakat.Services;
using EMutabakat.Services.Interfaces;
using EMutabakat.Tests.Testing;
using Moq;
using Xunit;

namespace EMutabakat.Tests.Services
{
    public class DovizKoduServiceTests
    {
        private readonly Mock<ILogService> _mockLog;

        public DovizKoduServiceTests()
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

        private DovizKoduService CreateService(string dbName)
        {
            var factory = TestDbContextFactory.CreateMockFactory(dbName);
            var httpAccessor = FakeHttpContextAccessor.CreateAuthenticated("admin@test.com");
            return new DovizKoduService(factory.Object, _mockLog.Object, httpAccessor.Object);
        }

        // ─── GetAllAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_BosDatabasede_BosListeDoner()
        {
            var service = CreateService(nameof(GetAllAsync_BosDatabasede_BosListeDoner));

            var result = await service.GetAllAsync();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_KayitVarsa_TumKayitlariDoner()
        {
            var dbName = nameof(GetAllAsync_KayitVarsa_TumKayitlariDoner);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.DovizKodlari.AddRange(
                    new DovizKodu { TCMB = "USD", Name = "Amerikan Doları", DovizKoduAktifPasif = 1 },
                    new DovizKodu { TCMB = "EUR", Name = "Euro", DovizKoduAktifPasif = 1 },
                    new DovizKodu { TCMB = "GBP", Name = "İngiliz Sterlini", DovizKoduAktifPasif = 0 }
                );
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.GetAllAsync();

            Assert.Equal(3, result.Count);
        }

        // ─── GetActiveAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetActiveAsync_SadeceAktifKayitlariDoner()
        {
            var dbName = nameof(GetActiveAsync_SadeceAktifKayitlariDoner);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.DovizKodlari.AddRange(
                    new DovizKodu { TCMB = "USD", Name = "Amerikan Doları", DovizKoduAktifPasif = 1 },
                    new DovizKodu { TCMB = "EUR", Name = "Euro", DovizKoduAktifPasif = 1 },
                    new DovizKodu { TCMB = "GBP", Name = "İngiliz Sterlini", DovizKoduAktifPasif = 0 }
                );
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.GetActiveAsync();

            Assert.Equal(2, result.Count);
            Assert.All(result, d => Assert.Equal(1, d.DovizKoduAktifPasif));
        }

        // ─── GetByTcmbAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetByTcmbAsync_MevcutKod_KaydiDoner()
        {
            var dbName = nameof(GetByTcmbAsync_MevcutKod_KaydiDoner);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.DovizKodlari.Add(new DovizKodu { TCMB = "USD", Name = "Amerikan Doları", DovizKoduAktifPasif = 1 });
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.GetByTcmbAsync("usd"); // küçük harf — normalize edilmeli

            Assert.NotNull(result);
            Assert.Equal("USD", result!.TCMB);
        }

        [Fact]
        public async Task GetByTcmbAsync_YokKod_NullDoner()
        {
            var service = CreateService(nameof(GetByTcmbAsync_YokKod_NullDoner));

            var result = await service.GetByTcmbAsync("XYZ");

            Assert.Null(result);
        }

        // ─── AddAsync ────────────────────────────────────────────────────────────

        [Fact]
        public async Task AddAsync_GecerliKayit_EklenirveDoner()
        {
            var service = CreateService(nameof(AddAsync_GecerliKayit_EklenirveDoner));

            var doviz = new DovizKodu { TCMB = "usd", Name = "Amerikan Doları", DovizKoduAktifPasif = 1 };
            var result = await service.AddAsync(doviz);

            Assert.NotNull(result);
            Assert.Equal("USD", result.TCMB); // büyük harfe normalize edilmeli
        }

        [Fact]
        public async Task AddAsync_BosTcmb_ExceptionFirlatir()
        {
            var service = CreateService(nameof(AddAsync_BosTcmb_ExceptionFirlatir));

            var doviz = new DovizKodu { TCMB = "  ", Name = "Test", DovizKoduAktifPasif = 1 };

            await Assert.ThrowsAsync<Exception>(() => service.AddAsync(doviz));
        }

        [Fact]
        public async Task AddAsync_BosName_ExceptionFirlatir()
        {
            var service = CreateService(nameof(AddAsync_BosName_ExceptionFirlatir));

            var doviz = new DovizKodu { TCMB = "TST", Name = "", DovizKoduAktifPasif = 1 };

            await Assert.ThrowsAsync<Exception>(() => service.AddAsync(doviz));
        }

        [Fact]
        public async Task AddAsync_MukerrerTcmb_ExceptionFirlatir()
        {
            var dbName = nameof(AddAsync_MukerrerTcmb_ExceptionFirlatir);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.DovizKodlari.Add(new DovizKodu { TCMB = "USD", Name = "Amerikan Doları", DovizKoduAktifPasif = 1 });
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var doviz = new DovizKodu { TCMB = "USD", Name = "Başka Dolar", DovizKoduAktifPasif = 1 };

            await Assert.ThrowsAsync<Exception>(() => service.AddAsync(doviz));
        }

        [Fact]
        public async Task AddAsync_GeçersizAktifPasif_ExceptionFirlatir()
        {
            var service = CreateService(nameof(AddAsync_GeçersizAktifPasif_ExceptionFirlatir));

            var doviz = new DovizKodu { TCMB = "TST", Name = "Test", DovizKoduAktifPasif = 5 };

            await Assert.ThrowsAsync<Exception>(() => service.AddAsync(doviz));
        }

        // ─── UpdateAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_MevcutKayit_GuncellenirVeDoner()
        {
            var dbName = nameof(UpdateAsync_MevcutKayit_GuncellenirVeDoner);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.DovizKodlari.Add(new DovizKodu { TCMB = "USD", Name = "Eski Ad", DovizKoduAktifPasif = 1 });
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var updated = new DovizKodu { TCMB = "USD", Name = "Yeni Ad", DovizKoduAktifPasif = 0 };

            var result = await service.UpdateAsync(updated, "USD");

            Assert.NotNull(result);
            Assert.Equal("Yeni Ad", result!.Name);
            Assert.Equal(0, result.DovizKoduAktifPasif);
        }

        [Fact]
        public async Task UpdateAsync_YokKayit_NullDoner()
        {
            var service = CreateService(nameof(UpdateAsync_YokKayit_NullDoner));

            var updated = new DovizKodu { TCMB = "XYZ", Name = "Yok", DovizKoduAktifPasif = 1 };

            var result = await service.UpdateAsync(updated, "XYZ");

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_BosTcmb_ExceptionFirlatir()
        {
            var dbName = nameof(UpdateAsync_BosTcmb_ExceptionFirlatir);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.DovizKodlari.Add(new DovizKodu { TCMB = "USD", Name = "Dolar", DovizKoduAktifPasif = 1 });
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var updated = new DovizKodu { TCMB = "", Name = "Yeni Ad", DovizKoduAktifPasif = 1 };

            await Assert.ThrowsAsync<Exception>(() => service.UpdateAsync(updated, "USD"));
        }

        // ─── DeleteAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_MevcutKayit_TrueDoner()
        {
            var dbName = nameof(DeleteAsync_MevcutKayit_TrueDoner);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.DovizKodlari.Add(new DovizKodu { TCMB = "USD", Name = "Amerikan Doları", DovizKoduAktifPasif = 1 });
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.DeleteAsync("USD");

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteAsync_YokKayit_FalseDoner()
        {
            var service = CreateService(nameof(DeleteAsync_YokKayit_FalseDoner));

            var result = await service.DeleteAsync("XYZ");

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_SilindiktenSonra_GetByTcmbNullDoner()
        {
            var dbName = nameof(DeleteAsync_SilindiktenSonra_GetByTcmbNullDoner);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.DovizKodlari.Add(new DovizKodu { TCMB = "USD", Name = "Amerikan Doları", DovizKoduAktifPasif = 1 });
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            await service.DeleteAsync("USD");

            var result = await service.GetByTcmbAsync("USD");
            Assert.Null(result);
        }
    }
}
