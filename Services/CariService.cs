using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Text.RegularExpressions;

namespace EMutabakat.Services
{
    public class CariService : ICariService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public CariService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<Cari>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Cariler
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.CariGrup)
                .Include(x => x.DovizKodu)
                .OrderBy(x => x.CariAdi)
                .ToListAsync();
        }

        public async Task<string> GenerateNextCariIdAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var ids = await context.Cariler
                .AsNoTracking()
                .Select(x => x.CariId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToListAsync();

            var maxNumeric = 0;
            foreach (var id in ids)
            {
                var match = Regex.Match(id!, @"\d+");
                if (match.Success && int.TryParse(match.Value, out var number) && number > maxNumeric)
                {
                    maxNumeric = number;
                }
            }

            return $"P{maxNumeric + 1}";
        }

        public async Task<Cari?> GetByIdAsync(string cariId, int firmaId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedCariId = cariId?.Trim() ?? string.Empty;

            return await context.Cariler
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.CariGrup)
                .Include(x => x.DovizKodu)
                .FirstOrDefaultAsync(x => x.CariId == normalizedCariId && x.FirmaId == firmaId);
        }

        public async Task<Cari> AddAsync(Cari cari)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await ValidateCariAsync(context, cari);

            var exists = await context.Cariler.AnyAsync(x => x.CariId == cari.CariId && x.FirmaId == cari.FirmaId);
            if (exists)
                throw new Exception("Aynı Cari ID ve Firma ile kayıt zaten mevcut.");

            context.Cariler.Add(cari);
            await context.SaveChangesAsync();
            return cari;
        }

        public async Task<Cari?> UpdateAsync(Cari cari)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            cari.CariId = cari.CariId?.Trim() ?? string.Empty;
            cari.CariDovizKodu = string.IsNullOrWhiteSpace(cari.CariDovizKodu) ? null : cari.CariDovizKodu.Trim().ToUpperInvariant();
            cari.CariGrupId = cari.CariGrupId?.Trim() ?? string.Empty;
            cari.OriginalCariId = string.IsNullOrWhiteSpace(cari.OriginalCariId) ? cari.CariId : cari.OriginalCariId.Trim();
            cari.OriginalFirmaId = cari.OriginalFirmaId <= 0 ? cari.FirmaId : cari.OriginalFirmaId;

            var existingCari = await context.Cariler
                .FirstOrDefaultAsync(x => x.CariId == cari.OriginalCariId && x.FirmaId == cari.OriginalFirmaId);

            if (existingCari == null)
                return null;

            await ValidateCariAsync(context, cari);

            var keyChanged = !string.Equals(cari.OriginalCariId, cari.CariId, StringComparison.Ordinal)
                || cari.OriginalFirmaId != cari.FirmaId;

            if (keyChanged)
            {
                var keyExists = await context.Cariler.AnyAsync(x => x.CariId == cari.CariId && x.FirmaId == cari.FirmaId);
                if (keyExists)
                    throw new Exception("Aynı Cari ID ve Firma ile kayıt zaten mevcut.");

                var newCari = new Cari
                {
                    CariId = cari.CariId,
                    FirmaId = cari.FirmaId,
                    CariAdi = cari.CariAdi,
                    CariUnvan = cari.CariUnvan,
                    CariAdres = cari.CariAdres,
                    CariIlce = cari.CariIlce,
                    CariIl = cari.CariIl,
                    CariVergiDairesi = cari.CariVergiDairesi,
                    CariVergiNumarasi = cari.CariVergiNumarasi,
                    CariWebAdresi = cari.CariWebAdresi,
                    CariYetkiliAdiSoyadi = cari.CariYetkiliAdiSoyadi,
                    CariYetkiliTelefon = cari.CariYetkiliTelefon,
                    CariYetkiliGsm = cari.CariYetkiliGsm,
                    CariYetkiliMail = cari.CariYetkiliMail,
                    CariGrupId = cari.CariGrupId,
                    CariDovizKodu = cari.CariDovizKodu,
                    CariAktifPasif = cari.CariAktifPasif
                };

                context.Cariler.Add(newCari);
                await context.SaveChangesAsync();

                await context.Mutabakatlar
                    .Where(m => m.CariId == cari.OriginalCariId && m.FirmaId == cari.OriginalFirmaId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(m => m.CariId, cari.CariId)
                        .SetProperty(m => m.FirmaId, cari.FirmaId));

                context.Cariler.Remove(existingCari);
                await context.SaveChangesAsync();

                return await context.Cariler
                    .AsNoTracking()
                    .Include(x => x.Firma)
                    .Include(x => x.CariGrup)
                    .Include(x => x.DovizKodu)
                    .FirstOrDefaultAsync(x => x.CariId == cari.CariId && x.FirmaId == cari.FirmaId);
            }

            existingCari.CariAdi = cari.CariAdi;
            existingCari.CariUnvan = cari.CariUnvan;
            existingCari.CariAdres = cari.CariAdres;
            existingCari.CariIlce = cari.CariIlce;
            existingCari.CariIl = cari.CariIl;
            existingCari.CariVergiDairesi = cari.CariVergiDairesi;
            existingCari.CariVergiNumarasi = cari.CariVergiNumarasi;
            existingCari.CariWebAdresi = cari.CariWebAdresi;
            existingCari.CariYetkiliAdiSoyadi = cari.CariYetkiliAdiSoyadi;
            existingCari.CariYetkiliTelefon = cari.CariYetkiliTelefon;
            existingCari.CariYetkiliGsm = cari.CariYetkiliGsm;
            existingCari.CariYetkiliMail = cari.CariYetkiliMail;
            existingCari.CariGrupId = cari.CariGrupId;
            existingCari.CariDovizKodu = cari.CariDovizKodu;
            existingCari.CariAktifPasif = cari.CariAktifPasif;

            await context.SaveChangesAsync();
            return existingCari;
        }

        public async Task<bool> DeleteAsync(string cariId, int firmaId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedCariId = cariId?.Trim() ?? string.Empty;

            var cari = await context.Cariler
                .FirstOrDefaultAsync(x => x.CariId == normalizedCariId && x.FirmaId == firmaId);

            if (cari == null)
                return false;

            try
            {
                context.Cariler.Remove(cari);
                await context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23503")
                {
                    throw new Exception("Bu cari kaydı başka kayıtlarda kullanıldığı için silinemez.");
                }

                throw new Exception("Cari silinirken bir veritabanı hatası oluştu.");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("association between entity types") ||
                    ex.Message.Contains("relationship") ||
                    ex.Message.Contains("severed"))
                {
                    throw new Exception("Bu cari kaydı başka kayıtlarda kullanıldığı için silinemez.");
                }

                throw new Exception("Cari silinirken bir işlem hatası oluştu.");
            }
            catch
            {
                throw new Exception("Cari silinirken bir hata oluştu.");
            }
        }

        private async Task ValidateCariAsync(AppDbContext context, Cari cari)
        {
            cari.CariId = cari.CariId?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(cari.CariId))
                throw new Exception("Cari ID zorunludur.");

            if (cari.FirmaId <= 0)
                throw new Exception("Firma seçimi zorunludur.");

            if (string.IsNullOrWhiteSpace(cari.CariAdi))
                throw new Exception("Cari adı zorunludur.");

            if (string.IsNullOrWhiteSpace(cari.CariVergiDairesi))
                throw new Exception("Vergi dairesi zorunludur.");

            if (string.IsNullOrWhiteSpace(cari.CariVergiNumarasi))
                throw new Exception("Vergi numarası zorunludur.");

            if (string.IsNullOrWhiteSpace(cari.CariYetkiliMail))
                throw new Exception("Yetkili mail zorunludur.");

            if (string.IsNullOrWhiteSpace(cari.CariGrupId))
                throw new Exception("Cari grup seçimi zorunludur.");

            cari.CariGrupId = cari.CariGrupId.Trim();

            if (cari.CariAktifPasif != 0 && cari.CariAktifPasif != 1)
                throw new Exception("Aktif/Pasif değeri geçersiz.");

            var firmaExists = await context.Firmalar.AnyAsync(x => x.FirmaId == cari.FirmaId);
            if (!firmaExists)
                throw new Exception("Seçilen firma bulunamadı.");

            var cariGrup = await context.CariGruplar
                .FirstOrDefaultAsync(x => x.CariGrupId == cari.CariGrupId);

            if (cariGrup == null)
                throw new Exception("Seçilen cari grup bulunamadı.");

            if (cariGrup.FirmaId != cari.FirmaId)
                throw new Exception("Seçilen cari grup, seçilen firmaya ait değildir.");

            if (!string.IsNullOrWhiteSpace(cari.CariDovizKodu))
            {
                var dovizExists = await context.DovizKodlari.AnyAsync(x => x.TCMB == cari.CariDovizKodu);
                if (!dovizExists)
                    throw new Exception("Geçerli bir döviz kodu seçiniz.");
            }
        }

        private static string? GetStringCell(IRow row, int idx)
        {
            var cell = row.GetCell(idx);
            return cell?.ToString()?.Trim();
        }

        private static int ParseIntCell(IRow row, int idx)
        {
            var cell = row.GetCell(idx);
            if (cell == null) return 0;

            if (cell.CellType == CellType.Numeric)
                return Convert.ToInt32(cell.NumericCellValue);

            return int.TryParse(cell.ToString(), out var value) ? value : 0;
        }

        public async Task<(int created, int updated, List<string> errors)> ImportFromExcelAsync(Stream stream, string fileName, int firmaId)
        {
            var errors = new List<string>();
            var created = 0;
            var updated = 0;

            try
            {
                IWorkbook workbook;
                var ext = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;

                if (ext == ".xlsx")
                    workbook = new XSSFWorkbook(stream);
                else
                    workbook = new HSSFWorkbook(stream);

                var sheet = workbook.GetSheetAt(0);
                if (sheet == null)
                {
                    errors.Add("Excel sayfası bulunamadı.");
                    return (0, 0, errors);
                }

                var headerRow = sheet.GetRow(0);
                if (headerRow == null)
                {
                    errors.Add("Excel başlık satırı bulunamadı.");
                    return (0, 0, errors);
                }

                var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headerRow.LastCellNum; i++)
                {
                    var cell = headerRow.GetCell(i);
                    if (cell == null) continue;

                    var text = cell.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                        headerMap[text] = i;
                }

                string[] requiredColumns =
                {
                    "CariId",
                    "CariAdi",
                    "CariVergiDairesi",
                    "CariVergiNumarasi",
                    "CariYetkiliMail",
                    "CariGrupId",
                    "DovizKodu"
                };

                foreach (var column in requiredColumns)
                {
                    if (!headerMap.ContainsKey(column))
                    {
                        errors.Add($"Gerekli sütun '{column}' bulunamadı.");
                    }
                }

                if (errors.Count > 0)
                    return (0, 0, errors);

                await using var context = await _contextFactory.CreateDbContextAsync();

                if (firmaId <= 0)
                {
                    errors.Add("Firma seçimi zorunludur.");
                    return (0, 0, errors);
                }

                var firmaExists = await context.Firmalar.AnyAsync(x => x.FirmaId == firmaId);
                if (!firmaExists)
                {
                    errors.Add("Seçilen firma bulunamadı.");
                    return (0, 0, errors);
                }

                for (int r = 1; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;

                    var cari = new Cari
                    {
                        CariId = GetStringCell(row, headerMap["CariId"]) ?? string.Empty,
                        FirmaId = firmaId,
                        CariAdi = GetStringCell(row, headerMap["CariAdi"]) ?? string.Empty,
                        CariUnvan = headerMap.ContainsKey("CariUnvan") ? GetStringCell(row, headerMap["CariUnvan"]) : null,
                        CariAdres = headerMap.ContainsKey("CariAdres") ? GetStringCell(row, headerMap["CariAdres"]) : null,
                        CariIlce = headerMap.ContainsKey("CariIlce") ? GetStringCell(row, headerMap["CariIlce"]) : null,
                        CariIl = headerMap.ContainsKey("CariIl") ? GetStringCell(row, headerMap["CariIl"]) : null,
                        CariVergiDairesi = GetStringCell(row, headerMap["CariVergiDairesi"]) ?? string.Empty,
                        CariVergiNumarasi = GetStringCell(row, headerMap["CariVergiNumarasi"]) ?? string.Empty,
                        CariWebAdresi = headerMap.ContainsKey("CariWebAdresi") ? GetStringCell(row, headerMap["CariWebAdresi"]) : null,
                        CariYetkiliAdiSoyadi = headerMap.ContainsKey("CariYetkiliAdiSoyadi") ? GetStringCell(row, headerMap["CariYetkiliAdiSoyadi"]) : null,
                        CariYetkiliTelefon = headerMap.ContainsKey("CariYetkiliTelefon") ? GetStringCell(row, headerMap["CariYetkiliTelefon"]) : null,
                        CariYetkiliGsm = headerMap.ContainsKey("CariYetkiliGsm") ? GetStringCell(row, headerMap["CariYetkiliGsm"]) : null,
                        CariYetkiliMail = GetStringCell(row, headerMap["CariYetkiliMail"]) ?? string.Empty,
                        CariGrupId = GetStringCell(row, headerMap["CariGrupId"]) ?? string.Empty,
                        CariDovizKodu = GetStringCell(row, headerMap["DovizKodu"]),
                        CariAktifPasif = headerMap.ContainsKey("CariAktifPasif")
                            ? ParseIntCell(row, headerMap["CariAktifPasif"])
                            : 1
                    };

                    cari.CariId = cari.CariId.Trim();
                    cari.CariGrupId = cari.CariGrupId.Trim();

                    try
                    {
                        await ValidateCariAsync(context, cari);

                        var existing = await context.Cariler
                            .FirstOrDefaultAsync(x => x.CariId == cari.CariId && x.FirmaId == cari.FirmaId);

                        if (existing == null)
                        {
                            context.Cariler.Add(cari);
                            created++;
                        }
                        else
                        {
                            var hasChange = !string.Equals(existing.CariAdi, cari.CariAdi, StringComparison.Ordinal)
                                || !string.Equals(existing.CariUnvan, cari.CariUnvan, StringComparison.Ordinal)
                                || !string.Equals(existing.CariAdres, cari.CariAdres, StringComparison.Ordinal)
                                || !string.Equals(existing.CariIlce, cari.CariIlce, StringComparison.Ordinal)
                                || !string.Equals(existing.CariIl, cari.CariIl, StringComparison.Ordinal)
                                || !string.Equals(existing.CariVergiDairesi, cari.CariVergiDairesi, StringComparison.Ordinal)
                                || !string.Equals(existing.CariVergiNumarasi, cari.CariVergiNumarasi, StringComparison.Ordinal)
                                || !string.Equals(existing.CariWebAdresi, cari.CariWebAdresi, StringComparison.Ordinal)
                                || !string.Equals(existing.CariYetkiliAdiSoyadi, cari.CariYetkiliAdiSoyadi, StringComparison.Ordinal)
                                || !string.Equals(existing.CariYetkiliTelefon, cari.CariYetkiliTelefon, StringComparison.Ordinal)
                                || !string.Equals(existing.CariYetkiliGsm, cari.CariYetkiliGsm, StringComparison.Ordinal)
                                || !string.Equals(existing.CariYetkiliMail, cari.CariYetkiliMail, StringComparison.Ordinal)
                                || !string.Equals(existing.CariGrupId, cari.CariGrupId, StringComparison.Ordinal)
                                || existing.CariDovizKodu != cari.CariDovizKodu
                                || existing.CariAktifPasif != cari.CariAktifPasif;

                            if (hasChange)
                            {
                                existing.CariAdi = cari.CariAdi;
                                existing.CariUnvan = cari.CariUnvan;
                                existing.CariAdres = cari.CariAdres;
                                existing.CariIlce = cari.CariIlce;
                                existing.CariIl = cari.CariIl;
                                existing.CariVergiDairesi = cari.CariVergiDairesi;
                                existing.CariVergiNumarasi = cari.CariVergiNumarasi;
                                existing.CariWebAdresi = cari.CariWebAdresi;
                                existing.CariYetkiliAdiSoyadi = cari.CariYetkiliAdiSoyadi;
                                existing.CariYetkiliTelefon = cari.CariYetkiliTelefon;
                                existing.CariYetkiliGsm = cari.CariYetkiliGsm;
                                existing.CariYetkiliMail = cari.CariYetkiliMail;
                                existing.CariGrupId = cari.CariGrupId;
                                existing.CariDovizKodu = cari.CariDovizKodu;
                                existing.CariAktifPasif = cari.CariAktifPasif;
                                updated++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Satır {r + 1}: {ex.Message}");
                    }
                }

                if (errors.Count > 0)
                    return (0, 0, errors);

                await context.SaveChangesAsync();

                return (created, updated, errors);
            }
            catch (Exception ex)
            {
                errors.Add($"İşlem sırasında hata oluştu: {ex.Message}");
                return (0, 0, errors);
            }
        }
    }
}