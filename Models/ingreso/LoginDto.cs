using System.ComponentModel.DataAnnotations;

namespace Management.Server.Models.ingreso
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email es requerido")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Contraseña es requerida")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
