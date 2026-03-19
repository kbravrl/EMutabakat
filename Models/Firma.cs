using System.ComponentModel.DataAnnotations;

namespace EMutabakat.Models
{
    public class Firma
    {
        [Key]
        public int FirmaId { get; set; }

        public string FirmaAdi { get; set; }
        public string FirmaUnvan { get; set; }
        public string FirmaAdres { get; set; }
        public string FirmaIlce { get; set; }
        public string FirmaIl { get; set; }
        public string FirmaVergiDairesi { get; set; }
        public string FirmaVergiNumarasi { get; set; }
        public string FirmaMersisNumarasi { get; set; }
        public string FirmaWebAdresi { get; set; }
        public string FirmaYetkiliAdiSoyadi { get; set; }
        public string FirmaMail { get; set; }
        public string FirmaTelefon { get; set; }
        public string FirmaGsm { get; set; }
        public string FirmaSmtpHost { get; set; }
        public int FirmaSmtpPort { get; set; }
        public string FirmaSmtpUser { get; set; }
        public string FirmaSmtpPassword { get; set; }
        public string FirmaSmtpSecure { get; set; }

        public int FirmaAktifPasif { get; set; }
    }
}