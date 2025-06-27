namespace Management.Server.Models.membresias
{
    public class PaymentMonthStatusDto
    {
        public Guid MembresiaId { get; set; }
        public DateTime Mes { get; set; }
        public bool EsPagado { get; set; }
        public decimal CantidadPagada { get; set; } // The amount recorded as paid
        public decimal PrecioMensualMembresia { get; set; } // The original base price for the month
        public decimal TotalAjustes { get; set; } // Sum of all charges (positive) and credits (negative) for this month
        public decimal MontoAdeudado { get; set; } // The final calculated outstanding amount for this month
        public string MetodoPago { get; set; }
        public DateTime? FechaDePago { get; set; }
        public bool EsAjusteManual { get; set; }
        public string EstadoTexto { get; set; }
    }
}
