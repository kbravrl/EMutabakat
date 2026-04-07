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
                using var smtpClient = new SmtpClient(kullanici.Firma.FirmaSmtpHost, kullanici.Firma.FirmaSmtpPort);

                smtpClient.Credentials = new NetworkCredential(
                    kullanici.Firma.FirmaSmtpUser,
                    kullanici.Firma.FirmaSmtpPassword);

                smtpClient.EnableSsl = IsSecureEnabled(kullanici.Firma.FirmaSmtpSecure);

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(kullanici.Firma.FirmaMail, kullanici.Firma.FirmaAdi),
                    Subject = BuildSubject(mutabakat, kullanici.Firma, isReminder),
                    Body = BuildHtmlBody(mutabakat, kullanici, approveUrl, rejectUrl),
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
            var donem = mutabakat.MutabakatDonemi.ToString("MM.yyyy");
            var prefix = isReminder ? "[Hatırlatma] " : "";

            return $"{prefix}{firma.FirmaAdi} Cari Hesap Mutabakatı - {donem}";
        }

        private string BuildHtmlBody(Mutabakat mutabakat, Kullanici kullanici, string approveUrl, string rejectUrl)
        {
            string donem = mutabakat.MutabakatDonemi.ToString("MM.yyyy");
            var bakiyeVal = mutabakat.MutabakatBakiye;
            string bakiye = bakiyeVal == Math.Truncate(bakiyeVal)
                ? ((long)bakiyeVal).ToString()
                : bakiyeVal.ToString("N2");
            string doviz = GetDovizName(mutabakat.MutabakatDovizKodu);
            string bakiyeTipi = GetBakiyeTipiText(mutabakat.MutabakatBakiyeTipi);

            return $@"
    <div style='font-family:Arial; line-height:1.6'>

        <h2 style='text-align:center;'>MUTABAKAT</h2>

        <p><b>Gönderen Firma :</b> {kullanici.Firma.FirmaAdi}</p>
        <p><b>Vergi Dairesi :</b> {kullanici.Firma.FirmaVergiDairesi ?? "-"}</p>
        <p><b>Vergi No :</b> {kullanici.Firma.FirmaVergiNumarasi ?? "-"}</p>

        <br/>

        <p><b>Muhatap Firma :</b> {mutabakat.Cari.CariUnvan}</p>
        <p><b>Vergi Dairesi :</b> {mutabakat.Cari.CariVergiDairesi ?? "-"}</p>
        <p><b>Vergi No :</b> {mutabakat.Cari.CariVergiNumarasi ?? "-"}</p>

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

        <p>
        Mutabık olmadığınız takdirde hesap ekstrenizin ivedilikle gönderilmesini rica ederiz.
        </p>

        <p><b>HATA VE UNUTMA MÜSTESNADIR.</b></p>

        <p>
        Firma ve iletişim bilgilerinizdeki değişiklikleri bildirmenizi rica ederiz.
        </p>

        <br/><br/>

        <a href='{approveUrl}' 
           style='background-color:green;color:white;padding:12px 20px;text-decoration:none;margin-right:10px;'>
           ✔ Mutabıkız
        </a>

        <a href='{rejectUrl}' 
           style='background-color:red;color:white;padding:12px 20px;text-decoration:none;'>
           ✖ Mutabık Değiliz
        </a>

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