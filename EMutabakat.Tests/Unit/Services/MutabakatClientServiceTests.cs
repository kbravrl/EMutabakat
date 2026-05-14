using EMutabakat.Models;
using EMutabakat.Services;
using EMutabakat.Services.Interfaces;
using Moq;
using Xunit;

namespace EMutabakat.Tests.Unit.Services
{
    public class MutabakatClientServiceTests
    {
        private readonly Mock<IMutabakatService> _mockMutabakatService;
        private readonly Mock<ISdService> _mockSdService;
        private readonly MutabakatClientService _service;

        public MutabakatClientServiceTests()
        {
            _mockMutabakatService = new Mock<IMutabakatService>();
            _mockSdService = new Mock<ISdService>();
            _service = new MutabakatClientService(_mockMutabakatService.Object, _mockSdService.Object);
        }

        [Fact]
        public async Task ApproveAsync_EksikBilgileriCariUzerindenTamamlar()
        {
            var mutabakat = new Mutabakat
            {
                MutabakatId = "M1",
                MutabakatToken = "token",
                Cari = new Cari
                {
                    CariYetkiliMail = "cari@test.com",
                    CariYetkiliAdiSoyadi = "Cari Yetkili",
                    CariYetkiliGsm = "05551234567"
                }
            };

            _mockMutabakatService
                .Setup(x => x.GetByTokenAsync("token"))
                .ReturnsAsync(mutabakat);

            _mockMutabakatService
                .Setup(x => x.ApproveAsync("token", "cari@test.com", "Cari Yetkili", "05551234567"))
                .ReturnsAsync(true);

            var result = await _service.ApproveAsync("token");

            Assert.True(result);
        }

        [Fact]
        public async Task RejectAsync_MutabakatYoksa_FalseDoner()
        {
            _mockMutabakatService
                .Setup(x => x.GetByTokenAsync("yok"))
                .ReturnsAsync((Mutabakat?)null);

            var result = await _service.RejectAsync("yok");

            Assert.False(result);
        }

        [Fact]
        public async Task RejectAsync_DosyaYoksa_FalseDoner()
        {
            _mockMutabakatService
                .Setup(x => x.GetByTokenAsync("token"))
                .ReturnsAsync(new Mutabakat { MutabakatToken = "token", Cari = new Cari() });

            var result = await _service.RejectAsync("token", originalFileName: "red.pdf");

            Assert.False(result);
        }

        [Fact]
        public async Task RejectAsync_GecerliDosyaKaydedilirVeServisePathIleIletilir()
        {
            var mutabakat = new Mutabakat
            {
                MutabakatId = "M1",
                MutabakatToken = "token",
                MutabakatTarihi = new DateTime(2026, 5, 1),
                CariId = "C1",
                Cari = new Cari
                {
                    CariId = "C1",
                    CariVergiNumarasi = "1234567890",
                    CariYetkiliMail = "cari@test.com",
                    CariYetkiliAdiSoyadi = "Cari Yetkili",
                    CariYetkiliGsm = "05551234567"
                }
            };

            _mockMutabakatService
                .Setup(x => x.GetByTokenAsync("token"))
                .ReturnsAsync(mutabakat);

            _mockSdService
                .Setup(x => x.SaveMutabakatResponseFileAsync(
                    "token",
                    mutabakat.MutabakatTarihi,
                    "C1",
                    "1234567890",
                    It.IsAny<Stream>(),
                    "red.pdf",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("responses/red.pdf");

            _mockMutabakatService
                .Setup(x => x.RejectAsync(
                    "token",
                    "cari@test.com",
                    "Cari Yetkili",
                    "05551234567",
                    "Açıklama",
                    "responses/red.pdf"))
                .ReturnsAsync(true);

            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

            var result = await _service.RejectAsync(
                "token",
                aciklama: " Açıklama ",
                fileStream: stream,
                originalFileName: "red.pdf");

            Assert.True(result);
        }

        [Fact]
        public async Task RejectAsync_AnaServisBasarisizsa_KaydedilenDosyayiSiler()
        {
            var mutabakat = new Mutabakat
            {
                MutabakatId = "M1",
                MutabakatToken = "token",
                MutabakatTarihi = new DateTime(2026, 5, 1),
                CariId = "C1",
                Cari = new Cari
                {
                    CariId = "C1",
                    CariVergiNumarasi = "1234567890",
                    CariYetkiliMail = "cari@test.com",
                    CariYetkiliAdiSoyadi = "Cari Yetkili",
                    CariYetkiliGsm = "05551234567"
                }
            };

            _mockMutabakatService
                .Setup(x => x.GetByTokenAsync("token"))
                .ReturnsAsync(mutabakat);

            _mockSdService
                .Setup(x => x.SaveMutabakatResponseFileAsync(
                    It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("responses/red.pdf");

            _mockMutabakatService
                .Setup(x => x.RejectAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync(false);

            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

            var result = await _service.RejectAsync("token", fileStream: stream, originalFileName: "red.pdf");

            Assert.False(result);
            _mockSdService.Verify(x => x.DeleteMutabakatResponseFileAsync("responses/red.pdf", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
