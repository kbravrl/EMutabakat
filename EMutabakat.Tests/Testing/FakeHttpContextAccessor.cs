using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace EMutabakat.Tests.Testing
{
    /// <summary>
    /// Testlerde kullanıcı kimliğini simüle etmek için yardımcı sınıf.
    /// </summary>
    public static class FakeHttpContextAccessor
    {
        /// <summary>
        /// Belirtilen e-posta ile kimliği doğrulanmış bir kullanıcı döndüren mock oluşturur.
        /// </summary>
        public static Mock<IHttpContextAccessor> CreateAuthenticated(string email)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            var mock = new Mock<IHttpContextAccessor>();
            mock.Setup(x => x.HttpContext).Returns(httpContext);

            return mock;
        }

        /// <summary>
        /// Kimliği doğrulanmamış (anonim) bir kullanıcı döndüren mock oluşturur.
        /// </summary>
        public static Mock<IHttpContextAccessor> CreateAnonymous()
        {
            var mock = new Mock<IHttpContextAccessor>();
            mock.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
            return mock;
        }

        /// <summary>
        /// HttpContext'i null döndüren mock oluşturur.
        /// </summary>
        public static Mock<IHttpContextAccessor> CreateNull()
        {
            var mock = new Mock<IHttpContextAccessor>();
            mock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            return mock;
        }
    }
}
