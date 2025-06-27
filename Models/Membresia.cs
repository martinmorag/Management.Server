using Plataforma.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Management.Server.Models
{
    public class Membresia
    {
        [Key]
        public Guid Id { get; set; } // Using int for simplicity, could be GUID if preferred

        // Foreign Key to AspNetUsers (UsuarioIdentidad)
        [Required]
        [StringLength(450)] // Match the length of IdentityUser.Id
        public Guid UsuarioId { get; set; }
        [ForeignKey("UsuarioId")]
        public Cliente Usuario { get; set; } // Navigation property

        // Foreign Key to MembershipType
        [Required]
        public Guid TipoMembresiaId { get; set; }
        [ForeignKey("TipoMembresiaId")]
        public TipoMembresia TipoMembresia { get; set; } // Navigation property

        [Required]
        public DateTime FechaComienzo { get; set; }

        public DateTime? FechaFinalizacion { get; set; } // Null for ongoing, specific date for fixed terms

        [Required]
        public bool EstaActiva { get; set; } // True if the membership is currently active

        [Column(TypeName = "decimal(18, 2)")] // Actual price if different from MembershipType's standard price
        public decimal? PrecioPagado { get; set; }

        // Navigation property for related Payments
        public virtual ICollection<Pago> PagosGenerales { get; set; } = new List<Pago>();
        public ICollection<AjusteDeudaManual> AjustesDeudaManual { get; set; } = new List<AjusteDeudaManual>();
        public virtual ICollection<PagoMembresia> PagosMembresia { get; set; } = new List<PagoMembresia>();
    }
}
