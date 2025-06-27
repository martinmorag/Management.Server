using Management.Server.Data;
using Management.Server.Models;
using Management.Server.Models.membresias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Plataforma.Models;

namespace Management.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthScndController : ControllerBase
    {
        private readonly UserManager<UsuarioIdentidad> _userManager;
        private readonly SignInManager<UsuarioIdentidad> _signInManager;
        private readonly ILogger<AuthController> _logger;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly ManagementContext _context;

        public AuthScndController(
            UserManager<UsuarioIdentidad> userManager,
            SignInManager<UsuarioIdentidad> signInManager,
            ILogger<AuthController> logger,
            RoleManager<IdentityRole<Guid>> roleManager,
            ManagementContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet("clientes/{userId}/membresia/pagos/estado")]
        public async Task<List<PaymentMonthStatusDto>> GetClientPaymentStatus(Guid membresiaId)
        {
                // 1. Fetch the membership details, including its type for the monthly price
                var membresia = await _context.Membresias
                    .Include(m => m.TipoMembresia)
                    .FirstOrDefaultAsync(m => m.Id == membresiaId);

                if (membresia == null)
                {
                    // You might want to throw an exception or return an empty list/specific message
                    return new List<PaymentMonthStatusDto>();
                }

                // 2. Pre-load all payments and manual adjustments for this membership
                //    This minimizes database roundtrips inside the loop.
                var allPayments = await _context.PagosMembresia
                    .Where(p => p.MembresiaId == membresiaId)
                    .ToListAsync();

                var allAdjustments = await _context.AjustesDeudaManual
                    .Where(a => a.MembresiaId == membresiaId)
                    .ToListAsync();

                var monthlyStatuses = new List<PaymentMonthStatusDto>();
                var todayUtc = DateTime.SpecifyKind(new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1), DateTimeKind.Utc);
                var membershipStartDateUtc = DateTime.SpecifyKind(new DateTime(membresia.FechaComienzo.Year, membresia.FechaComienzo.Month, 1), DateTimeKind.Utc);

                // Determine the end date for the history loop.
                // It should go up to the current month, or the membership's end date if it's in the past.
                DateTime loopEndDateUtc;
                if (membresia.FechaFinalizacion.HasValue && membresia.FechaFinalizacion.Value.ToUniversalTime() < todayUtc)
                {
                    loopEndDateUtc = DateTime.SpecifyKind(new DateTime(membresia.FechaFinalizacion.Value.Year, membresia.FechaFinalizacion.Value.Month, 1), DateTimeKind.Utc);
                }
                else
                {
                    loopEndDateUtc = todayUtc;
                }

                decimal monthlyPrice = membresia.TipoMembresia?.PrecioMensual ?? 0;

                // 3. Iterate through each month from the membership start to the determined end date
                for (DateTime month = membershipStartDateUtc; month <= loopEndDateUtc; month = month.AddMonths(1))
                {
                    // Find relevant data for the current month from pre-loaded lists
                    var paymentForMonth = allPayments
                        .FirstOrDefault(p => p.MesDePago == month); // Using direct comparison as MesDePago is normalized

                    var adjustmentsForMonth = allAdjustments
                        .Where(a => a.MesAplicado == month) // Using direct comparison as MesAplicado is normalized
                        .ToList();

                    // Calculate financial figures for the month
                    decimal totalAjustes = adjustmentsForMonth.Sum(a => a.CantidadAjustada); // Sums positive (charges) and negative (credits)
                    decimal totalPagado = paymentForMonth?.CantidadPagada ?? 0;

                    // The actual amount that needs to be covered (base price + charges - credits)
                    decimal montoEsperadoACubrir = monthlyPrice + totalAjustes;

                    // The outstanding balance for this specific month
                    decimal montoFinalAdeudado = montoEsperadoACubrir - totalPagado;

                    // Determine status based on the calculated final amount
                    string estadoTexto;
                    bool esPagadoBooleano;

                    if (montoFinalAdeudado <= 0)
                    {
                        estadoTexto = "Pagado";
                        esPagadoBooleano = true;
                    }
                    else if (totalPagado > 0) // If some payment was made but not fully paid
                    {
                        estadoTexto = "Parcialmente Pagado";
                        esPagadoBooleano = false;
                    }
                    else
                    {
                        estadoTexto = "Pendiente";
                        esPagadoBooleano = false;
                    }

                    // Add the month's status to the list
                    monthlyStatuses.Add(new PaymentMonthStatusDto
                    {
                        Mes = month,
                        EsPagado = esPagadoBooleano,
                        CantidadPagada = totalPagado, // The amount recorded in PagoMembresia for this month
                        PrecioMensualMembresia = monthlyPrice, // The base monthly price of the membership
                        TotalAjustes = totalAjustes, // Sum of all charges/credits applied to this month
                        MontoAdeudado = montoFinalAdeudado, // The actual outstanding amount for this month
                        MetodoPago = paymentForMonth?.MetodoPago ?? "N/A",
                        FechaDePago = paymentForMonth?.FechaDePago,
                        EsAjusteManual = paymentForMonth?.EsAjusteManual ?? false,
                        EstadoTexto = estadoTexto, // Human-readable status for UI
                                                   // DetallePago field (from previous example) would typically be constructed on the frontend
                                                   // based on MontoAdeudado and CantidadPagada, or if you prefer to send it from here:
                                                   // DetallePago = $"Pagado: {totalPagado:C}, Adeuda: {montoFinalAdeudado:C}" // Example
                    });
                }

                return monthlyStatuses;
        }


        [HttpPost("membresias/{membresiaId}/pagar")]
        public async Task<IActionResult> RecordMembershipPayment(Guid membresiaId, [FromBody] PagoMembresiaDto model)
        {
            var membership = await _context.Membresias.FindAsync(membresiaId);
            if (membership == null)
            {
                return NotFound(new { message = "Membresía no encontrada." });
            }

            // Validate DTO for required fields
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var paymentMonthUtc = DateTime.SpecifyKind(new DateTime(model.MesDePago.Year, model.MesDePago.Month, 1), DateTimeKind.Utc);

            // Check if payment for this month already exists
            var existingPayment = await _context.PagosMembresia
                .AnyAsync(p => p.MembresiaId == membresiaId && p.MesDePago == paymentMonthUtc && p.EsPagado);

            if (existingPayment)
            {
                return Conflict(new { message = $"El pago para el mes {model.MesDePago.ToString("yyyy-MM")} ya ha sido registrado." });
            }

            var newPayment = new PagoMembresia
            {
                MembresiaId = membresiaId,
                MesDePago = paymentMonthUtc,
                CantidadPagada = model.CantidadPagada,
                FechaDePago = DateTime.UtcNow,
                EsPagado = true,
                MetodoPago = model.MetodoPago
            };

            _context.PagosMembresia.Add(newPayment);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Pago registrado para membresía {membresiaId} para el mes {model.MesDePago.ToString("yyyy-MM")}.");
            return Ok(new { message = "Pago registrado exitosamente.", payment = newPayment });
        }
        [HttpPost("membresias/{membresiaId}/ajustar-deuda")]
        [Authorize(Roles = "Administrador")] // Only admins can adjust debt
        public async Task<IActionResult> AdjustMembershipDebt(Guid membresiaId, [FromBody] DebtAdjustmentDto model)
        {
            if (!model.IsAnyAdjustmentProvided)
            {
                return BadRequest(new { message = "Se debe proporcionar un valor para 'Monto Esperado Nuevo' o 'Monto Pagado Nuevo'." });
            }

            var membresia = await _context.Membresias
                .Include(m => m.TipoMembresia)
                .FirstOrDefaultAsync(m => m.Id == membresiaId);

            if (membresia == null)
            {
                return NotFound(new { message = "Membresía no encontrada." });
            }

            //DateTime mesAjustadoUtc = DateTime.SpecifyKind(new DateTime(model.MesAjustado.Year, model.MesAjustado.Month, 1), DateTimeKind.Utc);
            DateTime mesAjustadoUtc = DateTime.SpecifyKind(new DateTime(model.MesAjustado.Value.Year, model.MesAjustado.Value.Month, 1), DateTimeKind.Utc);

            // ---Handle MontoEsperadoNuevo(Adjusting "What's Owed")-- -
            if (model.MontoEsperadoNuevo.HasValue)
            {
                // First, remove any previous "base adjustment" for this month
                // We want to replace the effect of a previous fixed amount adjustment.
                var existingBaseAdjustment = await _context.AjustesDeudaManual
                    .FirstOrDefaultAsync(a => a.MembresiaId == membresiaId &&
                                             a.MesAplicado == mesAjustadoUtc &&
                                             a.TipoAjusteInterno == "Monto Fijo Manual"); // Identify previous manual override

                // Calculate the current "effective base amount" before this adjustment
                // This is the original price + any other existing (non-"Monto Fijo Manual") adjustments
                decimal currentEffectiveBaseAmount = membresia.TipoMembresia.PrecioMensual;
                var otherAdjustments = await _context.AjustesDeudaManual
                    .Where(a => a.MembresiaId == membresiaId &&
                                a.MesAplicado == mesAjustadoUtc &&
                                a.TipoAjusteInterno != "Monto Fijo Manual") // Exclude the one we might be replacing
                    .ToListAsync();
                currentEffectiveBaseAmount += otherAdjustments.Sum(a => a.CantidadAjustada);


                // Calculate the adjustment needed to reach the MontoEsperadoNuevo
                decimal requiredAdjustment = model.MontoEsperadoNuevo.Value - currentEffectiveBaseAmount;

                if (existingBaseAdjustment != null)
                {
                    // Update existing adjustment
                    existingBaseAdjustment.CantidadAjustada = requiredAdjustment;
                    existingBaseAdjustment.Motivo = $"Ajuste a monto esperado: {model.Motivo}";
                    existingBaseAdjustment.FechaDeRegistro = DateTime.UtcNow;
                    existingBaseAdjustment.RegistradoPor = User.Identity?.Name;
                }
                else if (requiredAdjustment != 0) // Only add if there's an actual adjustment needed
                {
                    // Create a new adjustment to make the total expected amount match
                    var newFixedAmountAdjustment = new AjusteDeudaManual
                    {
                        Id = Guid.NewGuid(),
                        MembresiaId = membresia.Id,
                        Membresia = membresia,
                        MesAplicado = mesAjustadoUtc,
                        CantidadAjustada = requiredAdjustment,
                        TipoAjusteInterno = "Monto Fijo Manual", // Indicate it's a manual override adjustment
                        Motivo = $"Ajuste a monto esperado: {model.Motivo}",
                        FechaDeRegistro = DateTime.UtcNow,
                        RegistradoPor = User.Identity?.Name
                    };
                    _context.AjustesDeudaManual.Add(newFixedAmountAdjustment);
                }
                _logger.LogInformation($"Monto esperado para membresía {membresia.Id} en {mesAjustadoUtc:yyyy-MM} ajustado a {model.MontoEsperadoNuevo.Value:C}.");
            }


            //---Handle MontoPagadoNuevo(Adjusting "What's Paid")-- -
            if (model.MontoPagadoNuevo.HasValue)
            {
                var existingPagoMembresia = await _context.PagosMembresia
                    .FirstOrDefaultAsync(p => p.MembresiaId == membresiaId && p.MesDePago == mesAjustadoUtc);

                if (existingPagoMembresia == null)
                {
                    // Create a new PagoMembresia record if no payment existed for this month
                    existingPagoMembresia = new PagoMembresia
                    {
                        Id = Guid.NewGuid(),
                        MembresiaId = membresia.Id,
                        Membresia = membresia,
                        MesDePago = mesAjustadoUtc,
                        FechaDePago = DateTime.UtcNow,
                        MetodoPago = $"Ajuste Manual",
                        EsAjusteManual = true,
                        Motivo = model.Motivo
                    };
                    _context.PagosMembresia.Add(existingPagoMembresia);
                }

                existingPagoMembresia.CantidadPagada = model.MontoPagadoNuevo.Value;
                existingPagoMembresia.FechaDePago = DateTime.UtcNow; // Update adjustment date
                existingPagoMembresia.Motivo = $"Ajuste de pago: {model.Motivo}";
                existingPagoMembresia.EsPagado = (existingPagoMembresia.CantidadPagada >= (membresia.TipoMembresia.PrecioMensual + (_context.AjustesDeudaManual
                    .Where(a => a.MembresiaId == membresiaId && a.MesAplicado == mesAjustadoUtc)
                    .Sum(a => a.CantidadAjustada)))); // Recalculate EsPagado based on *total* expected amount

                _logger.LogInformation($"Monto pagado para membresía {membresia.Id} en {mesAjustadoUtc:yyyy-MM} ajustado a {model.MontoPagadoNuevo.Value:C}.");
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Ajuste de deuda realizado exitosamente." });
        }
        [HttpGet("clientes/{userId}/membresia/pagos/historial")] // New endpoint for full history
        [Authorize(Roles = "Administrador")] // Only admins can view full payment history
        public async Task<IActionResult> GetClientMembershipPaymentHistory(Guid userId)
        {
            var user = await _context.Users
                 .OfType<Cliente>() // Cast to Cliente to access Membresias
                 .Include(u => u.Membresias)
                     .ThenInclude(m => m.TipoMembresia)
                 .Include(u => u.Membresias)
                     .ThenInclude(m => m.PagosMembresia) // Eager load Payments
                 .Include(u => u.Membresias)
                     .ThenInclude(m => m.AjustesDeudaManual) // Eager load Adjustments
                 .FirstOrDefaultAsync(u => u.Id == userId); // userId from route is Guid, but IdentityUser.Id is string
                                                                       // Ensure conversion if your UsuarioIdentidad.Id is string

            if (user == null)
            {
                return NotFound(new { message = "Cliente no encontrado." });
            }

            // Dictionary to aggregate payment statuses by month (DateTime as key)
            var paymentMonthsAggregate = new Dictionary<DateTime, PaymentMonthStatusDto>();

            // Determine the overall earliest start date among all THIS client's memberships
            DateTime overallEarliestStartDateForClient = DateTime.UtcNow; // Default to now
            if (user.Membresias.Any())
            {
                overallEarliestStartDateForClient = user.Membresias.Min(m => m.FechaComienzo);
            }
            DateTime clientMembershipPeriodStartUtc = DateTime.SpecifyKind(
                new DateTime(overallEarliestStartDateForClient.Year, overallEarliestStartDateForClient.Month, 1), DateTimeKind.Utc);

            DateTime currentMonthUtc = DateTime.SpecifyKind(
                new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1), DateTimeKind.Utc);

            // Iterate through all memberships of THIS client
            foreach (var membership in user.Membresias)
            {
                decimal monthlyPriceForMembership = membership.TipoMembresia?.PrecioMensual ?? 0;

                // Define the loop's end date for THIS specific membership
                DateTime loopEndDateForMembership = (membership.FechaFinalizacion.HasValue && membership.FechaFinalizacion.Value.Date < currentMonthUtc.Date)
                    ? DateTime.SpecifyKind(new DateTime(membership.FechaFinalizacion.Value.Year, membership.FechaFinalizacion.Value.Month, 1), DateTimeKind.Utc)
                    : currentMonthUtc;

                // Loop from the start of THIS membership (normalized to month start) up to its end or current month
                for (DateTime month = DateTime.SpecifyKind(new DateTime(membership.FechaComienzo.Year, membership.FechaComienzo.Month, 1), DateTimeKind.Utc);
                     month <= loopEndDateForMembership;
                     month = month.AddMonths(1))
                {
                    DateTime monthKey = month; // Month is already normalized to first day, UTC

                    // Get existing entry or create a new one for this month in the aggregate dictionary
                    if (!paymentMonthsAggregate.TryGetValue(monthKey, out PaymentMonthStatusDto currentMonthStatus))
                    {
                        currentMonthStatus = new PaymentMonthStatusDto
                        {
                            Mes = monthKey,
                            EsPagado = false,
                            CantidadPagada = 0,
                            PrecioMensualMembresia = 0, // This will accumulate base price
                            TotalAjustes = 0,
                            MontoAdeudado = 0,
                            MetodoPago = "N/A",
                            FechaDePago = null,
                            EsAjusteManual = false,
                            EstadoTexto = "Pendiente",
                            MembresiaId = membership.Id // Associate with the current membership's ID
                        };
                        paymentMonthsAggregate[monthKey] = currentMonthStatus;
                    }

                    // 1. Accumulate expected price from *this* membership for *this* month
                    currentMonthStatus.PrecioMensualMembresia += monthlyPriceForMembership;

                    // 2. Aggregate payments for this month from THIS membership
                    var paymentsForMonth = membership.PagosMembresia
                        .Where(p => p.MesDePago.Year == month.Year && p.MesDePago.Month == month.Month)
                        .ToList();

                    // Accumulate the paid amount
                    currentMonthStatus.CantidadPagada += paymentsForMonth.Sum(p => p.CantidadPagada);

                    if (paymentsForMonth.Any())
                    {
                        // Update payment details for the aggregated month (e.g., latest date, method)
                        currentMonthStatus.FechaDePago = currentMonthStatus.FechaDePago.HasValue ?
                            (paymentsForMonth.Max(p => p.FechaDePago) > currentMonthStatus.FechaDePago.Value ? paymentsForMonth.Max(p => p.FechaDePago) : currentMonthStatus.FechaDePago) :
                            paymentsForMonth.Max(p => p.FechaDePago);

                        // You might need a more sophisticated way to represent MetodoPago if multiple payments exist for a month
                        // For simplicity, we take the last payment's method
                        currentMonthStatus.MetodoPago = paymentsForMonth.LastOrDefault()?.MetodoPago ?? currentMonthStatus.MetodoPago;

                        // If any payment for this month was a manual adjustment, flag it
                        if (paymentsForMonth.Any(p => p.EsAjusteManual))
                        {
                            currentMonthStatus.EsAjusteManual = true;
                        }
                    }

                    // 3. Aggregate adjustments for this month from THIS membership
                    var adjustmentsForMonth = membership.AjustesDeudaManual
                        .Where(a => a.MesAplicado.Year == month.Year && a.MesAplicado.Month == month.Month)
                        .ToList();

                    // Accumulate total adjustments (can be positive or negative)
                    currentMonthStatus.TotalAjustes += adjustmentsForMonth.Sum(a => a.CantidadAjustada);

                    // If any manual adjustment exists for this month, flag it
                    if (adjustmentsForMonth.Any())
                    {
                        currentMonthStatus.EsAjusteManual = true;
                    }
                }
            }

            // Now, iterate through the aggregated paymentMonths to finalize statuses and debt
            var finalPaymentMonths = new List<PaymentMonthStatusDto>();
            foreach (var monthStatus in paymentMonthsAggregate.Values.OrderBy(d => d.Mes)) // Order chronologically
            {
                // Recalculate montoFinalAdeudado and EsPagado based on accumulated values
                monthStatus.MontoAdeudado = monthStatus.PrecioMensualMembresia + monthStatus.TotalAjustes - monthStatus.CantidadPagada;
                monthStatus.EsPagado = monthStatus.MontoAdeudado <= 0; // True if debt is zero or negative (overpaid)

                // Determine descriptive status
                if (monthStatus.EsPagado)
                {
                    monthStatus.EstadoTexto = "Pagado";
                }
                else if (monthStatus.CantidadPagada > 0 || monthStatus.TotalAjustes != 0)
                { // If some payment or adjustment was made, but not fully paid
                    monthStatus.EstadoTexto = "Parcialmente Pagado";
                }
                else
                {
                    monthStatus.EstadoTexto = "Pendiente"; // No payment or adjustment, still owes full amount
                }
                finalPaymentMonths.Add(monthStatus);
            }

            // Determine the "current" membership details for the overall DTO.
            // This logic might need adjustment if a client can have multiple "active" memberships simultaneously
            // and you need a specific one to represent the "current" state in the header.
            var displayMembership = user.Membresias
                .OrderByDescending(m => m.FechaComienzo) // Pick the most recent membership
                .FirstOrDefault(); // Or add more specific criteria like m.EstaActiva

            return Ok(new PaymentHistoryDto
            {
                MembresiaId = displayMembership?.Id,
                TipoMembresia = displayMembership?.TipoMembresia?.Nombre ?? "Sin Membresía Asignada",
                // The PrecioMensualMembresia in PaymentHistoryDto might now represent the base price of the displayMembership
                PrecioMensualMembresia = displayMembership?.TipoMembresia?.PrecioMensual ?? 0,
                PaymentMonths = finalPaymentMonths,
                Message = "Historial de pagos cargado exitosamente."
            });
        }
        [HttpGet("clientes-con-historial-pagos")] 
        [Authorize(Roles = "Administrador")] // Add authorization as needed
        public async Task<IActionResult> GetClientsWithPaymentHistoryOverview()
        {
            try
            {
                var clients = await _context.Users
                 .OfType<Cliente>()
                 .Include(c => c.Membresias)
                     .ThenInclude(m => m.TipoMembresia)
                 .Include(c => c.Membresias)
                     .ThenInclude(m => m.PagosMembresia)
                 .Include(c => c.Membresias)
                     .ThenInclude(m => m.AjustesDeudaManual)
                 .ToListAsync();


                var clientOverviews = new List<BackendClientResponseForHistoryDto>();
                DateTime currentMonthUtc = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                foreach (var client in clients)
                {
                    var paymentStatusList = new List<PaymentMonthStatusDto>();

                    foreach (var membership in client.Membresias)
                    {
                        foreach (var pago in membership.PagosMembresia)
                        {
                            var pagoMonth = new DateTime(pago.MesDePago.Year, pago.MesDePago.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                            var ajustes = membership.AjustesDeudaManual
                                .Where(a => a.MesAplicado.Year == pago.MesDePago.Year && a.MesAplicado.Month == pago.MesDePago.Month)
                                .ToList();

                            var totalAjustes = ajustes.Sum(a => a.CantidadAjustada);

                            var precioMembresia = membership.TipoMembresia?.PrecioMensual ?? 0;

                            var montoAdeudado = precioMembresia + totalAjustes - pago.CantidadPagada;

                            var statusDto = new PaymentMonthStatusDto
                            {
                                Mes = pagoMonth,
                                CantidadPagada = pago.CantidadPagada,
                                PrecioMensualMembresia = precioMembresia,
                                TotalAjustes = totalAjustes,
                                MontoAdeudado = montoAdeudado,
                                MetodoPago = pago.MetodoPago ?? "N/A",
                                FechaDePago = pago.FechaDePago,
                                EsAjusteManual = pago.EsAjusteManual,
                                MembresiaId = membership.Id,
                                EsPagado = montoAdeudado <= 0,
                                EstadoTexto = montoAdeudado <= 0
                                    ? "Pagado"
                                    : (pago.CantidadPagada > 0 || totalAjustes != 0
                                        ? "Parcialmente Pagado"
                                        : "Pendiente")
                            };

                            paymentStatusList.Add(statusDto);
                        }
                    }

                    var displayMembership = client.Membresias
                        .OrderByDescending(m => m.FechaComienzo)
                        .FirstOrDefault(m => m.EstaActiva && (!m.FechaFinalizacion.HasValue || m.FechaFinalizacion.Value >= DateTime.UtcNow));

                    clientOverviews.Add(new BackendClientResponseForHistoryDto
                    {
                        Id = client.Id,
                        Nombre = client.Nombre,
                        Apellido = client.Apellido,
                        Email = client.Email,
                        TipoMembresia = displayMembership?.TipoMembresia?.Nombre ?? "Sin Membresía Asignada",
                        EstadoMembresia = displayMembership?.EstaActiva ?? false,
                        NombreUsuario = client.UserName,
                        CurrentMembresiaId = displayMembership?.Id,
                        PaymentHistory = paymentStatusList.OrderBy(p => p.Mes).ToList()
                    });
                }


                return Ok(clientOverviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching clients with payment history for overview.");
                return StatusCode(500, "Error interno del servidor al obtener el resumen de pagos de los clientes.");
            }
        }
    }
}
