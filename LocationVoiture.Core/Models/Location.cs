using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocationVoiture.Core.Models
{
    public enum LocationStatut
    {
        EnAttente,
        Confirmee,
        Terminee,
        Annulee
    }

    public class Location
    {
        [Key]
        public int LocationID { get; set; } // Clé Primaire

        [Required]
        public DateTime DateDebut { get; set; }

        [Required]
        public DateTime DateFin { get; set; }

        public DateTime? DateRetourReelle { get; set; } // Pour le suivi

        [Required]
        public LocationStatut Statut { get; set; }

        // Token for email confirmation of rental request
        public string? ConfirmationToken { get; set; }

        [Column(TypeName = "decimal(18, 2)")] // Pour l'argent
        public decimal MontantTotal { get; set; }

        // --- Relations ---
        public int ClientID { get; set; } // Clé Étrangère
        [ForeignKey("ClientID")]
        public virtual Client Client { get; set; }

        public int VehiculeID { get; set; } // Clé Étrangère
        [ForeignKey("VehiculeID")]
        public virtual Vehicule Vehicule { get; set; }

        public int? EmployeID { get; set; } // Clé Étrangère (optionnelle, validée par admin)
        [ForeignKey("EmployeID")]
        public virtual Employe? Employe { get; set; }

        public virtual ICollection<Paiement> Paiements { get; set; }
    }
}