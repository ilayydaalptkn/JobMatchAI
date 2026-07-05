using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization; // 🔥 Bu namespace'i yukarıya ekle!

namespace JobMatchAPI.Models
{
    public class Basvuru
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int KullaniciId { get; set; }

        [Required]
        public int IlanId { get; set; }

        public string Durum { get; set; } = "Beklemede";

        public DateTime BasvuruTarihi { get; set; } = DateTime.UtcNow;

        [ForeignKey("KullaniciId")]
        [JsonIgnore] // 🔥 KRİTİK: Swagger'ın sonsuz döngüye girmesini ve 500 vermesini engeller!
        public virtual Kullanici? Kullanici { get; set; }
        public int? AiSkoru { get; set; } // %0 - %100 arası yapay zeka puanı
        public string? AiGeriBildirim { get; set; } // Detaylı CV analizi raporu
        public string? AiNitelikOzeti { get; set; } // Adayın öne çıkan güçlü/zayıf yönleri
    }
}