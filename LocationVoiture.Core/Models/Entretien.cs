using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocationVoiture.Core.Models
{
    public class Entretien
    {
        [Key]
        public int EntretienID { get; set; } // Clé Primaire

        [Required]
        public DateTime DateEntretien { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } // ex: "Vidange", "Changement pneus"

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Cout { get; set; }

        // --- Relations ---
        public int VehiculeID { get; set; } // Clé Étrangère
        [ForeignKey("VehiculeID")]
        public virtual Vehicule Vehicule { get; set; }
    }
}