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
                new Claim(ClaimTypes.Email, kullanici.KullaniciMail),
                new Claim(ClaimTypes.Role, string.IsNullOrWhiteSpace(kullanici.Rol) ? KullaniciRolleri.Standart : kullanici.Rol)
            };

            var firmaIds = (kullanici.Firmalar?.Select(x => x.FirmaId).ToList() ?? new List<int>());
            if (!firmaIds.Contains(kullanici.FirmaId))
            {
                firmaIds.Add(kullanici.FirmaId);
            }

            foreach (var firmaId in firmaIds.Distinct())
            {
                claims.Add(new Claim("firma_id", firmaId.ToString()));
            }

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
