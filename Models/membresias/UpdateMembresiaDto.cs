using System.ComponentModel.DataAnnotations;

namespace Management.Server.Models.membresias
{
    public class UpdateMembresiaDto
    {
        [Required]
        public Guid Id { get; set; } // Required for update

        [Required(ErrorMessage = "El nombre del tipo de membresía es requerido.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los {1} caracteres.")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El precio mensual es requerido.")]
        [Range(0.01, 1000000.00, ErrorMessage = "El precio mensual debe ser un valor positivo.")]
        public decimal PrecioMensual { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "La duración en meses debe ser un número positivo o cero.")]
        public int? DuracionMeses { get; set; }

        public bool EstaActiva { get; set; }
    }
}
