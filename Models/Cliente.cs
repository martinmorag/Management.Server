using Plataforma.Models;

namespace Management.Server.Models
{
    public class Cliente : UsuarioIdentidad
    {
        public ICollection<Membresia> Membresias { get; set; } = new List<Membresia>();
    }
}
