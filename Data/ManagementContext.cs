using Management.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Plataforma.Models;

namespace Management.Server.Data
{
    public class ManagementContext : IdentityDbContext<UsuarioIdentidad, IdentityRole<Guid>, Guid>
    {
        public ManagementContext(DbContextOptions<ManagementContext> options)
            : base(options)
        {
        }

        public DbSet<Membresia> Membresias { get; set; }
        public DbSet<TipoMembresia> TiposMembresia { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<PagoMembresia> PagosMembresia { get; set; }
        public DbSet<AjusteDeudaManual> AjustesDeudaManual { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UsuarioIdentidad>()
            .HasDiscriminator<string>("Discriminator")
            .HasValue<UsuarioIdentidad>("UsuarioIdentidad")
            .HasValue<Administrador>("Administrador")
            .HasValue<Cliente>("Cliente");

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                            // Convert to UTC before saving (handle Unspecified kind by setting to UTC first)
                            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                            // When reading, specify it's UTC so EF knows its kind
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));

                        // If you also have properties that could be DateTimeOffset,
                        // Npgsql usually handles them correctly as 'timestamp with time zone' by default.
                        // For pure DateTime/DateTime?, this converter is key.
                    }
                }
            }

            builder.Entity<Membresia>()
                .HasMany(m => m.PagosGenerales)
                .WithOne(p => p.Membresia)
                .HasForeignKey(p => p.MembresiaId);
            builder.Entity<PagoMembresia>()
                .HasIndex(p => new { p.MembresiaId, p.MesDePago })
                .IsUnique();
        }
    }
}
