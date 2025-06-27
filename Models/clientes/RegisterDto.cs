using System.ComponentModel.DataAnnotations;

namespace Management.Server.Models.clientes
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "El email es requerido.")]
        [EmailAddress(ErrorMessage = "El email no es válido.")]
        public string Email { get; set; }
        [Required(ErrorMessage = "El nombre de usuario es requerido.")]
        public string NombreUsuario { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida.")]
        [StringLength(100, ErrorMessage = "La contraseña debe tener al menos {9} caracteres.", MinimumLength = 9)]
        [DataType(DataType.Password)]
        public string Contrasena { get; set; }

        [Required(ErrorMessage = "La confirmación de contraseña es requerida.")]
        [Compare("Contrasena", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
        public string ConfirmarContrasena { get; set; }

        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(100)]
        public string Apellido { get; set; }
        [Required(ErrorMessage = "El tipo de membresía es requerido.")]
        public Guid TipoMembresiaId { get; set; }

        [Required(ErrorMessage = "La fecha de inicio de la membresía es requerida.")]
        public DateTime MembresiaFechaComienzo { get; set; }

        // Optional: If the admin wants to override the price
        [Range(0.01, 1000000.00, ErrorMessage = "El precio pagado debe ser un valor positivo.")]
        public decimal? PrecioPagado { get; set; }
    }
}
