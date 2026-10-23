using MediRappel.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MediRappel.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
    public DbSet<Dose> Doses => Set<Dose>();
    public DbSet<Ordonnance> Ordonnances => Set<Ordonnance>();
    public DbSet<Medicament> Medicaments => Set<Medicament>();
    public DbSet<RendezVous> RendezVous => Set<RendezVous>();
    public DbSet<Reglage> Reglages => Set<Reglage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Utilisateur>().HasIndex(u => u.Email).IsUnique();
        b.Entity<Utilisateur>()
            .HasOne(u => u.Reglage).WithOne(r => r.Utilisateur!)
            .HasForeignKey<Reglage>(r => r.UtilisateurId);
        b.Entity<Dose>().HasIndex(d => new { d.UtilisateurId, d.Date });
        b.Entity<RendezVous>().HasIndex(r => new { r.UtilisateurId, r.Date });
    }
}
