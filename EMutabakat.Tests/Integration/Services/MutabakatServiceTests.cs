using EMutabakat.Models;
using EMutabakat.Services;
using EMutabakat.Services.Interfaces;
using EMutabakat.Tests.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using static EMutabakat.Models.Mutabakat;

namespace EMutabakat.Tests.Integration.Services
{
    public class MutabakatServiceTests
    {
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<ISdService> _mockSd;
        private readonly Mock<ILogService> _mockLog;
        private readonly IConfiguration _configuration;

        public MutabakatServiceTests()
        {
            _mockEmail = new Mock<IEmailService>();
            _mockSd = new Mock<ISdService>();
            _mockLog = new Mock<ILogService>();

            _mockEmail
                .Setup(x => x.SendMutabakatMailAsync(
                    It.IsAny<Mutabakat>(),
                    It.IsAny<Kullanici>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(true);

            _mockSd
                .Setup(x => x.DeleteMutabakatResponseFileAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

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

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AppSettings:BaseUrl"] = "https://test.local"
                })
                .Build();
        }

        private MutabakatService CreateService(string dbName, string? userEmail = "admin@test.com")
        {
            var factory = TestDbContextFactory.CreateMockFactory(dbName);
            var httpAccessor = userEmail != null
                ? FakeHttpContextAccessor.CreateAuthenticated(userEmail)
                : FakeHttpContextAccessor.CreateAnonymous();

            return new MutabakatService(
                factory.Object,
                _mockEmail.Object,
                _mockSd.Object,
                _configuration,
                httpAccessor.Object,
                _mockLog.Object);
        }

        private static Firma CreateFirma(int id = 1)
        {
            return new Firma
            {
                FirmaId = id,
                FirmaAdi = $"Test Firma {id}",
                FirmaVergiDairesi = "Test VD",
                FirmaVergiNumarasi = "1234567890",
                FirmaYetkiliAdiSoyadi = "Firma Yetkili",
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

        private static Cari CreateCari(string cariId = "P1", int firmaId = 1, string doviz = "TL", string grupId = "P1")
        {
            return new Cari
            {
                CariId = cariId,
                FirmaId = firmaId,
                CariAdi = $"Test Cari {cariId}",
                CariUnvan = "Test Cari",
                CariVergiDairesi = "Test VD",
                CariVergiNumarasi = "1234567890",
                CariYetkiliAdiSoyadi = "Cari Yetkili",
                CariYetkiliMail = "cari@test.com",
                CariYetkiliGsm = "05551234567",
                CariGrupId = grupId,
                CariDovizKodu = doviz,
                CariAktifPasif = 1
            };
        }

        private static DovizKodu CreateDoviz(string code = "TL")
        {
            return new DovizKodu
            {
                TCMB = code,
                Name = code,
                DovizKoduAktifPasif = 1
            };
        }

        private static Mutabakat CreateMutabakat(
            string id = "P1",
            string cariId = "P1",
            int firmaId = 1,
            decimal bakiye = 100,
            string doviz = "TL")
        {
            return new Mutabakat
            {
                MutabakatId = id,
                CariId = cariId,
                FirmaId = firmaId,
                MutabakatTarihi = new DateTime(2026, 5, 1),
                MutabakatDovizKodu = doviz,
                MutabakatBakiye = bakiye,
                MutabakatBakiyeTipi = "B",
                MutabakatAciklama = "Test açıklama",
                MutabakatToken = $"token-{id}",
                Status = MutabakatStatus.Kaydedildi
            };
        }

        private static async Task SeedBaseDataAsync(string dbName)
        {
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Firmalar.Add(CreateFirma(1));
                ctx.DovizKodlari.Add(CreateDoviz("TL"));
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.CariGruplar.Add(new CariGrup { CariGrupId = "P1", FirmaId = 1, CariGrupAdi = "Test Grup", CariGrupAktifPasif = 1 });
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Cariler.Add(CreateCari("P1", 1, "TL", "P1"));
                await ctx.SaveChangesAsync();
            }
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
                foreach (var firmaId in firmaIds)
                {
                    var firma = await ctx.Firmalar.FindAsync(firmaId);
                    if (firma != null)
                        kullanici.Firmalar.Add(firma);
                }
                await ctx.SaveChangesAsync();
            }
        }

        // ─── GenerateNextMutabakatIdAsync ───────────────────────────────────────

        [Fact]
        public async Task GenerateNextMutabakatIdAsync_BosDatabasede_P1Doner()
        {
            var service = CreateService(nameof(GenerateNextMutabakatIdAsync_BosDatabasede_P1Doner));

            var result = await service.GenerateNextMutabakatIdAsync();

            Assert.Equal("P1", result);
        }

        [Fact]
        public async Task GenerateNextMutabakatIdAsync_MevcutKayitlarla_SonrakiIdDoner()
        {
            var dbName = nameof(GenerateNextMutabakatIdAsync_MevcutKayitlarla_SonrakiIdDoner);
            await SeedBaseDataAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                var m1 = CreateMutabakat("P1");
                m1.MutabakatTarihi = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var m7 = CreateMutabakat("P7");
                m7.MutabakatTarihi = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
                ctx.Mutabakatlar.AddRange(m1, m7);
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.GenerateNextMutabakatIdAsync();

            Assert.Equal("P8", result);
        }

        // ─── AddAsync ────────────────────────────────────────────────────────────

        [Fact]
        public async Task AddAsync_GecerliMutabakat_KaydedildiStatusuIleEklenir()
        {
            var dbName = nameof(AddAsync_GecerliMutabakat_KaydedildiStatusuIleEklenir);
            await SeedBaseDataAsync(dbName);
            var service = CreateService(dbName);

            var result = await service.AddAsync(CreateMutabakat(bakiye: -250));

            Assert.NotNull(result);
            Assert.Equal(MutabakatStatus.Kaydedildi, result.Status);
            Assert.Equal(250, result.MutabakatBakiye);
            Assert.Equal(DateTimeKind.Utc, result.MutabakatTarihi.Kind);
        }

        [Fact]
        public async Task AddAsync_CariMailVerilirse_CariMailNormalizeEdilir()
        {
            var dbName = nameof(AddAsync_CariMailVerilirse_CariMailNormalizeEdilir);
            await SeedBaseDataAsync(dbName);
            var service = CreateService(dbName);

            await service.AddAsync(CreateMutabakat(), "YETKILI@TEST.COM");

            await using var ctx = TestDbContextFactory.Create(dbName);
            var cari = await ctx.Cariler.FindAsync("P1", 1);
            Assert.NotNull(cari);
            Assert.Equal("yetkili@test.com", cari!.CariYetkiliMail);
        }

        [Fact]
        public async Task AddAsync_AyniFirmaCariTarihVeAyniBakiyeVarsa_ExceptionFirlatir()
        {
            var dbName = nameof(AddAsync_AyniFirmaCariTarihVeAyniBakiyeVarsa_ExceptionFirlatir);
            await SeedBaseDataAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Mutabakatlar.Add(CreateMutabakat("P1", bakiye: 100));
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var yeni = CreateMutabakat("P2", bakiye: 100);

            await Assert.ThrowsAsync<Exception>(() => service.AddAsync(yeni));
        }

        [Fact]
        public async Task AddAsync_AyniFirmaCariTarihFarkliBakiyeVarsa_EskiKaydiArsivlerVeYenisiniEkler()
        {
            var dbName = nameof(AddAsync_AyniFirmaCariTarihFarkliBakiyeVarsa_EskiKaydiArsivlerVeYenisiniEkler);
            await SeedBaseDataAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Mutabakatlar.Add(CreateMutabakat("M1", bakiye: 100));
                await ctx.SaveChangesAsync();
            }

            var service = CreateService(dbName);
            var result = await service.AddAsync(CreateMutabakat("M2", bakiye: 200));

            Assert.Equal("M2", result.MutabakatId);

            await using var assertCtx = TestDbContextFactory.Create(dbName);
            Assert.False(assertCtx.Mutabakatlar.Any(x => x.MutabakatId == "M1"));
            Assert.True(assertCtx.Mutabakatlar.Any(x => x.MutabakatId == "M2"));
            Assert.Single(assertCtx.SilinenMutabakatlar);
        }

