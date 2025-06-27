using System.ComponentModel.DataAnnotations;

namespace Management.Server.Models.membresias
{
    public class PagoMembresiaDto
    {
        [Required(ErrorMessage = "El mes de pago es requerido.")]
        public DateTime MesDePago { get; set; } // Only Month and Year matter, but full DateTime is used for consistency

        [Required(ErrorMessage = "La cantidad pagada es requerida.")]
        [Range(0.01, 1000000.00, ErrorMessage = "La cantidad pagada debe ser un valor positivo.")]
        public decimal CantidadPagada { get; set; }

        [StringLength(50, ErrorMessage = "El método de pago no puede exceder los {1} caracteres.")]
        public string? MetodoPago { get; set; }
    }
}
