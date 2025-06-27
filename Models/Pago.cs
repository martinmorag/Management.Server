using Plataforma.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Management.Server.Models
{
    public class Pago
    {
        [Key]
        public Guid Id { get; set; } // Using int for simplicity, could be GUID if preferred

        // Foreign Key to AspNetUsers (UsuarioIdentidad)
        [Required]
        [StringLength(450)] // Match the length of IdentityUser.Id
        public Guid UsuarioId { get; set; }
        [ForeignKey("UsuarioId")]
        public UsuarioIdentidad Usuario { get; set; } // Navigation property

        // Foreign Key to Membership (Optional, but good for linking to a specific subscription instance)
        public Guid? MembresiaId { get; set; } // Nullable if a payment isn't tied to a specific membership instance (e.g., initial signup fee)
        [ForeignKey("MembresiaId")]
        public Membresia Membresia { get; set; } // Navigation property

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Cantidad { get; set; }

        [Required]
        public DateTime FechaPago { get; set; }

        [StringLength(50)]
        public string MetodoPago { get; set; } // e.g., "Cash", "Credit Card", "Transfer"

        [Required]
        [StringLength(50)]
        public string Estado { get; set; } // e.g., "Paid", 
    }
}
