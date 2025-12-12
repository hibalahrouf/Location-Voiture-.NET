using System.ComponentModel.DataAnnotations;


namespace LocationVoiture.Core.Models
{
    public class Employe
    {
        [Key]
        public int EmployeID { get; set; } // Clé Primaire

        [Required]
        [MaxLength(100)]
        public string Nom { get; set; }

        [Required]
        [MaxLength(100)]
        public string Prenom { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } // Pour la connexion au back-office

        [Required]
        public string MotDePasseHash { get; set; }

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } // ex: "Admin", "Agent"

        // --- Relations ---
        public virtual ICollection<Location> LocationsGerees { get; set; }
    }
}