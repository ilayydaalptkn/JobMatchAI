using Microsoft.EntityFrameworkCore;
using JobMatchAPI.Models;

namespace JobMatchAPI.Data
{
    public class VeriTabaniBaglantisi : DbContext
    {
        public VeriTabaniBaglantisi(DbContextOptions<VeriTabaniBaglantisi> options) : base(options) { }

        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<Ilan> Ilanlar { get; set; }
        public DbSet<Basvuru> Basvurular { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Basvuru>()
                .HasOne(b => b.Kullanici)
                .WithMany()
                .HasForeignKey(b => b.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Basvuru>()
                .HasOne<Ilan>()
                .WithMany()
                .HasForeignKey(b => b.IlanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Ilan>()
                .HasOne<Kullanici>()
                .WithMany()
                .HasForeignKey(i => i.KullaniciId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
