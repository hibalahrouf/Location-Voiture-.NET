using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocationVoiture.Core.Models
{
    public class Paiement
    {
        [Key]
        public int PaiementID { get; set; } // Clé Primaire

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Montant { get; set; }

        [Required]
        public DateTime DatePaiement { get; set; }

        [Required]
        [MaxLength(50)]
        public string MethodePaiement { get; set; } // ex: "Carte", "Espèces"

        // --- Relations ---
        public int LocationID { get; set; } // Clé Étrangère
        [ForeignKey("LocationID")]
        public virtual Location Location { get; set; }
    }
}