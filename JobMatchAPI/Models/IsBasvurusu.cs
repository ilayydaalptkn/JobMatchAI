using System;

namespace JobMatchAPI.Models
{
    public class IsBasvurusu
    {
        public int Id { get; set; }
        public int KullaniciId { get; set; }        // Başvuran öğrencinin numarası (Eski UserId)
        public int IlanId { get; set; }             // Başvurulan ilanın numarası (Eski JobPostingId)
        public DateTime BasvuruTarihi { get; set; } = DateTime.UtcNow; // Başvuru Tarihi (Eski AppliedDate)
        public string Durum { get; set; } = "Beklemede"; // Başvuru Durumu (Eski Status)
    }
}