using Microsoft.EntityFrameworkCore;
using JobMatchAPI.Models;

namespace JobMatchAPI.Data
{
    public class VeriTabaniBaglantisi : DbContext
    {
        public VeriTabaniBaglantisi(DbContextOptions<VeriTabaniBaglantisi> options) : base(options)
        {
        }

        // --- VERİTABANI TABLOLARI (DbSet) ---

        // Kullanıcılar tablosu
        public DbSet<Kullanici> Kullanicilar { get; set; }

        // 🔥 TEKİL VE NET TANIM: Çakışmayı önlemek için sadece IsIlani modelini kullanan tek bir Ilanlar tablosu bırakıyoruz
        public DbSet<Ilan> Ilanlar { get; set; }

        // Başvurular tablosu
        public DbSet<Basvuru> Basvurular { get; set; }
    }
}