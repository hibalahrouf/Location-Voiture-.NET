using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace LocationVoiture.Core.Models
{
    public class Vehicule
    {
        [Key]
        public int VehiculeID { get; set; } // Clé Primaire

        [Required]
        [MaxLength(20)]
        public string Immatriculation { get; set; }

        [Required]
        [MaxLength(100)]
        public string Marque { get; set; }

        [Required]
        [MaxLength(100)]
        public string Modele { get; set; }

        public int Annee { get; set; }

        public string? Couleur { get; set; }

        public string? ImageURL { get; set; } // Pour "Gestion des... images"

        // Stock quantity management
        public int QuantiteTotal { get; set; } = 1; // Total units of this vehicle model
        public int QuantiteDisponible { get; set; } = 1; // Available units for rent

        public DateTime? DateProchainEntretien { get; set; } // Next scheduled maintenance date

        // Computed property - true if any units available
        [NotMapped]
        public bool Disponible => QuantiteDisponible > 0;

        // --- Relations ---
        public int TypeVehiculeID { get; set; } // Clé Étrangère
        [ForeignKey("TypeVehiculeID")]
        public virtual TypeVehicule TypeVehicule { get; set; }

        public virtual ICollection<Location> Locations { get; set; } = new List<Location>();
        public virtual ICollection<Entretien> Entretiens { get; set; } = new List<Entretien>();
        public virtual ICollection<VehiculeImage> Images { get; set; } = new List<VehiculeImage>();

        [NotMapped]
        public decimal PrixJournee => TypeVehicule?.Tarifs?.FirstOrDefault()?.PrixParJour ?? 0;
    }
}