namespace Management.Server.Models.membresias
{
    public class PaymentHistoryDto
    {
        public Guid? MembresiaId { get; set; }
        public string TipoMembresia { get; set; }
        public decimal PrecioMensualMembresia { get; set; }
        public List<PaymentMonthStatusDto> PaymentMonths { get; set; }
        public string Message { get; set; }
    }
}
