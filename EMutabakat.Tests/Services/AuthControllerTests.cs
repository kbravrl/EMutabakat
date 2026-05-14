using EMutabakat.Controllers;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace EMutabakat.Tests.Services
{
    public class AuthControllerTests
    {
        private readonly Mock<IKullaniciService> _mockKullaniciService;

        public AuthControllerTests()
        {
            _mockKullaniciService = new Mock<IKullaniciService>();
        }

        private AuthController CreateController(bool withAuthService = false)
        {
            var controller = new AuthController(_mockKullaniciService.Object);

            // HttpContext mock'u — cookie sign-in için IAuthenticationService gerekli
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(x => x.SignInAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string?>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties?>()))
                .Returns(Task.CompletedTask);

            authServiceMock
                .Setup(x => x.SignOutAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string?>(),
                    It.IsAny<AuthenticationProperties?>()))
                .Returns(Task.CompletedTask);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProviderMock.Object
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        // ─── Login ───────────────────────────────────────────────────────────────

        [Fact]
        public async Task Login_GecerliKimlik_OkDoner()
        {
            var kullanici = new Kullanici
            {
                KullaniciId = "P1",
                KullaniciAdi = "Test",
                KullaniciSoyadi = "User",
                KullaniciMail = "user@test.com",
                KullaniciAktifPasif = "1",
                Firmalar = new List<Firma>()
            };

            _mockKullaniciService
                .Setup(x => x.LoginAsync("user@test.com", "sifre123"))
                .ReturnsAsync(kullanici);

            var controller = CreateController();
            var model = new LoginModel { Mail = "user@test.com", Sifre = "sifre123" };

            var result = await controller.Login(model);

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Login_YanlisKimlik_UnauthorizedDoner()
        {
            _mockKullaniciService
                .Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((Kullanici?)null);

            var controller = CreateController();
            var model = new LoginModel { Mail = "user@test.com", Sifre = "yanlisSifre" };

            var result = await controller.Login(model);

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Login_GecersizModel_BadRequestDoner()
        {
            var controller = CreateController();
            controller.ModelState.AddModelError("Mail", "Mail zorunludur.");

            var model = new LoginModel { Mail = "", Sifre = "sifre" };

            var result = await controller.Login(model);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_KullaniciFirmalariClaimeEklenir()
        {
            var kullanici = new Kullanici
            {
                KullaniciId = "P1",
                KullaniciAdi = "Test",
                KullaniciSoyadi = "User",
                KullaniciMail = "user@test.com",
                KullaniciAktifPasif = "1",
                Firmalar = new List<Firma>
                {
                    new Firma { FirmaId = 1, FirmaAdi = "Firma A", FirmaVergiDairesi = "VD", FirmaVergiNumarasi = "1234567890",
                        FirmaYetkiliAdiSoyadi = "Y", FirmaMail = "f@t.com", FirmaTelefon = "02121234567",
                        FirmaSmtpHost = "s", FirmaSmtpPort = 587, FirmaSmtpUser = "u@t.com", FirmaSmtpPassword = "p", FirmaSmtpSecure = "true" },
                    new Firma { FirmaId = 2, FirmaAdi = "Firma B", FirmaVergiDairesi = "VD", FirmaVergiNumarasi = "0987654321",
                        FirmaYetkiliAdiSoyadi = "Y", FirmaMail = "g@t.com", FirmaTelefon = "02121234568",
                        FirmaSmtpHost = "s", FirmaSmtpPort = 587, FirmaSmtpUser = "v@t.com", FirmaSmtpPassword = "p", FirmaSmtpSecure = "true" }
                }
            };

            _mockKullaniciService
                .Setup(x => x.LoginAsync("user@test.com", "sifre123"))
                .ReturnsAsync(kullanici);

            ClaimsPrincipal? capturedPrincipal = null;
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(x => x.SignInAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string?>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties?>()))
                .Callback<HttpContext, string?, ClaimsPrincipal, AuthenticationProperties?>(
                    (_, _, principal, _) => capturedPrincipal = principal)
                .Returns(Task.CompletedTask);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            var controller = new AuthController(_mockKullaniciService.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = serviceProviderMock.Object }
            };

            var model = new LoginModel { Mail = "user@test.com", Sifre = "sifre123" };
            await controller.Login(model);

            Assert.NotNull(capturedPrincipal);
            var firmaClaims = capturedPrincipal!.Claims
                .Where(c => c.Type == "firma_id")
                .Select(c => c.Value)
                .ToList();

            Assert.Contains("1", firmaClaims);
            Assert.Contains("2", firmaClaims);
        }

        // ─── Logout ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task Logout_HerZaman_OkDoner()
        {
            var controller = CreateController();

            var result = await controller.Logout();

            Assert.IsType<OkResult>(result);
        }
    }
}
