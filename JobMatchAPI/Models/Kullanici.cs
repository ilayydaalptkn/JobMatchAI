using System;

namespace JobMatchAPI.Models
{
    public class Kullanici
    {
        public int Id { get; set; }
        public string AdSoyad { get; set; } = string.Empty;
        public string Eposta { get; set; } = string.Empty;

        public string Telefon { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
        public string Sehir { get; set; } = string.Empty;
        public string Rol { get; set; } = "Ogrenci"; // "Ogrenci" veya "Isveren"
        public DateTime KayitTarihi { get; set; } = DateTime.UtcNow;
    }
}