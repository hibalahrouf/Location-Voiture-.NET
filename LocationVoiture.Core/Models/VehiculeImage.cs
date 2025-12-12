using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocationVoiture.Core.Models
{
    public class VehiculeImage
    {
        [Key]
        public int ImageID { get; set; }

        [Required]
        public string ImagePath { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;

        public int VehiculeID { get; set; }
        
        [ForeignKey("VehiculeID")]
        public virtual Vehicule Vehicule { get; set; } = null!;
    }
}
