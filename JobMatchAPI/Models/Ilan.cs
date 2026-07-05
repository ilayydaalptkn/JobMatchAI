using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobMatchAPI.Models
{
    public class Ilan
    {
        [Key]
        public int Id { get; set; } // Veritabanındaki gerçek birincil anahtar

        // Veritabanında olmadığı için bunu da NotMapped yapıyoruz, sistem patlamıyor!
        [NotMapped]
        public int KullaniciId { get; set; }

        [Required]
        public string Baslik { get; set; } = string.Empty;

        [Required]
        public string Aciklama { get; set; } = string.Empty;

        [Required]
        public string SirketAdi { get; set; } = string.Empty;

        public string Sehir { get; set; } = string.Empty;

        public string Maas { get; set; } = string.Empty;

        public DateTime YayinlanmaTarihi { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public string YapayZekaSkoru { get; set; } = "%85";

        [NotMapped]
        public string EslesmeNedeni { get; set; } = "Profilinizdeki yetenekler bu ilanla uyuşuyor.";

        [NotMapped]
        public string EksikYetenekler { get; set; } = "Belirgin bir eksik bulunamadı.";
    }
}