using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using EMutabakat.Services.Interfaces;
using EMutabakat.Models;

namespace EMutabakat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IKullaniciService _kullaniciService;

        public AuthController(IKullaniciService kullaniciService)
        {
            _kullaniciService = kullaniciService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var kullanici = await _kullaniciService.LoginAsync(model.Mail, model.Sifre);
            if (kullanici == null) return Unauthorized();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, kullanici.KullaniciId.ToString()),
                new Claim(ClaimTypes.Name, kullanici.KullaniciMail),
                new Claim(ClaimTypes.Email, kullanici.KullaniciMail)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return Ok();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var yeniKullanici = new Kullanici
            {
                FirmaId = model.FirmaId,
                KullaniciAdi = model.KullaniciAdi,
                KullaniciSoyadi = model.KullaniciSoyadi,
                KullaniciMail = model.KullaniciMail,
                KullaniciGsm = model.KullaniciGsm,
                Sifre = model.Sifre,
                KullaniciAktifPasif = "1"
            };

            var result = await _kullaniciService.RegisterAsync(yeniKullanici);
            if (result == null) return Conflict("Email already exists");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.KullaniciId.ToString()),
                new Claim(ClaimTypes.Name, result.KullaniciMail),
                new Claim(ClaimTypes.Email, result.KullaniciMail)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return Ok();
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }
    }
}
