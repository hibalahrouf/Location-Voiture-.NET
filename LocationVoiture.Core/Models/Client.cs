using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocationVoiture.Core.Models
{
    public class Client
    {
        [Key]
        public int ClientID { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nom { get; set; }

        [Required]
        [MaxLength(100)]
        public string Prenom { get; set; }

        [MaxLength(500)]
        public string? Adresse { get; set; }

        [MaxLength(20)]
        public string? Telephone { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; }

        [Required]
        public string MotDePasseHash { get; set; }
        [NotMapped] // Indique à EF Core de ne PAS créer cette colonne en BDD
        public string NomComplet
        {
            get { return $"{Prenom} {Nom}"; }
        }
        public virtual ICollection<Location> Locations { get; set; } = new List<Location>();
    }
}