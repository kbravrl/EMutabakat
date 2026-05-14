using EMutabakat.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Runtime.CompilerServices;

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
        /// callerFilePath otomatik olarak çağıran dosya yolunu alır; böylece
        /// farklı test sınıflarındaki aynı isimli testler izole kalır.
        /// </summary>
        public static AppDbContext Create(string? dbName = null, [CallerFilePath] string callerFilePath = "")
        {
            var key = BuildKey(dbName, callerFilePath);
            bool isNew = !SqliteConnectionCache.Exists(key);
            var connection = SqliteConnectionCache.GetOrCreate(key);
            var options = BuildOptions(connection);
            var ctx = new AppDbContext(options);
            if (isNew)
                ctx.Database.EnsureCreated();
            return ctx;
        }

        /// <summary>
        /// IDbContextFactory&lt;AppDbContext&gt; mock'u oluşturur.
        /// Her CreateDbContextAsync çağrısında aynı SQLite bağlantısını kullanan
        /// yeni bir context döner; böylece testler arası veri izolasyonu korunur.
        /// callerFilePath otomatik olarak çağıran dosya yolunu alır.
        /// </summary>
        public static Mock<IDbContextFactory<AppDbContext>> CreateMockFactory(string? dbName = null, [CallerFilePath] string callerFilePath = "")
        {
            var key = BuildKey(dbName, callerFilePath);
            bool isNew = !SqliteConnectionCache.Exists(key);
            var connection = SqliteConnectionCache.GetOrCreate(key);

            // Tabloları yalnızca bir kez oluştur
            if (isNew)
            {
                using var initCtx = new AppDbContext(BuildOptions(connection));
                initCtx.Database.EnsureCreated();
            }

            var mockFactory = new Mock<IDbContextFactory<AppDbContext>>();
            mockFactory
                .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new AppDbContext(BuildOptions(connection)));

            return mockFactory;
        }

        // ── Yardımcılar ──────────────────────────────────────────────────────────

        private static string BuildKey(string? dbName, string callerFilePath)
        {
            // Dosya adından sınıf adını çıkar (uzantısız)
            var fileName = Path.GetFileNameWithoutExtension(callerFilePath);
            var name = dbName ?? Guid.NewGuid().ToString();
            // Farklı test sınıflarındaki aynı isimli testleri izole et
            return string.IsNullOrEmpty(fileName) ? name : $"{fileName}::{name}";
        }

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

        public static bool Exists(string name) => _connections.ContainsKey(name);

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
