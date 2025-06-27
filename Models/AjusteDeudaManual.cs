using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Management.Server.Models
{
    public class AjusteDeudaManual
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid MembresiaId { get; set; }
        [ForeignKey("MembresiaId")]
        public Membresia Membresia { get; set; } // Navigation property

        [Required]
        public DateTime MesAplicado { get; set; } // The month the adjustment applies to (normalized to 1st day UTC)

        [Required]
        [Column(TypeName = "decimal(18, 2)")] // Ensure correct precision for currency
        public decimal CantidadAjustada { get; set; } // Positive for charge, negative for credit/discount

        [Required]
        [MaxLength(50)]
        public string TipoAjusteInterno { get; set; } // E.g., "Charge", "Discount", "Correction"

        [Required]
        [MaxLength(250)]
        public string Motivo { get; set; } // Detailed reason for the adjustment

        public DateTime FechaDeRegistro { get; set; } = DateTime.UtcNow; // When this adjustment was recorded

        public string RegistradoPor { get; set; } // e.g., Username or User ID
    }
}
