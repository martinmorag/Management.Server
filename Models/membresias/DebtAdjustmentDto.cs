using System.ComponentModel.DataAnnotations;

namespace Management.Server.Models.membresias
{
    public class DebtAdjustmentDto
    {
        [Required(ErrorMessage = "El mes de ajuste es requerido.")]
        public DateTime? MesAjustado { get; set; } // Month and year to adjust (e.g., first day of the month in UTC)

        [Required(ErrorMessage = "El motivo del ajuste es requerido.")]
        [MaxLength(200, ErrorMessage = "El motivo no puede exceder los 200 caracteres.")]
        public string Motivo { get; set; } // e.g., "Pago Offline", "Descuento por promoción", "Cargo por mora"

        [Range(0.00, (double)decimal.MaxValue, ErrorMessage = "El monto esperado no puede ser negativo.")]
        public decimal? MontoEsperadoNuevo { get; set; }

        // Optional. If provided, this will set the *new* amount considered paid for the month.
        [Range(0.00, (double)decimal.MaxValue, ErrorMessage = "El monto pagado no puede ser negativo.")]
        public decimal? MontoPagadoNuevo { get; set; }

        public bool IsAnyAdjustmentProvided => MontoEsperadoNuevo.HasValue || MontoPagadoNuevo.HasValue;
    }
    public enum AdjustmentType
    {
        PagoCompleto,
        AumentoDeuda,
        DisminucionDeuda,
        RevertirPago
    }
}