        [Fact]
        public async Task AddAsync_GecersizDovizKodu_ExceptionFirlatir()
        {
            var dbName = nameof(AddAsync_GecersizDovizKodu_ExceptionFirlatir);
            await SeedBaseDataAsync(dbName);
            var service = CreateService(dbName);
            var mutabakat = CreateMutabakat(doviz: "XXX");

            await Assert.ThrowsAsync<Exception>(() => service.AddAsync(mutabakat));
        }

        // ─── GetAllAsync / GetByIdAsync ─────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_NormalKullanici_SadeceYetkiliOlduguFirmalariGorur()
        {
            var dbName = nameof(GetAllAsync_NormalKullanici_SadeceYetkiliOlduguFirmalariGorur);
            await SeedBaseDataAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Firmalar.Add(CreateFirma(2));
                ctx.CariGruplar.Add(new CariGrup { CariGrupId = "P2", FirmaId = 2, CariGrupAdi = "Test Grup 2", CariGrupAktifPasif = 1 });
                ctx.Cariler.Add(CreateCari("P2", 2, "TL", "P2"));
                ctx.Mutabakatlar.AddRange(
                    CreateMutabakat("P1", "P1", 1),
                    CreateMutabakat("P2", "P2", 2));
                await ctx.SaveChangesAsync();
            }
            await SeedUserAsync(dbName, "user@test.com", isSeed: false, firmaIds: new List<int> { 1 });

