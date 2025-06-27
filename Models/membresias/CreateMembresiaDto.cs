using System.ComponentModel.DataAnnotations;

namespace Management.Server.Models.membresias
{
    public class CreateMembresiaDto
    {
        [Required(ErrorMessage = "El nombre del tipo de membresía es requerido.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los {1} caracteres.")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El precio mensual es requerido.")]
        [Range(0.01, 1000000.00, ErrorMessage = "El precio mensual debe ser un valor positivo.")]
        public decimal PrecioMensual { get; set; }

        // DuracionMeses is optional, representing perpetual membership if null/0
        [Range(0, int.MaxValue, ErrorMessage = "La duración en meses debe ser un número positivo o cero.")]
        public int? DuracionMeses { get; set; } = null; // Nullable, default to null

        public bool EstaActiva { get; set; } = true; // Default to active
    }
}
