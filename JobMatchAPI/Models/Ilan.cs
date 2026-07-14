using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobMatchAPI.Models
{
    public class Ilan
    {
        [Key]
        public int Id { get; set; }

        public int? KullaniciId { get; set; }

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
        public int? YapayZekaSkoru { get; set; }

        [NotMapped]
        public string? EslesmeNedeni { get; set; }
    }
}