            var service = CreateService(dbName, "user@test.com");
            var result = await service.GetAllAsync();

            Assert.Single(result);
            Assert.Equal(1, result[0].FirmaId);
        }

        [Fact]
        public async Task GetByIdAsync_YetkisiOlmayanFirma_NullDoner()
        {
            var dbName = nameof(GetByIdAsync_YetkisiOlmayanFirma_NullDoner);
            await SeedBaseDataAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Mutabakatlar.Add(CreateMutabakat("M1", "C1", 1));
                await ctx.SaveChangesAsync();
            }
            await SeedUserAsync(dbName, "user@test.com", isSeed: false, firmaIds: new List<int>());

            var service = CreateService(dbName, "user@test.com");
            var result = await service.GetByIdAsync("M1");

            Assert.Null(result);
        }

        // ─── UpdateAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_MevcutKayit_GuncellenirVeBakiyePozitifeCevrilir()
        {
            var dbName = nameof(UpdateAsync_MevcutKayit_GuncellenirVeBakiyePozitifeCevrilir);
            await SeedBaseDataAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Mutabakatlar.Add(CreateMutabakat("M1", bakiye: 100));
                await ctx.SaveChangesAsync();
            }
            var service = CreateService(dbName);

            var updated = CreateMutabakat("M1", bakiye: -500);
            updated.OriginalMutabakatId = "M1";
            updated.MutabakatAciklama = " Güncellendi ";

            var result = await service.UpdateAsync(updated);

            Assert.NotNull(result);
            Assert.Equal(500, result!.MutabakatBakiye);
            Assert.Equal("Güncellendi", result.MutabakatAciklama);
        }

        [Fact]
        public async Task UpdateAsync_YokKayit_NullDoner()
        {
            var dbName = nameof(UpdateAsync_YokKayit_NullDoner);
            await SeedBaseDataAsync(dbName);
            var service = CreateService(dbName);

            var result = await service.UpdateAsync(CreateMutabakat("YOK"));

            Assert.Null(result);
        }

        // ─── DeleteAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_MevcutKayit_TrueDonerVeDosyaVarsaSiler()
        {
            var dbName = nameof(DeleteAsync_MevcutKayit_TrueDonerVeDosyaVarsaSiler);
            await SeedBaseDataAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                var mutabakat = CreateMutabakat("M1");
                mutabakat.MutabakatReceiveStoragePath = "responses/test.pdf";
                ctx.Mutabakatlar.Add(mutabakat);
                await ctx.SaveChangesAsync();
            }
            var service = CreateService(dbName);

            var result = await service.DeleteAsync("M1");

            Assert.True(result);
            _mockSd.Verify(x => x.DeleteMutabakatResponseFileAsync("responses/test.pdf", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_YokKayit_FalseDoner()
        {
            var service = CreateService(nameof(DeleteAsync_YokKayit_FalseDoner));

            var result = await service.DeleteAsync("YOK");

            Assert.False(result);
        }

        // ─── SendMailAsync / SendReminderAsync ──────────────────────────────────

        [Fact]
        public async Task SendMailAsync_GecerliMutabakat_StatusGonderildiYapar()
        {
            var dbName = nameof(SendMailAsync_GecerliMutabakat_StatusGonderildiYapar);
            await SeedBaseDataAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Mutabakatlar.Add(CreateMutabakat("M1"));
                await ctx.SaveChangesAsync();
            }
            await SeedUserAsync(dbName, "admin@test.com", isSeed: false, firmaIds: new List<int> { 1 });
            var service = CreateService(dbName, "admin@test.com");

            var result = await service.SendMailAsync("M1");

            Assert.True(result);
            await using var assertCtx = TestDbContextFactory.Create(dbName);
            var saved = await assertCtx.Mutabakatlar.FirstOrDefaultAsync(x => x.MutabakatId == "P1");
            Assert.NotNull(saved);
            Assert.Equal(MutabakatStatus.Gonderildi, saved!.Status);
            Assert.NotNull(saved.MutabakatGonderimTarihSaat);
        }

        [Fact]
        public async Task SendMailAsync_MutabikKayit_FalseDonerVeMailGondermez()
        {
            var dbName = nameof(SendMailAsync_MutabikKayit_FalseDonerVeMailGondermez);
            await SeedBaseDataAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                var mutabakat = CreateMutabakat("M1");
                mutabakat.Status = MutabakatStatus.Mutabik;
                ctx.Mutabakatlar.Add(mutabakat);
                await ctx.SaveChangesAsync();
            }
            await SeedUserAsync(dbName, "admin@test.com", isSeed: false, firmaIds: new List<int> { 1 });
            var service = CreateService(dbName, "admin@test.com");

            var result = await service.SendMailAsync("M1");

            Assert.False(result);
            _mockEmail.Verify(x => x.SendMutabakatMailAsync(
                It.IsAny<Mutabakat>(), It.IsAny<Kullanici>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task SendReminderAsync_GecerliMutabakat_StatusHatirlatmaYapar()
        {
            var dbName = nameof(SendReminderAsync_GecerliMutabakat_StatusHatirlatmaYapar);
            await SeedBaseDataAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Mutabakatlar.Add(CreateMutabakat("M1"));
                await ctx.SaveChangesAsync();
            }
            await SeedUserAsync(dbName, "admin@test.com", isSeed: false, firmaIds: new List<int> { 1 });
            var service = CreateService(dbName, "admin@test.com");

            var result = await service.SendReminderAsync("M1");

            Assert.True(result);
            await using var assertCtx = TestDbContextFactory.Create(dbName);
            var saved = await assertCtx.Mutabakatlar.FirstOrDefaultAsync(x => x.MutabakatId == "P1");
            Assert.Equal(MutabakatStatus.Hatirlatma, saved!.Status);
            _mockEmail.Verify(x => x.SendMutabakatMailAsync(
                It.IsAny<Mutabakat>(), It.IsAny<Kullanici>(),
                It.IsAny<string>(), It.IsAny<string>(), true), Times.Once);
        }

        // ─── ApproveAsync / RejectAsync ──────────────────────────────────────────

        [Fact]
        public async Task ApproveAsync_GecerliToken_StatusMutabikYapar()
        {
            var dbName = nameof(ApproveAsync_GecerliToken_StatusMutabikYapar);
            await SeedBaseDataAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Mutabakatlar.Add(CreateMutabakat("M1"));
                await ctx.SaveChangesAsync();
            }
            var service = CreateService(dbName);

            var result = await service.ApproveAsync("token-M1", "cevap@test.com", "Cevap Veren", "05550000000");

            Assert.True(result);
            await using var assertCtx = TestDbContextFactory.Create(dbName);
            var saved = await assertCtx.Mutabakatlar.FirstOrDefaultAsync(x => x.MutabakatId == "P1");
            Assert.Equal(MutabakatStatus.Mutabik, saved!.Status);
            Assert.Equal("cevap@test.com", saved.MutabakatCevapMail);
            Assert.Equal("Cevap Veren", saved.MutabakatCevapAdSoyad);
        }

        [Fact]
        public async Task ApproveAsync_ZatenCevaplanmisKayit_FalseDoner()
        {
            var dbName = nameof(ApproveAsync_ZatenCevaplanmisKayit_FalseDoner);
            await SeedBaseDataAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                var mutabakat = CreateMutabakat("P1");
                mutabakat.Status = MutabakatStatus.MutabikDegil;
                ctx.Mutabakatlar.Add(mutabakat);
                await ctx.SaveChangesAsync();
            }
            var service = CreateService(dbName);

            var result = await service.ApproveAsync("token-P1", "cevap@test.com", "Cevap Veren", "05550000000");

            Assert.False(result);
        }

        [Fact]
        public async Task RejectAsync_FilePathYoksa_FalseDoner()
        {
            var dbName = nameof(RejectAsync_FilePathYoksa_FalseDoner);
            await SeedBaseDataAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Mutabakatlar.Add(CreateMutabakat("P1"));
                await ctx.SaveChangesAsync();
            }
            var service = CreateService(dbName);

            var result = await service.RejectAsync("token-P1", "cevap@test.com", "Cevap Veren", "05550000000", "Açıklama", null);

            Assert.False(result);
        }

        [Fact]
        public async Task RejectAsync_GecerliToken_StatusMutabikDegilYaparVeCariBilgileriniGunceller()
        {
            var dbName = nameof(RejectAsync_GecerliToken_StatusMutabikDegilYaparVeCariBilgileriniGunceller);
            await SeedBaseDataAsync(dbName);
            await using (var ctx = TestDbContextFactory.Create(dbName))
            {
                ctx.Mutabakatlar.Add(CreateMutabakat("M1"));
                await ctx.SaveChangesAsync();
            }
            var service = CreateService(dbName);

            var result = await service.RejectAsync(
                "token-M1",
                "red@test.com",
                "Red Veren",
                "05559998877",
                " Eksik belge ",
                "responses/red.pdf");

            Assert.True(result);
            await using var assertCtx = TestDbContextFactory.Create(dbName);
            var saved = await assertCtx.Mutabakatlar.FirstOrDefaultAsync(x => x.MutabakatId == "P1");
            var cari = await assertCtx.Cariler.FindAsync("P1", 1);

            Assert.Equal(MutabakatStatus.MutabikDegil, saved!.Status);
            Assert.Equal("Eksik belge", saved.MutabakatCevapAciklama);
            Assert.Equal("responses/red.pdf", saved.MutabakatReceiveStoragePath);
            Assert.Equal("red@test.com", cari!.CariYetkiliMail);
            Assert.Equal("Red Veren", cari.CariYetkiliAdiSoyadi);
            Assert.Equal("05559998877", cari.CariYetkiliGsm);
        }
    }
}
