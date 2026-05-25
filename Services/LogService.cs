using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EMutabakat.Services
{
    public class LogService : ILogService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LogService(
            IDbContextFactory<AppDbContext> contextFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _contextFactory = contextFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<AppLog>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var allowedFirmaIds = await GetAllowedFirmaIdsAsync(context);

            if (allowedFirmaIds.Count == 0)
                return new List<AppLog>();

            var allowedUserEmails = await context.Kullanicilar
                .AsNoTracking()
                .Include(k => k.Firmalar)
                .Where(k => k.Firmalar.Any(f => allowedFirmaIds.Contains(f.FirmaId)))
                .Select(k => k.KullaniciMail)
                .ToListAsync();

            return await context.AppLogs
                .AsNoTracking()
                .Where(x => x.UserEmail != null && allowedUserEmails.Contains(x.UserEmail))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<AppLog>> GetRecentAsync(int count = 200)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var allowedFirmaIds = await GetAllowedFirmaIdsAsync(context);

            if (allowedFirmaIds.Count == 0)
                return new List<AppLog>();

            var allowedUserEmails = await context.Kullanicilar
                .AsNoTracking()
                .Include(k => k.Firmalar)
                .Where(k => k.Firmalar.Any(f => allowedFirmaIds.Contains(f.FirmaId)))
                .Select(k => k.KullaniciMail)
                .ToListAsync();

            return await context.AppLogs
                .AsNoTracking()
                .Where(x => x.UserEmail != null && allowedUserEmails.Contains(x.UserEmail))
                .OrderByDescending(x => x.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        private async Task<List<int>> GetAllowedFirmaIdsAsync(AppDbContext context)
        {
            var mail = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(mail))
                return new List<int>();

            var kullanici = await context.Kullanicilar
                .Include(k => k.Firmalar)
                .FirstOrDefaultAsync(k => k.KullaniciMail == mail);

            if (kullanici == null)
                return new List<int>();

            if (kullanici.IsSeedUser)
            {
                return await context.Firmalar
                    .Select(f => f.FirmaId)
                    .Distinct()
                    .Where(id => id > 0)
                    .ToListAsync();
            }

            return kullanici.Firmalar
                .Select(f => f.FirmaId)
                .Distinct()
                .Where(id => id > 0)
                .ToList();
        }

        public async Task AddAsync(string level, string source, string message, string? userEmail = null, string? details = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var log = new AppLog
            {
                CreatedAt = DateTime.UtcNow,
                Level = string.IsNullOrWhiteSpace(level) ? "Info" : level.Trim(),
                Source = string.IsNullOrWhiteSpace(source) ? "System" : source.Trim(),
                Message = string.IsNullOrWhiteSpace(message) ? "Log message is empty." : message.Trim(),
                UserEmail = string.IsNullOrWhiteSpace(userEmail) ? null : userEmail.Trim(),
                Details = details
            };

            context.AppLogs.Add(log);
            await context.SaveChangesAsync();
        }

        public async Task AddChangeAsync(string source, string entityId, object oldEntity, object newEntity, string? userEmail = null)
        {
            var changes = CompareObjects(oldEntity, newEntity);

            string? details;
            string message;

            if (changes.Count == 0)
            {
                message = $"{source} güncellendi, değişiklik yok | {entityId}";
                details = null;
            }
            else
            {
                message = $"{source} güncellendi ({changes.Count} alan değişti) | {entityId}";
                var payload = new LogDetailPayload { Type = "changes", Changes = changes };
                details = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
            }

            await AddAsync("Bilgi", source, message, userEmail, details);
        }

        public async Task AddImportResultAsync(string source, string message, List<string> errors, string? userEmail = null)
        {
            string? details = null;

            if (errors.Count > 0)
            {
                var payload = new LogDetailPayload { Type = "errors", Errors = errors };
                details = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
            }

            var level = errors.Count > 0 ? "Hata" : "Bilgi";
            await AddAsync(level, source, message, userEmail, details);
        }

        private static List<FieldChange> CompareObjects(object oldObj, object newObj, string prefix = "")
        {
            var changes = new List<FieldChange>();

            if (oldObj == null || newObj == null)
                return changes;

            var type = oldObj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead);

            foreach (var prop in properties)
            {
                var fieldName = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                var oldVal = prop.GetValue(oldObj);
                var newVal = prop.GetValue(newObj);

                if (IsPrimitiveOrString(prop.PropertyType))
                {
                    var oldStr = FormatValue(prop.PropertyType, oldVal, fieldName);
                    var newStr = FormatValue(prop.PropertyType, newVal, fieldName);

                    if (!string.Equals(oldStr, newStr, StringComparison.Ordinal))
                    {
                        changes.Add(new FieldChange
                        {
                            Field = FormatFieldName(fieldName),
                            OldValue = string.IsNullOrEmpty(oldStr) ? null : oldStr,
                            NewValue = string.IsNullOrEmpty(newStr) ? null : newStr
                        });
                    }
                }
                else if (IsGenericList(prop.PropertyType))
                {
                    // List<T> karşılaştırması: elemanları sıralı string olarak karşılaştır
                    var oldStr = SerializeList(oldVal);
                    var newStr = SerializeList(newVal);

                    if (!string.Equals(oldStr, newStr, StringComparison.Ordinal))
                    {
                        changes.Add(new FieldChange
                        {
                            Field = FormatFieldName(fieldName),
                            OldValue = string.IsNullOrEmpty(oldStr) ? null : oldStr,
                            NewValue = string.IsNullOrEmpty(newStr) ? null : newStr
                        });
                    }
                }
                else if (prop.PropertyType.IsClass && oldVal != null && newVal != null)
                {
                    var nestedChanges = CompareObjects(oldVal, newVal, fieldName);
                    changes.AddRange(nestedChanges);
                }
            }

            return changes;
        }

        private static bool IsGenericList(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>);
        }

        private static string SerializeList(object? listObj)
        {
            if (listObj == null)
                return string.Empty;

            var items = ((System.Collections.IEnumerable)listObj)
                .Cast<object>()
                .Select(x => x?.ToString() ?? string.Empty)
                .OrderBy(x => x)
                .ToList();

            return string.Join(", ", items);
        }

        /// <summary>Enum ise Description attribute'unu, bool ise Evet/Hayır ya da Yetkili/Yetkisiz, diğerleri ToString döner.</summary>
        private static string FormatValue(Type propType, object? val, string fieldName = "")
        {
            if (val == null) return string.Empty;

            var underlying = Nullable.GetUnderlyingType(propType) ?? propType;

            if (underlying.IsEnum)
            {
                var field = underlying.GetField(val.ToString()!);
                if (field != null)
                {
                    var desc = field.GetCustomAttribute<DescriptionAttribute>();
                    if (desc != null) return desc.Description;
                }
                return val.ToString() ?? string.Empty;
            }

            if (underlying == typeof(bool))
            {
                // Alan adı "Yetki" içeriyorsa Yetkili/Yetkisiz, diğerleri Evet/Hayır
                var isYetki = fieldName.Contains("Yetki", StringComparison.OrdinalIgnoreCase);
                return (bool)val
                    ? (isYetki ? "Yetkili" : "Evet")
                    : (isYetki ? "Yetkisiz" : "Hayır");
            }

            return val.ToString() ?? string.Empty;
        }

        /// <summary>
        /// "Yetkiler.Mutabakatlar" → "Yetkiler - Mutabakatlar"
        /// "KullaniciAdi" → "Kullanıcı Adı" (CamelCase'i boşluklara böler)
        /// </summary>
        private static string FormatFieldName(string fieldName)
        {
            // Noktalı prefix'i " - " ile ayır
            var parts = fieldName.Split('.');
            var formatted = parts.Select(p => SplitCamelCase(p));
            return string.Join(" - ", formatted);
        }

        private static string SplitCamelCase(string input)
        {
            // Büyük harften önce boşluk ekle: "TamYetki" → "Tam Yetki"
            return Regex.Replace(input, @"(?<=[a-zçğıöşü])(?=[A-ZÇĞİÖŞÜ])", " ");
        }

        private static bool IsPrimitiveOrString(Type t)
        {
            var underlying = Nullable.GetUnderlyingType(t) ?? t;
            return underlying.IsPrimitive
                || underlying.IsEnum
                || underlying == typeof(string)
                || underlying == typeof(decimal)
                || underlying == typeof(DateTime)
                || underlying == typeof(DateTimeOffset)
                || underlying == typeof(Guid);
        }

        public async Task DeleteAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var logs = await context.AppLogs.ToListAsync();

            if (logs.Count == 0)
                return;

            context.AppLogs.RemoveRange(logs);
            await context.SaveChangesAsync();
        }

        public async Task DeleteLastDaysAsync(int dayCount)
        {
            if (dayCount <= 0)
                return;

            await using var context = await _contextFactory.CreateDbContextAsync();

            var startDate = DateTime.UtcNow.Date.AddDays(-dayCount);

            var logs = await context.AppLogs
                .Where(x => x.CreatedAt >= startDate)
                .ToListAsync();

            if (logs.Count == 0)
                return;

            context.AppLogs.RemoveRange(logs);
            await context.SaveChangesAsync();
        }

        public Task<byte[]> ExportToExcelAsync(List<AppLog> logs)
        {
            var orderedLogs = logs
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .ToList();

            IWorkbook workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Loglar");
            var wrapStyle = workbook.CreateCellStyle();
            wrapStyle.WrapText = true;
            wrapStyle.VerticalAlignment = VerticalAlignment.Top;

            var idStyle = workbook.CreateCellStyle();
            idStyle.WrapText = true;
            idStyle.VerticalAlignment = VerticalAlignment.Top;
            idStyle.Alignment = HorizontalAlignment.Right;

            var headers = new[]
            {
                "ID",
                "Tarih",
                "Seviye",
                "Kaynak",
                "Kullanıcı",
                "Mesaj",
                "Detaylar"
            };

            var headerRow = sheet.CreateRow(0);
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
            }

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            for (int i = 0; i < orderedLogs.Count; i++)
            {
                var log = orderedLogs[i];
                var row = sheet.CreateRow(i + 1);

                var c0 = row.CreateCell(0); c0.SetCellValue(log.Id);                                                              c0.CellStyle = idStyle;
                var c1 = row.CreateCell(1); c1.SetCellValue(log.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss"));         c1.CellStyle = wrapStyle;
                var c2 = row.CreateCell(2); c2.SetCellValue(log.Level ?? "");                                                     c2.CellStyle = wrapStyle;
                var c3 = row.CreateCell(3); c3.SetCellValue(log.Source ?? "");                                                    c3.CellStyle = wrapStyle;
                var c4 = row.CreateCell(4); c4.SetCellValue(log.UserEmail ?? "");                                                 c4.CellStyle = wrapStyle;
                var c5 = row.CreateCell(5); c5.SetCellValue(log.Message ?? "");                                                   c5.CellStyle = wrapStyle;
                var c6 = row.CreateCell(6); c6.SetCellValue(FormatDetailsForExport(log.Details, jsonOptions));                    c6.CellStyle = wrapStyle;
            }

            sheet.SetColumnWidth(0, 2000);
            sheet.SetColumnWidth(1, 5500);
            sheet.SetColumnWidth(2, 3500);  
            sheet.SetColumnWidth(3, 4500);  
            sheet.SetColumnWidth(4, 9000);
            sheet.SetColumnWidth(5, 18000);
            sheet.SetColumnWidth(6, 22000);

            using var ms = new MemoryStream();
            workbook.Write(ms, true);
            return Task.FromResult(ms.ToArray());
        }

        private static string FormatDetailsForExport(string? json, JsonSerializerOptions options)
        {
            if (string.IsNullOrWhiteSpace(json))
                return string.Empty;

            var payload = JsonSerializer.Deserialize<LogDetailPayload>(json, options);

            if (payload?.Changes != null && payload.Changes.Count > 0)
            {
                var lines = payload.Changes.Select(c =>
                $"{c.Field}: {c.OldValue ?? "(boş)"} → {c.NewValue ?? "(boş)"}");
                return string.Join("\n", lines);
            }

            if (payload?.Errors != null && payload.Errors.Count > 0)
            {
                var lines = payload.Errors.Select((e, idx) => $"Hata {idx + 1}: {e}");
                return string.Join("\n", lines);
            }
          
            return json;
        }
    }

    public class FieldChange
    {
        public string Field { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }

    public class LogDetailPayload
    {
        /// <summary>"changes", "errors" veya "info"</summary>
        public string Type { get; set; } = string.Empty;
        public List<FieldChange>? Changes { get; set; }
        public List<string>? Errors { get; set; }
        public Dictionary<string, string>? Info { get; set; }
    }

}