namespace Management.Server.Models.clientes
{
    public class ClientDetailsDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string NombreUsuario { get; set; }
        // Current Membership Details
        public Guid? TipoMembresiaIdActual { get; set; } // Nullable if client has no membership
        public DateTime? MembresiaFechaComienzoActual { get; set; } // Nullable
        public decimal? PrecioPagadoActual { get; set; } // Nullable
        public bool EstaActivaMembresiaActual { get; set; } // True if currently active
        public DateTime? FechaFinalizacionActual { get; set; } // Nullable        
    }
}
