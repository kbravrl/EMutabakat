using EMutabakat.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

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
        public DbSet<KullaniciYetki> KullaniciYetkileri { get; set; }
        public DbSet<CariGrup> CariGruplar { get; set; }
        public DbSet<Cari> Cariler { get; set; }
        public DbSet<Mutabakat> Mutabakatlar { get; set; }
        public DbSet<SilinenMutabakat> SilinenMutabakatlar { get; set; }
        public DbSet<DovizKodu> DovizKodlari { get; set; }
        public DbSet<AppLog> AppLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Kullanici>()
                .HasMany(k => k.Firmalar)
                .WithMany(f => f.Kullanicilar)
                .UsingEntity<Dictionary<string, object>>(
                    "KullaniciFirmalar",
                    j => j.HasOne<Firma>()
                          .WithMany()
                          .HasForeignKey("FirmaId")
                          .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Kullanici>()
                          .WithMany()
                          .HasForeignKey("KullaniciId")
                          .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.Property<int>("KullaniciFirmaId")
                         .HasColumnName("KullaniciFirmaId")
                         .ValueGeneratedOnAdd();

                        j.HasKey("KullaniciFirmaId");
                        j.HasIndex("KullaniciId", "FirmaId").IsUnique();
                        j.ToTable("KullaniciFirmalar");
                    }
                );

            modelBuilder.Entity<Kullanici>()
                .HasOne(k => k.Yetkileri)
                .WithOne(y => y.Kullanici)
                .HasForeignKey<KullaniciYetki>(y => y.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CariGrup>()
                .HasKey(cg => new { cg.CariGrupId, cg.FirmaId });

            modelBuilder.Entity<CariGrup>()
                .HasIndex(cg => new { cg.FirmaId, cg.CariGrupAdi });

            modelBuilder.Entity<CariGrup>()
                .HasOne(cg => cg.Firma)
                .WithMany()
                .HasForeignKey(cg => cg.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cari>()
                .HasKey(c => new { c.CariId, c.FirmaId });

            modelBuilder.Entity<Cari>()
                .HasIndex(c => new { c.FirmaId, c.CariAdi })
                .IsUnique();

            modelBuilder.Entity<Cari>()
                .HasOne(c => c.Firma)
                .WithMany()
                .HasForeignKey(c => c.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cari>()
                .HasOne(c => c.CariGrup)
                .WithMany()
                .HasForeignKey(c => new { c.CariGrupId, c.FirmaId })
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cari>()
                .HasOne(c => c.DovizKodu)
                .WithMany()
                .HasForeignKey(c => c.CariDovizKodu)
                .HasPrincipalKey(d => d.TCMB)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DovizKodu>()
                .HasAlternateKey(d => d.TCMB);

            modelBuilder.Entity<Mutabakat>()
                .HasKey(m => new { m.FirmaId, m.CariId, m.MutabakatTarihi });

            modelBuilder.Entity<Mutabakat>()
                .HasIndex(m => new { m.FirmaId, m.MutabakatId })
                .IsUnique();

            modelBuilder.Entity<Mutabakat>()
                .HasOne(m => m.Firma)
                .WithMany()
                .HasForeignKey(m => m.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Mutabakat>()
                .HasOne(m => m.Cari)
                .WithMany()
                .HasForeignKey(m => new { m.CariId, m.FirmaId })
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Mutabakat>()
                .HasOne(m => m.DovizKodu)
                .WithMany()
                .HasForeignKey(m => m.MutabakatDovizKodu)
                .HasPrincipalKey(d => d.TCMB)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SilinenMutabakat>()
                .HasKey(sm => sm.Id);

            modelBuilder.Entity<SilinenMutabakat>()
                .HasOne(sm => sm.Firma)
                .WithMany()
                .HasForeignKey(sm => sm.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SilinenMutabakat>()
                .HasOne(sm => sm.Cari)
                .WithMany()
                .HasForeignKey(sm => new { sm.CariId, sm.FirmaId })
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SilinenMutabakat>()
                .HasOne(sm => sm.DovizKodu)
                .WithMany()
                .HasForeignKey(sm => sm.MutabakatDovizKodu)
                .HasPrincipalKey(d => d.TCMB)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SilinenMutabakat>()
                .HasIndex(sm => new { sm.FirmaId, sm.CariId, sm.MutabakatTarihi });

            modelBuilder.Entity<AppLog>()
                .HasIndex(x => x.CreatedAt);

            modelBuilder.Entity<AppLog>()
                .Property(x => x.Level)
                .HasMaxLength(20);

            modelBuilder.Entity<AppLog>()
                .Property(x => x.Source)
                .HasMaxLength(150);

            modelBuilder.Entity<AppLog>()
                .Property(x => x.UserEmail)
                .HasMaxLength(200);
        }
    }
}