using System.ComponentModel.DataAnnotations;

namespace LocationVoiture.Core.Models
{
    public class TypeVehicule
    {
        [Key]
        public int TypeVehiculeID { get; set; } // Clé Primaire

        [Required]
        [MaxLength(100)]
        public string Nom { get; set; } // ex: Berline, SUV, Utilitaire

        public string? Description { get; set; }

        // --- Relations ---
        public virtual ICollection<Vehicule> Vehicules { get; set; }
        public virtual ICollection<Tarif> Tarifs { get; set; }
    }
}