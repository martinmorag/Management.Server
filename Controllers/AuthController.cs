using Management.Server.Data;
using Management.Server.Models;
using Management.Server.Models.clientes;
using Management.Server.Models.ingreso;
using Management.Server.Models.membresias;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Plataforma.Models; 
using System.Linq;

namespace Management.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<UsuarioIdentidad> _userManager;
        private readonly SignInManager<UsuarioIdentidad> _signInManager;
        private readonly ILogger<AuthController> _logger;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly ManagementContext _context;

        public AuthController(
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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // To prevent user enumeration, return a generic error for both email and password issues
                _logger.LogWarning($"Login attempt failed for non-existent user: {model.Email}");
                return Unauthorized(new { message = "Email o contraseña inválidos." });
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, // Use the user object found by email
                model.Password,
                isPersistent: false,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                _logger.LogInformation($"User {model.Email} logged in.");

                // --- IMPORTANT: GET USER ROLES ---
                var roles = await _userManager.GetRolesAsync(user);
                string userRole = roles.FirstOrDefault(); // Get the first role (assuming one primary role per user for simplicity)
                if (userRole == null)
                {
                    userRole = "Cliente"; // Default to client if no specific role found or assigned
                }
                // --- END IMPORTANT ---

                return Ok(new { message = "Login successful", role = userRole }); // Include the role in the response
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning($"User {model.Email} account locked out.");
                return Unauthorized(new { message = "Cuenta bloqueada debido a múltiples intentos fallidos." });
            }
            if (result.IsNotAllowed)
            {
                _logger.LogWarning($"User {model.Email} is not allowed to sign in.");
                return Unauthorized(new { message = "No se permite iniciar sesión. Tu cuenta podría no estar confirmada." });
            }

            _logger.LogWarning($"Invalid login attempt for user {model.Email}.");
            return Unauthorized(new { message = "Email o contraseña inválidos." });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return Ok(new { message = "Logout successful" });
        }
        // CREAR CLIENTE
        [HttpGet("membresia-tipos")]
        public async Task<IActionResult> GetMembershipTypes()
        {
            var tipos = await _context.TiposMembresia
                                      .Where(mt => mt.EstaActiva)
                                      .Select(mt => new { mt.Id, mt.Nombre, mt.PrecioMensual, mt.DuracionMeses })
                                      .ToListAsync();
            return Ok(tipos);
        }
        [HttpGet("clientes")]
        public async Task<IActionResult> GetClients()
        {
            var clientRoleId = await _context.Roles
                .Where(r => r.Name == "Cliente")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (clientRoleId == null)
            {
                // Handle case where "Cliente" role does not exist
                return NotFound(new { message = "El rol 'Cliente' no se encontró en el sistema." });
            }

            var clients = await _context.Users
                .OfType<Cliente>()
                .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == clientRoleId))
                .Include(u => u.Membresias)
                    .ThenInclude(m => m.TipoMembresia)
                .Select(u => new ClientListDto
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Email = u.Email,
                    TipoMembresia = u.Membresias
                        .Where(m => m.EstaActiva && (!m.FechaFinalizacion.HasValue || m.FechaFinalizacion.Value >= DateTime.UtcNow))
                        .OrderByDescending(m => m.FechaComienzo)
                        .Select(m => m.TipoMembresia.Nombre)
                        .FirstOrDefault() ?? "Sin Membresía",

                    EstadoMembresia = u.Membresias
                        .Any(m => m.EstaActiva),

                    CurrentMembresiaId = u.Membresias
                        .Where(m => m.EstaActiva && (!m.FechaFinalizacion.HasValue || m.FechaFinalizacion.Value >= DateTime.UtcNow))
                        .OrderByDescending(m => m.FechaComienzo)
                        .Select(m => m.Id.ToString())
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(clients);
        }
        [HttpPost("registrar-cliente")]
        public async Task<IActionResult> RegisterClient([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userName = model.NombreUsuario; // Your current UserName logic
            var userWithSameUserName = await _userManager.FindByNameAsync(userName);
            if (userWithSameUserName != null)
            {
                _logger.LogWarning($"Registration attempt for existing UserName: {userName}");
                return Conflict(new { message = "Ya existe un usuario con ese nombre completo. Por favor, ajuste el nombre o contacte a soporte." });
            }

            var user = new Cliente
            {
                UserName = model.NombreUsuario,
                Email = model.Email,
                EmailConfirmed = false,
                Nombre = model.Nombre,
                Apellido = model.Apellido
            };

            var result = await _userManager.CreateAsync(user, model.Contrasena);

            if (result.Succeeded)
            {
                // Ensure "Client" role exists
                if (!await _roleManager.RoleExistsAsync("Cliente"))
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>("Cliente"));
                    _logger.LogInformation("Client role created as it did not exist.");
                }
                await _userManager.AddToRoleAsync(user, "Cliente");

                // --- NEW: Create Membership for the new client ---
                var membershipType = await _context.TiposMembresia.FindAsync(model.TipoMembresiaId);
                if (membershipType == null || !membershipType.EstaActiva)
                {
                    // Rollback user creation if membership type is invalid (complex, might need transaction)
                    // For simplicity, we'll return an error and might need manual cleanup or more robust logic
                    _logger.LogError($"MembershipType with ID {model.TipoMembresiaId} not found or not active during client registration.");
                    // Consider deleting the user created here if transaction is not used
                    return BadRequest(new { message = "Tipo de membresía inválido o inactivo." });
                }

                DateTime fechaComienzoUtc = DateTime.SpecifyKind(model.MembresiaFechaComienzo.Date, DateTimeKind.Utc);

                var newMembership = new Membresia
                {
                    Id = Guid.NewGuid(),
                    UsuarioId = user.Id,
                    TipoMembresiaId = model.TipoMembresiaId,
                    FechaComienzo = fechaComienzoUtc, // Use .Date to remove time component if not needed
                    EstaActiva = true, // Assume active upon creation
                    TipoMembresia = membershipType,
                    Usuario = user,
                    // If ActualPricePaid is provided in DTO, use it; otherwise, use the default from MembershipType
                    PrecioPagado = model.PrecioPagado ?? membershipType.PrecioMensual
                };

                // Calculate EndDate if duration is fixed
                if (membershipType.DuracionMeses.HasValue && membershipType.DuracionMeses.Value > 0)
                {
                    // Calculate the end date based on the *original* date, then set Kind to UTC
                    DateTime calculatedFechaFinalizacion = model.MembresiaFechaComienzo.Date.AddMonths(membershipType.DuracionMeses.Value);
                    newMembership.FechaFinalizacion = DateTime.SpecifyKind(calculatedFechaFinalizacion, DateTimeKind.Utc); // Use the UTC DateTime
                }
                else
                {
                    newMembership.FechaFinalizacion = null;
                }

                _context.Membresias.Add(newMembership);
                await _context.SaveChangesAsync();

                var initialPaymentAmount = model.PrecioPagado ?? membershipType.PrecioMensual;
                var initialPayment = new PagoMembresia
                {
                    Id = Guid.NewGuid(),
                    MembresiaId = newMembership.Id,
                    Membresia = newMembership,
                    MesDePago = DateTime.SpecifyKind(new DateTime(newMembership.FechaComienzo.Year, newMembership.FechaComienzo.Month, 1), DateTimeKind.Utc),
                    CantidadPagada = 0,
                    FechaDePago = DateTime.UtcNow,
                    MetodoPago = "Online Registration", // Or whatever fits your process
                    EsAjusteManual = false, // This is a real payment, not a manual adjustment
                    EsPagado = false
                };
                _context.PagosMembresia.Add(initialPayment);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"New client registered: {user.Email}, assigned 'Client' role, and membership created.");
                return Ok(new { message = "Cliente y membresía registrados exitosamente." });
            }

            // If user creation failed, return specific errors
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { message = "Error al registrar el cliente.", errors = errors });
        }
        // EDITAR CLIENTE
        [HttpGet("clientes/{id}")] // Route: /api/Auth/clientes/{id}
        public async Task<IActionResult> GetClientById(Guid id)
        {
            var user = await _userManager.Users
                                        .OfType<Cliente>()
                                         .Include(u => u.Membresias)
                                             .ThenInclude(m => m.TipoMembresia) // Eager load membership type
                                         .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new { message = "Cliente no encontrado." });
            }

            // Get the *most recent* or *active* membership for displaying in the form
            // You might need more sophisticated logic here if a user can have multiple overlapping memberships.
            var currentMembership = user.Membresias
                .Where(m => m.EstaActiva && (!m.FechaFinalizacion.HasValue || m.FechaFinalizacion.Value >= DateTime.UtcNow))
                .OrderByDescending(m => m.FechaComienzo)
                .FirstOrDefault();

            var clientDetails = new ClientDetailsDto
            {
                Id = user.Id,
                Email = user.Email,
                Nombre = user.Nombre,
                Apellido = user.Apellido,
                NombreUsuario = user.UserName, // Assuming UserName is stored

                // Populate membership details from the currentMembership
                TipoMembresiaIdActual = currentMembership?.TipoMembresiaId,
                MembresiaFechaComienzoActual = currentMembership?.FechaComienzo,
                PrecioPagadoActual = currentMembership?.PrecioPagado,
                EstaActivaMembresiaActual = currentMembership?.EstaActiva ?? false, // Default to false if no membership
                FechaFinalizacionActual = currentMembership?.FechaFinalizacion
            };

            return Ok(clientDetails);
        }
        [HttpPut("clientes/{id}")] // Route: /api/Auth/clientes/{id}
        public async Task<IActionResult> UpdateClient(Guid id, [FromBody] UpdateDto model)
        {
            if (id != model.Id)
            {
                return BadRequest(new { message = "ID de cliente no coincide." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByIdAsync(model.Id.ToString()); // FindByIdAsync expects string
            if (user == null)
            {
                return NotFound(new { message = "Cliente no encontrado para actualizar." });
            }

            // Update User properties
            user.Email = model.Email;
            user.Nombre = model.Nombre;
            user.Apellido = model.Apellido;
            user.UserName = model.NombreUsuario;
        
            // Update password if provided
            if (!string.IsNullOrEmpty(model.Contrasena))
            {
                // This is a simplified way to update password. In a real app, you might
                // use ChangePasswordAsync (if old password is provided) or RemovePasswordAsync then AddPasswordAsync.
                // For admin forced reset:
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, model.Contrasena);
                if (!resetResult.Succeeded)
                {
                    var errors = resetResult.Errors.Select(e => e.Description);
                    return BadRequest(new { message = "Error al actualizar contraseña.", errors = errors });
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(e => e.Description);
                return BadRequest(new { message = "Error al actualizar datos de cliente.", errors = errors });
            }

            // --- Update/Manage Membership ---
            // This is complex. A user might have multiple memberships, or you might need to end the old one
            // and start a new one, or just update the existing active one.
            // For simplicity here, let's assume we update the *latest active* membership or create a new one.

            //var currentActiveMembership = await _context.Membresias
            //    .Where(m => m.UsuarioId == user.Id && m.EstaActiva)
            //    .OrderByDescending(m => m.FechaComienzo)
            //    .FirstOrDefaultAsync();

            //var membershipType = await _context.TiposMembresia.FindAsync(model.TipoMembresiaId);
            //if (membershipType == null || !membershipType.EstaActiva)
            //{
            //    return BadRequest(new { message = "Tipo de membresía inválido o inactivo." });
            //}

            //if (currentActiveMembership != null && currentActiveMembership.TipoMembresiaId == model.TipoMembresiaId)
            //{
            //    currentActiveMembership.FechaComienzo = DateTime.SpecifyKind(model.MembresiaFechaComienzo.Date, DateTimeKind.Utc);
            //    currentActiveMembership.PrecioPagado = model.PrecioPagado ?? membershipType.PrecioMensual;

            //    if (membershipType.DuracionMeses.HasValue && membershipType.DuracionMeses.Value > 0)
            //    {
            //        currentActiveMembership.FechaFinalizacion = DateTime.SpecifyKind(model.MembresiaFechaComienzo.Date.AddMonths(membershipType.DuracionMeses.Value), DateTimeKind.Utc);
            //    }
            //    else
            //    {
            //        currentActiveMembership.FechaFinalizacion = null;
            //    }
            //    currentActiveMembership.EstaActiva = true; // Ensure it's active
            //    _context.Membresias.Update(currentActiveMembership); // Mark as updated
            //}
            //else
            //{
            //    // If there's no active membership, or the type changed, create a new one.
            //    // Optionally: set the old active membership to inactive before creating a new one
            //    if (currentActiveMembership != null)
            //    {
            //        currentActiveMembership.EstaActiva = false;
            //        _context.Membresias.Update(currentActiveMembership);
            //    }

            //    var newMembership = new Membresia
            //    {
            //        Id = Guid.NewGuid(), // Generate a new GUID for the new membership
            //        UsuarioId = user.Id,
            //        Usuario = user, // Assign the navigation property
            //        TipoMembresiaId = model.TipoMembresiaId,
            //        TipoMembresia = membershipType, // Assign the navigation property
            //        FechaComienzo = DateTime.SpecifyKind(model.MembresiaFechaComienzo.Date, DateTimeKind.Utc),
            //        EstaActiva = true,
            //        PrecioPagado = model.PrecioPagado ?? membershipType.PrecioMensual
            //    };

            //    if (membershipType.DuracionMeses.HasValue && membershipType.DuracionMeses.Value > 0)
            //    {
            //        newMembership.FechaFinalizacion = DateTime.SpecifyKind(model.MembresiaFechaComienzo.Date.AddMonths(membershipType.DuracionMeses.Value), DateTimeKind.Utc);
            //    }
            //    else
            //    {
            //        newMembership.FechaFinalizacion = null;
            //    }
            //    _context.Membresias.Add(newMembership);
            //}


            var currentActiveMembership = await _context.Membresias
        .Where(m => m.UsuarioId == user.Id && m.EstaActiva)
        .OrderByDescending(m => m.FechaComienzo) // Get the most recent active one
        .FirstOrDefaultAsync();

            var membershipType = await _context.TiposMembresia.FindAsync(model.TipoMembresiaId);
            if (membershipType == null || !membershipType.EstaActiva)
            {
                return BadRequest(new { message = "Tipo de membresía inválido o inactivo." });
            }

            // Normalize incoming date to UTC start of day for consistency
            var newMembresiaFechaComienzoUtc = DateTime.SpecifyKind(model.MembresiaFechaComienzo.Date, DateTimeKind.Utc);

            // Scenario 1: Existing active membership with the SAME type
            if (currentActiveMembership != null && currentActiveMembership.TipoMembresiaId == model.TipoMembresiaId)
            {
                // Only update if FechaComienzo or PrecioPagado has changed
                bool changed = false;
                if (currentActiveMembership.FechaComienzo != newMembresiaFechaComienzoUtc)
                {
                    currentActiveMembership.FechaComienzo = newMembresiaFechaComienzoUtc;
                    changed = true;
                }
                var effectivePrecioPagado = model.PrecioPagado ?? membershipType.PrecioMensual;
                if (currentActiveMembership.PrecioPagado != effectivePrecioPagado)
                {
                    currentActiveMembership.PrecioPagado = effectivePrecioPagado;
                    changed = true;
                }

                // Always recalculate FechaFinalizacion if duration is defined
                if (membershipType.DuracionMeses.HasValue && membershipType.DuracionMeses.Value > 0)
                {
                    var newFechaFinalizacion = DateTime.SpecifyKind(newMembresiaFechaComienzoUtc.AddMonths(membershipType.DuracionMeses.Value), DateTimeKind.Utc);
                    if (currentActiveMembership.FechaFinalizacion != newFechaFinalizacion)
                    {
                        currentActiveMembership.FechaFinalizacion = newFechaFinalizacion;
                        changed = true;
                    }
                }
                else // Perpetual
                {
                    if (currentActiveMembership.FechaFinalizacion.HasValue)
                    {
                        currentActiveMembership.FechaFinalizacion = null;
                        changed = true;
                    }
                }
                currentActiveMembership.EstaActiva = true; // Ensure it remains active

                if (changed)
                {
                    _context.Membresias.Update(currentActiveMembership);
                    // NO automatic initial payment creation here.
                    // If FechaComienzo changed, existing payment history might need manual adjustment by admin
                    // or will be picked up by the next scheduled debt generation run.
                }
            }
            // Scenario 2: No active membership, or membership type has changed
            else
            {
                // Invalidate old active membership if a new one is being created
                if (currentActiveMembership != null)
                {
                    currentActiveMembership.EstaActiva = false;
                    currentActiveMembership.FechaFinalizacion = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc); // End it today
                    _context.Membresias.Update(currentActiveMembership);
                }

                var newMembership = new Membresia
                {
                    Id = Guid.NewGuid(),
                    UsuarioId = user.Id,
                    TipoMembresiaId = model.TipoMembresiaId,
                    FechaComienzo = newMembresiaFechaComienzoUtc,
                    EstaActiva = true,
                    PrecioPagado = model.PrecioPagado ?? membershipType.PrecioMensual
                };

                if (membershipType.DuracionMeses.HasValue && membershipType.DuracionMeses.Value > 0)
                {
                    newMembership.FechaFinalizacion = DateTime.SpecifyKind(newMembresiaFechaComienzoUtc.AddMonths(membershipType.DuracionMeses.Value), DateTimeKind.Utc);
                }
                else
                {
                    newMembership.FechaFinalizacion = null;
                }
                _context.Membresias.Add(newMembership);

                // --- IMPORTANT: Decide if you want an initial payment when a *new* membership is activated via edit ---
                // Option A: Let the DebtGenerationService handle it (it will create a "pending" record for the first month if no payment exists).
                //           This is generally preferred as it's consistent with new registrations.
                // Option B: Automatically create an "initial payment" record for the first month if admin says it's paid.
                //           This mimics the registration payment.
                //           Choose Option B only if admin actions explicitly mean an upfront payment for the new membership.

                // OPTION B (if you need it):
                // if (newMembership.PrecioPagado > 0) // Or some other condition indicating an initial payment
                // {
                //     var initialPayment = new PagoMembresia
                //     {
                //         Id = Guid.NewGuid(),
                //         MembresiaId = newMembership.Id,
                //         MesDePago = DateTime.SpecifyKind(new DateTime(newMembership.FechaComienzo.Year, newMembership.FechaComienzo.Month, 1), DateTimeKind.Utc),
                //         CantidadPagada = newMembership.PrecioPagado.Value,
                //         FechaDePago = DateTime.UtcNow,
                //         MetodoPago = "Manual Admin Adjustment (New Membership)",
                //         Notas = "Pago inicial al crear nueva membresía mediante edición de cliente.",
                //         EsAjusteManual = true // This is a manager action
                //     };
                //     _context.PagosMembresia.Add(initialPayment);
                // }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Cliente {user.Email} actualizado exitosamente.");
            return Ok(new { message = "Cliente actualizado exitosamente." });
        }
        // ELIMINAR CLIENTE
        [HttpDelete("clientes/{id}")]
        public async Task<IActionResult> DeleteClient(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user == null)
            {
                return NotFound(new { message = "Cliente no encontrado para eliminar." });
            }

            //Optional: If you have complex relationships and not using cascade delete,
            //you might need to manually delete associated memberships here first:
            var memberships = await _context.Membresias.Where(m => m.UsuarioId == id).ToListAsync();
            _context.Membresias.RemoveRange(memberships);

            var result = await _userManager.DeleteAsync(user);
            await _context.SaveChangesAsync();

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                _logger.LogError($"Error al eliminar cliente {user.Email}: {string.Join(", ", errors)}");
                return BadRequest(new { message = "Error al eliminar el cliente.", errors = errors });
            }

            _logger.LogInformation($"Cliente {user.Email} eliminado exitosamente.");
            return Ok(new { message = "Cliente eliminado exitosamente." });
        }
        //CREAR TIPO DE MEMBRESIA
        [HttpPost("tiposmembresia")]
        public async Task<IActionResult> CreateTipoMembresia([FromBody] CreateMembresiaDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check for duplicate name
            if (await _context.TiposMembresia.AnyAsync(tm => tm.Nombre == model.Nombre))
            {
                return Conflict(new { message = $"Ya existe un tipo de membresía con el nombre '{model.Nombre}'." });
            }

            var newTipoMembresia = new TipoMembresia
            {
                Id = Guid.NewGuid(),
                Nombre = model.Nombre,
                PrecioMensual = model.PrecioMensual,
                DuracionMeses = model.DuracionMeses,
                EstaActiva = model.EstaActiva
            };

            _context.TiposMembresia.Add(newTipoMembresia);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Nuevo tipo de membresía creado: {newTipoMembresia.Nombre}");
            return CreatedAtAction(nameof(GetMembershipTypes), new { id = newTipoMembresia.Id }, new { message = "Tipo de membresía creado con éxito.", tipoMembresia = newTipoMembresia });
        }
        // EDITAR TIPOS DE MEMBRESIA
        [HttpGet("tiposmembresia/{id}")]
        public async Task<IActionResult> GetTipoMembresiaById(Guid id)
        {
            var tipoMembresia = await _context.TiposMembresia.FindAsync(id);

            if (tipoMembresia == null)
            {
                return NotFound(new { message = "Tipo de membresía no encontrado." });
            }

            var tipoMembresiaDto = new CreateMembresiaDto // Using CreateTipoMembresiaDto for simplicity, or create a specific Details DTO
            {
                Nombre = tipoMembresia.Nombre,
                PrecioMensual = tipoMembresia.PrecioMensual,
                DuracionMeses = tipoMembresia.DuracionMeses,
                EstaActiva = tipoMembresia.EstaActiva
            };

            return Ok(tipoMembresiaDto);
        }
        [HttpPut("tiposmembresia/{id}")]
        public async Task<IActionResult> UpdateTipoMembresia(Guid id, [FromBody] UpdateMembresiaDto model)
        {
            if (id != model.Id)
            {
                return BadRequest(new { message = "ID de tipo de membresía no coincide." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tipoMembresia = await _context.TiposMembresia.FindAsync(model.Id);
            if (tipoMembresia == null)
            {
                return NotFound(new { message = "Tipo de membresía no encontrado para actualizar." });
            }

            // Check for duplicate name, excluding the current type being updated
            if (await _context.TiposMembresia.AnyAsync(tm => tm.Nombre == model.Nombre && tm.Id != model.Id))
            {
                return Conflict(new { message = $"Ya existe otro tipo de membresía con el nombre '{model.Nombre}'." });
            }

            tipoMembresia.Nombre = model.Nombre;
            tipoMembresia.PrecioMensual = model.PrecioMensual;
            tipoMembresia.DuracionMeses = model.DuracionMeses;
            tipoMembresia.EstaActiva = model.EstaActiva;

            _context.TiposMembresia.Update(tipoMembresia);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Tipo de membresía {tipoMembresia.Nombre} ({tipoMembresia.Id}) actualizado exitosamente.");
            return Ok(new { message = "Tipo de membresía actualizado exitosamente." });
        }
        // ELIMINAR TIPO DE MEMBRESIA
        [HttpDelete("tiposmembresia/{id}")] // Route: /api/Auth/tiposmembresia/{id}
        public async Task<IActionResult> DeleteTipoMembresia(Guid id)
        {
            var tipoMembresia = await _context.TiposMembresia.FindAsync(id);

            if (tipoMembresia == null)
            {
                return NotFound(new { message = "Tipo de membresía no encontrado para eliminar." });
            }

            var hasActiveClients = await _context.Membresias.AnyAsync(m =>
            m.TipoMembresiaId == id &&
            (m.FechaFinalizacion == null || m.FechaFinalizacion > DateTime.UtcNow));
            if (hasActiveClients)
            {
                return BadRequest(new { message = "No se puede eliminar este tipo de membresía porque hay clientes activos asociados a ella. Desactive la membresía en su lugar o reasigne los clientes." });
            }


            _context.TiposMembresia.Remove(tipoMembresia);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Tipo de membresía '{tipoMembresia.Nombre}' ({tipoMembresia.Id}) eliminado exitosamente.");
            return Ok(new { message = "Tipo de membresía eliminado exitosamente." });
        }
    }
}