using Management.Server.Models;
using Microsoft.AspNetCore.Identity;

namespace Plataforma.Models
{
    public class UsuarioIdentidad : IdentityUser<Guid>
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Discriminator { get; set; }
    }
}