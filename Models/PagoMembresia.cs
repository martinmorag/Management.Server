using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Management.Server.Models
{
    public class PagoMembresia
    {
        [Key]
        public Guid Id { get; set; }

        public Guid MembresiaId { get; set; }
        [ForeignKey("MembresiaId")]
        public virtual Membresia Membresia { get; set; }

        [Required]
        // This date represents the start of the month for which the payment is due.
        // E.g., for January's payment, this would be 2025-01-01.
        public DateTime MesDePago { get; set; } // Stored in UTC (e.g., 2025-01-01 00:00:00Z)

        [Required]
        [Column(TypeName = "decimal(18, 2)")] // Ensure proper decimal precision in DB
        public decimal CantidadPagada { get; set; }

        [Required]
        public DateTime FechaDePago { get; set; } // When the payment was actually made (in UTC)

        public bool EsPagado { get; set; } = true; // Default to true if a payment record means it's paid

        // Optional: Could add payment method, transaction ID, etc.
        public string? MetodoPago { get; set; }
        public string? TransactionId { get; set; }
        public bool EsAjusteManual { get; set; } = false;
        public string Motivo { get; set; } = string.Empty; // Reason for the payment or adjustment
    }
}
