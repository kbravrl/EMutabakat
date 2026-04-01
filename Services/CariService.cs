using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace EMutabakat.Services
{
    public class CariService : ICariService
    {
        private readonly AppDbContext _db;

        public CariService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Cari>> GetAllAsync()
        {
            return await _db.Cariler
                .Include(x => x.Firma)
                .Include(x => x.CariGrup)
                .OrderBy(x => x.CariAdi)
                .ToListAsync();
        }

        public async Task<Cari?> GetByIdAsync(int id)
        {
            return await _db.Cariler
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.CariGrup)
                .FirstOrDefaultAsync(x => x.CariId == id);
        }

        public async Task<Cari> AddAsync(Cari cari)
        {
            await ValidateCariAsync(cari);

            _db.Cariler.Add(cari);
            await _db.SaveChangesAsync();
            return cari;
        }

        public async Task<Cari?> UpdateAsync(Cari cari)
        {
            var existingCari = await _db.Cariler
                .FirstOrDefaultAsync(x => x.CariId == cari.CariId);

            if (existingCari == null)
                return null;

            await ValidateCariAsync(cari);

            existingCari.FirmaId = cari.FirmaId;
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

            await _db.SaveChangesAsync();
            return existingCari;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var cari = await _db.Cariler
                .FirstOrDefaultAsync(x => x.CariId == id);

            if (cari == null)
                return false;

            try
            {
                _db.Cariler.Remove(cari);
                await _db.SaveChangesAsync();
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

        private async Task ValidateCariAsync(Cari cari)
        {
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

            if (cari.CariGrupId <= 0)
                throw new Exception("Cari grup seçimi zorunludur.");

            if (cari.CariAktifPasif != 0 && cari.CariAktifPasif != 1)
                throw new Exception("Aktif/Pasif değeri geçersiz.");

            var firmaExists = await _db.Firmalar.AnyAsync(x => x.FirmaId == cari.FirmaId);
            if (!firmaExists)
                throw new Exception("Seçilen firma bulunamadı.");

            var cariGrup = await _db.CariGruplar
                .FirstOrDefaultAsync(x => x.CariGrupId == cari.CariGrupId);

            if (cariGrup == null)
                throw new Exception("Seçilen cari grup bulunamadı.");

            if (cariGrup.FirmaId != cari.FirmaId)
                throw new Exception("Seçilen cari grup, seçilen firmaya ait değildir.");
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

        public async Task<(int created, List<string> errors)> ImportFromExcelAsync(Stream stream, string fileName)
        {
            var errors = new List<string>();
            var created = 0;

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
                    return (0, errors);
                }

                var headerRow = sheet.GetRow(0);
                if (headerRow == null)
                {
                    errors.Add("Excel başlık satırı bulunamadı.");
                    return (0, errors);
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
                    "FirmaId",
                    "CariAdi",
                    "CariVergiDairesi",
                    "CariVergiNumarasi",
                    "CariYetkiliMail",
                    "CariGrupId",
                    "CariAktifPasif"
                };

                foreach (var column in requiredColumns)
                {
                    if (!headerMap.ContainsKey(column))
                    {
                        errors.Add($"Gerekli sütun '{column}' bulunamadı.");
                    }
                }

                if (errors.Count > 0)
                    return (0, errors);

                var prepared = new List<Cari>();

                for (int r = 1; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;

                    var cari = new Cari
                    {
                        FirmaId = ParseIntCell(row, headerMap["FirmaId"]),
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
                        CariGrupId = ParseIntCell(row, headerMap["CariGrupId"]),
                        CariDovizKodu = headerMap.ContainsKey("CariDovizKodu")
                            ? ParseIntCell(row, headerMap["CariDovizKodu"])
                            : null,
                        CariAktifPasif = ParseIntCell(row, headerMap["CariAktifPasif"])
                    };

                    try
                    {
                        await ValidateCariAsync(cari);
                        prepared.Add(cari);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Satır {r + 1}: {ex.Message}");
                    }
                }

                if (errors.Count > 0)
                    return (0, errors);

                _db.Cariler.AddRange(prepared);
                await _db.SaveChangesAsync();

                created = prepared.Count;
                return (created, errors);
            }
            catch (Exception ex)
            {
                errors.Add($"İşlem sırasında hata oluştu: {ex.Message}");
                return (0, errors);
            }
        }
    }
}