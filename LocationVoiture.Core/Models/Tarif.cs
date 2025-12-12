using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocationVoiture.Core.Models
{
    public class Tarif
    {
        [Key]
        public int TarifID { get; set; } // Clé Primaire

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PrixParJour { get; set; }

        // --- Relations ---
        public int TypeVehiculeID { get; set; } // Clé Étrangère
        [ForeignKey("TypeVehiculeID")]
        public virtual TypeVehicule TypeVehicule { get; set; }
    }
}