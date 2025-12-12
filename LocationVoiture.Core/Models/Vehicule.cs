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

        public bool Disponible { get; set; } = true; // Pour "Gestion de la disponibilité"

        // --- Relations ---
        public int TypeVehiculeID { get; set; } // Clé Étrangère
        [ForeignKey("TypeVehiculeID")]
        public virtual TypeVehicule TypeVehicule { get; set; }

        public virtual ICollection<Location> Locations { get; set; }
        public virtual ICollection<Entretien> Entretiens { get; set; }

        [NotMapped]
        public decimal PrixJournee => TypeVehicule?.Tarifs?.FirstOrDefault()?.PrixParJour ?? 0;
    }
}