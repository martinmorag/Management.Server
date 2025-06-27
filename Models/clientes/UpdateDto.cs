using System.ComponentModel.DataAnnotations;

namespace Management.Server.Models.clientes
{
    public class UpdateDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El email es requerido.")]
        [EmailAddress(ErrorMessage = "El email no es válido.")]
        public string Email { get; set; }
        [Required(ErrorMessage = "El nombre de usuario es requerido.")]
        [StringLength(256, ErrorMessage = "El nombre de usuario no puede exceder los {1} caracteres.")]
        public string NombreUsuario { get; set; }

        [StringLength(100, ErrorMessage = "La contraseña debe tener al menos {2} caracteres y un máximo de {1}.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string? Contrasena { get; set; } // Optional password for update

        [Compare("Contrasena", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
        public string? ConfirmarContrasena { get; set; }

        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(100)]
        public string Apellido { get; set; }

        // --- MEMBERSHIP FIELDS (assumed required for update logic) ---
        [Required(ErrorMessage = "El tipo de membresía es requerido.")]
        public Guid TipoMembresiaId { get; set; }

        [Required(ErrorMessage = "La fecha de inicio de la membresía es requerida.")]
        public DateTime MembresiaFechaComienzo { get; set; }

        [Range(0.01, 1000000.00, ErrorMessage = "El precio pagado debe ser un valor positivo.")]
        public decimal? PrecioPagado { get; set; }
    }
}
