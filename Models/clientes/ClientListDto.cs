namespace Management.Server.Models.clientes
{
    public class ClientListDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public string TipoMembresia { get; set; } // The name of their current membership type
        public bool EstadoMembresia { get; set; }
        public string? CurrentMembresiaId { get; set; }
    }
}
