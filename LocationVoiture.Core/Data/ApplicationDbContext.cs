using LocationVoiture.Core.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LocationVoiture.Core.Data
{
    // Il hérite de IdentityDbContext pour gérer les utilisateurs ET vos modèles
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Ajoutez tous vos DbSet personnalisés ici
        public DbSet<Client> Clients { get; set; }
        public DbSet<Employe> Employes { get; set; }
        public DbSet<Vehicule> Vehicules { get; set; }
        public DbSet<TypeVehicule> TypesVehicules { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Paiement> Paiements { get; set; }
        public DbSet<Tarif> Tarifs { get; set; }
        public DbSet<Entretien> Entretiens { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
    }
}