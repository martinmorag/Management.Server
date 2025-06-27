using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Management.Server.Models
{
    public class TipoMembresia
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } // e.g., "Mensual Básico", "Anual Premium"

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")] // Ensure correct precision for currency
        public decimal PrecioMensual { get; set; }

        public int? DuracionMeses { get; set; } // e.g., 12 for annual. Null for ongoing monthly.

        [Required]
        public bool EstaActiva { get; set; } // True if the plan is currently offered

        // Navigation property for related Memberships
        public ICollection<Membresia> Membresias { get; set; } = new List<Membresia>();
    }
}
