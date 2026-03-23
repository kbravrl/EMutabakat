using System.Net;
using System.Net.Mail;
using System.Text;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;

namespace EMutabakat.Services
{
    public class EmailService : IEmailService
    {
        public async Task<bool> SendMutabakatMailAsync(
            Mutabakat mutabakat,
            Firma firma,
            Cari cari,
            string approveUrl,
            string rejectUrl,
            bool isReminder = false)
        {
            try
            {
                using var smtpClient = new SmtpClient(firma.FirmaSmtpHost, firma.FirmaSmtpPort);

                smtpClient.Credentials = new NetworkCredential(
                    firma.FirmaSmtpUser,
                    firma.FirmaSmtpPassword);

                smtpClient.EnableSsl = IsSecureEnabled(firma.FirmaSmtpSecure);

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(firma.FirmaMail, firma.FirmaAdi),
                    Subject = BuildSubject(mutabakat, firma, isReminder),
                    Body = BuildHtmlBody(mutabakat, firma, cari, approveUrl, rejectUrl, isReminder),
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                mailMessage.To.Add(cari.CariYetkiliMail);

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsSecureEnabled(string secureValue)
        {
            if (string.IsNullOrWhiteSpace(secureValue))
                return false;

            var value = secureValue.Trim().ToLower();

            return value == "1" ||
                   value == "true" ||
                   value == "ssl" ||
                   value == "tls";
        }

        private static string BuildSubject(Mutabakat mutabakat, Firma firma, bool isReminder)
        {
            var donem = mutabakat.MutabakatDonemi.ToString("MM.yyyy");
            var prefix = isReminder ? "[Hatırlatma] " : "";

            return $"{prefix}{firma.FirmaAdi} Cari Hesap Mutabakatı - {donem}";
        }

        private static string BuildHtmlBody(
            Mutabakat mutabakat,
            Firma firma,
            Cari cari,
            string approveUrl,
            string rejectUrl,
            bool isReminder)
        {
            var bakiyeText = mutabakat.MutabakatBakiye.ToString("N2");
            var dovizText = GetDovizText(mutabakat.MutabakatDovizKodu);
            var bakiyeTipiText = mutabakat.MutabakatBakiyeTipi == "B" ? "Borç" : "Alacak";
            var reminderText = isReminder
                ? "<p style='color:#b45309; font-weight:600;'>Bu bir hatırlatma mailidir.</p>"
                : "";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
</head>
<body style='font-family:Arial, Helvetica, sans-serif; background:#f7f7f7; padding:24px;'>
    <div style='max-width:700px; margin:0 auto; background:#ffffff; border-radius:12px; padding:32px; border:1px solid #e5e7eb;'>
        <h2 style='margin-top:0;'>Cari Hesap Mutabakatı</h2>

        {reminderText}

        <p>Sayın Yetkili,</p>

        <p>
            <strong>{firma.FirmaAdi}</strong> firması tarafından tarafınıza cari hesap mutabakatı gönderilmiştir.
        </p>

        <table style='width:100%; border-collapse:collapse; margin:24px 0;'>
            <tr>
                <td style='padding:8px; border:1px solid #e5e7eb;'><strong>Firma</strong></td>
                <td style='padding:8px; border:1px solid #e5e7eb;'>{firma.FirmaAdi}</td>
            </tr>
            <tr>
                <td style='padding:8px; border:1px solid #e5e7eb;'><strong>Cari</strong></td>
                <td style='padding:8px; border:1px solid #e5e7eb;'>{cari.CariAdi}</td>
            </tr>
            <tr>
                <td style='padding:8px; border:1px solid #e5e7eb;'><strong>Dönem</strong></td>
                <td style='padding:8px; border:1px solid #e5e7eb;'>{mutabakat.MutabakatDonemi:dd.MM.yyyy}</td>
            </tr>
            <tr>
                <td style='padding:8px; border:1px solid #e5e7eb;'><strong>Döviz</strong></td>
                <td style='padding:8px; border:1px solid #e5e7eb;'>{dovizText}</td>
            </tr>
            <tr>
                <td style='padding:8px; border:1px solid #e5e7eb;'><strong>Bakiye</strong></td>
                <td style='padding:8px; border:1px solid #e5e7eb;'>{bakiyeText} ({bakiyeTipiText})</td>
            </tr>
            <tr>
                <td style='padding:8px; border:1px solid #e5e7eb;'><strong>Açıklama</strong></td>
                <td style='padding:8px; border:1px solid #e5e7eb;'>{mutabakat.MutabakatAciklama}</td>
            </tr>
        </table>

        <p>Lütfen kayıtlarınızı kontrol ederek aşağıdaki seçeneklerden birini kullanınız:</p>

        <div style='margin-top:24px; margin-bottom:24px;'>
            <a href='{approveUrl}'
               style='display:inline-block; padding:12px 20px; margin-right:10px; background:#16a34a; color:#fff; text-decoration:none; border-radius:8px;'>
               Mutabıkız
            </a>

            <a href='{rejectUrl}'
               style='display:inline-block; padding:12px 20px; background:#dc2626; color:#fff; text-decoration:none; border-radius:8px;'>
               Mutabık Değiliz
            </a>
        </div>

        <p style='font-size:13px; color:#6b7280;'>
            Sorularınız için bizimle iletişime geçebilirsiniz.
        </p>
    </div>
</body>
</html>";
        }

        private static string GetDovizText(int kod)
        {
            return kod switch
            {
                0 => "00 - TL",
                1 => "01 - USD",
                2 => "02 - EUR",
                _ => "Tanımsız"
            };
        }
    }
}