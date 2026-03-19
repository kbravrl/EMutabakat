using EMutabakat.Models;
using Microsoft.EntityFrameworkCore;

namespace EMutabakat.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        public DbSet<Firma> Firmalar { get; set; }
        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<CariGrup> CariGruplar { get; set; }
        public DbSet<Cari> Cariler { get; set; }
        public DbSet<Mutabakat> Mutabakatlar { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Kullanici>()
                .HasOne(k => k.Firma)
                .WithMany()
                .HasForeignKey(k => k.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CariGrup>()
                .HasOne(cg => cg.Firma)
                .WithMany()
                .HasForeignKey(cg => cg.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cari>()
                .HasOne(c => c.Firma)
                .WithMany()
                .HasForeignKey(c => c.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cari>()
                .HasOne(c => c.CariGrup)
                .WithMany()
                .HasForeignKey(c => c.CariGrupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Mutabakat>()
                .HasOne(m => m.Firma)
                .WithMany()
                .HasForeignKey(m => m.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Mutabakat>()
                .HasOne(m => m.Cari)
                .WithMany()
                .HasForeignKey(m => m.CariId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}