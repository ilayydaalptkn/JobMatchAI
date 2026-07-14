using System;
using System.Text.Json.Serialization;

namespace JobMatchAPI.Models
{
    public class Kullanici
    {
        public int Id { get; set; }
        public string AdSoyad { get; set; } = string.Empty;
        public string Eposta { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;

        [JsonIgnore]
        public string Sifre { get; set; } = string.Empty;

        public string Sehir { get; set; } = string.Empty;
        public string Rol { get; set; } = "Ogrenci";
        public DateTime KayitTarihi { get; set; } = DateTime.UtcNow;

        public string CvOkul { get; set; } = string.Empty;
        public string CvHedefIs { get; set; } = string.Empty;
        public string CvSehir { get; set; } = string.Empty;
        public string CvYetenekler { get; set; } = "[]";
        public string CvTecrubeler { get; set; } = "[]";
    }
}
