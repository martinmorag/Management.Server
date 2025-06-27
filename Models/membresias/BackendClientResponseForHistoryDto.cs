namespace Management.Server.Models.membresias
{
    public class BackendClientResponseForHistoryDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public string TipoMembresia { get; set; }
        public bool EstadoMembresia { get; set; }
        public string? NombreUsuario { get; set; } // Nullable
        public Guid? CurrentMembresiaId { get; set; } // Nullable, matches frontend's string
        public List<PaymentMonthStatusDto> PaymentHistory { get; set; } = new List<PaymentMonthStatusDto>();
    }
}
