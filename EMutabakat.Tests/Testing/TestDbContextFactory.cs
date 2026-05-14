using EMutabakat.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EMutabakat.Tests.Testing
{
    /// <summary>
    /// Her test için izole bir InMemory veritabanı oluşturan yardımcı sınıf.
    /// </summary>
    public static class TestDbContextFactory
    {
        /// <summary>
        /// Benzersiz bir InMemory veritabanı ile AppDbContext oluşturur.
        /// </summary>
        public static AppDbContext Create(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        /// <summary>
        /// IDbContextFactory<AppDbContext> mock'u oluşturur.
        /// Her CreateDbContextAsync çağrısında aynı options ile yeni bir context döner.
        /// </summary>
        public static Mock<IDbContextFactory<AppDbContext>> CreateMockFactory(string? dbName = null)
        {
            var resolvedDbName = dbName ?? Guid.NewGuid().ToString();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(resolvedDbName)
                .Options;

            var mockFactory = new Mock<IDbContextFactory<AppDbContext>>();
            mockFactory
                .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new AppDbContext(options));

            return mockFactory;
        }
    }
}
