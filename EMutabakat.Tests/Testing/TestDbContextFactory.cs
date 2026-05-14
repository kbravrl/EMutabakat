using EMutabakat.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EMutabakat.Tests.Testing
{
    /// <summary>
    /// Her test için izole bir SQLite InMemory veritabanı oluşturan yardımcı sınıf.
    /// EF InMemory'nin aksine SQLite:
    ///   - FK constraint'leri uygular
    ///   - ExecuteUpdateAsync / ExecuteDeleteAsync destekler
    ///   - Gerçek SQL davranışı gösterir (unique index, cascade vb.)
    /// </summary>
    public static class TestDbContextFactory
    {
        /// <summary>
        /// Benzersiz bir SQLite InMemory bağlantısı ile AppDbContext oluşturur.
        /// Her çağrıda aynı dbName → aynı bağlantı nesnesi → aynı veri.
        /// </summary>
        public static AppDbContext Create(string? dbName = null)
        {
            var connection = SqliteConnectionCache.GetOrCreate(dbName ?? Guid.NewGuid().ToString());
            var options = BuildOptions(connection);
            var ctx = new AppDbContext(options);
            ctx.Database.EnsureCreated();
            return ctx;
        }

        /// <summary>
        /// IDbContextFactory&lt;AppDbContext&gt; mock'u oluşturur.
        /// Her CreateDbContextAsync çağrısında aynı SQLite bağlantısını kullanan
        /// yeni bir context döner; böylece testler arası veri izolasyonu korunur.
        /// </summary>
        public static Mock<IDbContextFactory<AppDbContext>> CreateMockFactory(string? dbName = null)
        {
            var resolvedDbName = dbName ?? Guid.NewGuid().ToString();
            var connection = SqliteConnectionCache.GetOrCreate(resolvedDbName);

            var mockFactory = new Mock<IDbContextFactory<AppDbContext>>();
            mockFactory
                .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    var ctx = new AppDbContext(BuildOptions(connection));
                    ctx.Database.EnsureCreated();
                    return ctx;
                });

            return mockFactory;
        }

        // ── Yardımcılar ──────────────────────────────────────────────────────────

        private static DbContextOptions<AppDbContext> BuildOptions(SqliteConnection connection)
        {
            return new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
        }
    }

    /// <summary>
    /// Test adına göre SQLite bağlantılarını önbellekte tutar.
    /// Aynı dbName ile yapılan tüm Create/CreateMockFactory çağrıları
    /// aynı açık bağlantıyı paylaşır; böylece seed → service → assert
    /// akışında veri kaybolmaz.
    /// </summary>
    internal static class SqliteConnectionCache
    {
        private static readonly Dictionary<string, SqliteConnection> _connections = new();

        public static SqliteConnection GetOrCreate(string name)
        {
            if (_connections.TryGetValue(name, out var existing))
                return existing;

            // "Data Source=:memory:" + her bağlantı kendi DB'sini tutar;
            // bağlantı açık kaldığı sürece veriler yaşar.
            var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();
            _connections[name] = conn;
            return conn;
        }
    }
}
