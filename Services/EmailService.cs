using System.Net;
using System.Net.Mail;
using System.Text;
using System.Diagnostics;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;

namespace EMutabakat.Services
{
    public class EmailService : IEmailService
    {
        public async Task<bool> SendMutabakatMailAsync(
            Mutabakat mutabakat,
            Kullanici kullanici,
            string approveUrl,
            string rejectUrl,
            bool isReminder = false)
        {
            try
            {
                var firma = kullanici.Firmalar?.FirstOrDefault();
                if (firma == null)
                    throw new Exception("Kullanıcıya bağlı firma bulunamadı.");

                using var smtpClient = new SmtpClient(firma.FirmaSmtpHost, firma.FirmaSmtpPort);

                smtpClient.Credentials = new NetworkCredential(
                    firma.FirmaSmtpUser,
                    firma.FirmaSmtpPassword);

                smtpClient.EnableSsl = IsSecureEnabled(firma.FirmaSmtpSecure);

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(firma.FirmaMail, firma.FirmaAdi),
                    Subject = BuildSubject(mutabakat, firma, isReminder),
                    Body = BuildHtmlBody(mutabakat, firma, approveUrl, rejectUrl),
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                mailMessage.To.Add(mutabakat.Cari.CariYetkiliMail);

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                var detail = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    detail += " -> " + inner.Message;
                    inner = inner.InnerException;
                }

                Debug.WriteLine($"SendMutabakatMailAsync failed: {detail}");
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
            var donem = mutabakat.MutabakatTarihi.ToString("MM.yyyy");
            var prefix = isReminder ? "[Hatırlatma] " : "";
            var cariFirmaAdi = mutabakat.Cari.Firma.FirmaAdi;

            return $"{prefix}{firma.FirmaAdi} - {donem} - {cariFirmaAdi}";
        }

        private string BuildHtmlBody(Mutabakat mutabakat, Firma firma, string approveUrl, string rejectUrl)
        {
            string donem = mutabakat.MutabakatTarihi.ToString("MM.yyyy");
            var bakiyeVal = mutabakat.MutabakatBakiye;
            string bakiye = bakiyeVal.ToString("N2");
            string doviz = GetDovizName(mutabakat.MutabakatDovizKodu);
            string bakiyeTipi = GetBakiyeTipiText(mutabakat.MutabakatBakiyeTipi);
            string yetkiliAdi = mutabakat.Cari.CariYetkiliAdiSoyadi;

            return $@"
    <div style='font-family:Verdana; line-height:1.6'>

        <h2 style='text-align:center;'>MUTABAKAT</h2>

        <p>Sayın <b>{yetkiliAdi}</b>,</p>

        <br/>

        <p>
        Şirketimiz nezdindeki Cari Hesabınız
        <b>{donem}</b> dönemi itibari ile
        <b>{bakiye} {doviz} {bakiyeTipi}</b>
        bakiyeniz bulunmaktadır.
        </p>

        <p>
        Mutabık olup olmadığınızı bildirmenizi, mutabık olunmaması durumunda
        cari hesap ekstrenizi göndermenizi rica ederiz.
        </p>

        <br/>

        <p>
        T.T.K. 92. maddesi gereği mutabakat veya itirazınızı 1 ay içinde
        bildirmediğiniz takdirde bakiyede mutabık sayılacağımızı bilgilerinize sunarız.
        </p>

        <p><b>HATA VE UNUTMA MÜSTESNADIR.</b></p>

        <p>
        Firma ve iletişim bilgilerinizdeki değişiklikler için lütfen bizimle iletişime geçiniz.
        </p>

        <br/><br/>

        <table cellpadding='0' cellspacing='0' style='margin-top:24px;'>
            <tr>
                <td style='padding-right:16px;'>
                    <a href='{approveUrl}'
                       style='display:inline-block;
                              background-color:#16a34a;
                              color:white;
                              font-family:Verdana;
                              font-size:14px;
                              font-weight:bold;
                              padding:8px 16px;
                              border-radius:6px;
                              text-decoration:none;
                              letter-spacing:0.5px;
                              box-shadow:0 2px 6px rgba(0,0,0,0.2);'>
                              Mutabıkız ✔
                    </a>
                </td>
                <td>
                    <a href='{rejectUrl}'
                       style='display:inline-block;
                              background-color:#dc2626;
                              color:white;
                              font-family:Verdana;
                              font-size:14px;
                              font-weight:bold;
                              padding:8px 16px;
                              border-radius:6px;
                              text-decoration:none;
                              letter-spacing:0.5px;
                              box-shadow:0 2px 6px rgba(0,0,0,0.2);'>
                              Mutabık Değiliz ✖
                    </a>
                </td>
            </tr>
        </table>

        <br/><br/>
    </div>
    ";
        }

        private static string GetDovizName(string? kod)
        {
            return (kod ?? string.Empty).ToUpperInvariant() switch
            {
                "TL" => "Türk lirası",
                "USD" => "Amerikan doları",
                "EUR" => "Euro",
                _ => "Tanımsız"
            };
        }

        private static string GetBakiyeTipiText(string bakiyeTipi)
        {
            return bakiyeTipi switch
            {
                "B" => "Borç",
                "A" => "Alacak",
                _ => bakiyeTipi ?? "-"
            };
        }
    }
}