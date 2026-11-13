using EuforiaEssencePW.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EuforiaEssencePW.Data
{
    public class ApplicationDbContext : IdentityDbContext<Usuario>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Perfume> Perfumes { get; set; }
        public DbSet<Presentacion> Presentaciones { get; set; }
        public DbSet<Direccion> Direcciones { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallesPedido { get; set; }
        public DbSet<RegistroAuditoria> RegistrosAuditoria { get; set; }
        public DbSet<Campania> Campanias { get; set; }
        public DbSet<CampaniaPerfume> CampaniasPerfumes { get; set; }
        public DbSet<IntentoPago> IntentosPago { get; set; }
        public DbSet<ComprobantePago> ComprobantesPago { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<ComprobantePago>()
    .Property(c => c.Id)
    .ValueGeneratedNever();

            builder.Entity<ComprobantePago>()
                .HasOne(c => c.Pedido)
                .WithMany()
                .HasForeignKey(c => c.PedidoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Solo puede haber un comprobante pendiente por pedido.
            builder.Entity<ComprobantePago>()
                .HasIndex(c => c.PedidoId)
                .IsUnique()
                .HasFilter("[Estado] = N'Pendiente'");
            builder.Entity<Marca>()
                .HasIndex(m => m.Nombre)
                .IsUnique();

            builder.Entity<Categoria>()
                .HasIndex(c => c.Nombre)
                .IsUnique();

            builder.Entity<Presentacion>()
                .HasIndex(p => new
                {
                    p.PerfumeId,
                    p.Mililitros
                })
                .IsUnique();
            builder.Entity<Campania>()
    .Property(c => c.Desde)
    .HasColumnType("date");

            builder.Entity<Campania>()
                .Property(c => c.Hasta)
                .HasColumnType("date");
            builder.Entity<IntentoPago>()
    .Property(p => p.Id)
    .ValueGeneratedNever();

            builder.Entity<IntentoPago>()
                .HasOne(p => p.Pedido)
                .WithMany()
                .HasForeignKey(p => p.PedidoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<IntentoPago>()
                .HasIndex(p => new { p.PedidoId, p.Fecha });
            builder.Entity<Campania>()
                .HasIndex(c => new { c.Activa, c.Desde, c.Hasta });

            builder.Entity<CampaniaPerfume>()
                .HasKey(cp => new { cp.CampaniaId, cp.PerfumeId });

            builder.Entity<CampaniaPerfume>()
                .HasOne(cp => cp.Campania)
                .WithMany(c => c.Productos)
                .HasForeignKey(cp => cp.CampaniaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CampaniaPerfume>()
                .HasOne(cp => cp.Perfume)
                .WithMany()
                .HasForeignKey(cp => cp.PerfumeId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Presentacion>()
                .Property(p => p.Precio)
                .HasPrecision(18, 2);

            builder.Entity<Perfume>()
                .HasOne(p => p.Marca)
                .WithMany(m => m.Perfumes)
                .HasForeignKey(p => p.MarcaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Perfume>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Perfumes)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Pedido>()
    .HasIndex(p => p.TokenConfirmacion)
    .IsUnique()
    .HasFilter("[TokenConfirmacion] IS NOT NULL");

            builder.Entity<Presentacion>()
                .HasOne(p => p.Perfume)
                .WithMany(p => p.Presentaciones)
                .HasForeignKey(p => p.PerfumeId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Direccion>()
    .HasOne(d => d.Usuario)
    .WithMany()
    .HasForeignKey(d => d.UsuarioId)
    .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Pedido>()
                .HasOne(p => p.Usuario)
                .WithMany()
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Pedido>()
                .HasOne(p => p.Direccion)
                .WithMany(d => d.Pedidos)
                .HasForeignKey(p => p.DireccionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DetallePedido>()
                .HasOne(d => d.Pedido)
                .WithMany(p => p.Detalles)
                .HasForeignKey(d => d.PedidoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DetallePedido>()
                .HasOne(d => d.Presentacion)
                .WithMany()
                .HasForeignKey(d => d.PresentacionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DetallePedido>()
                .Property(d => d.PrecioUnitario)
                .HasPrecision(18, 2);
            builder.Entity<RegistroAuditoria>()
    .HasIndex(a => a.Fecha);

            builder.Entity<RegistroAuditoria>()
                .HasIndex(a => new { a.Entidad, a.RegistroId });    

            builder.Entity<DetallePedido>()
                .HasIndex(d => new
                {
                    d.PedidoId,
                    d.PresentacionId
                })
                .IsUnique();
        }

        public override async Task<int> SaveChangesAsync(
    CancellationToken cancellationToken = default)
        {
            ChangeTracker.DetectChanges();

            var usuario = _httpContextAccessor.HttpContext?.User;

            var usuarioId = usuario?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            var usuarioNombre = usuario?.Identity?.Name ?? "Sistema";

            var fecha = DateTime.UtcNow;

            var entradas = ChangeTracker
                .Entries<AuditableEntity>()
                .Where(e =>
                    e.State == EntityState.Added ||
                    e.State == EntityState.Modified ||
                    e.State == EntityState.Deleted)
                .ToList();

            var pendientes = new List<(
                EntityEntry Entrada,
                RegistroAuditoria Registro)>();

            foreach (var entrada in entradas)
            {
                // Para editar y eliminar, recuperamos los valores reales
                // que estaban almacenados en SQL Server.
                var anteriores = entrada.State == EntityState.Added
                    ? null
                    : await entrada.GetDatabaseValuesAsync(cancellationToken);

                if (entrada.State == EntityState.Added)
                {
                    entrada.Entity.FechaCreacion = fecha;
                    entrada.Entity.UsuarioCreacionId = usuarioId;
                    entrada.Entity.FechaModificacion = null;
                    entrada.Entity.UsuarioModificacionId = null;
                }
                else if (entrada.State == EntityState.Modified)
                {
                    entrada.Property(e => e.FechaCreacion).IsModified = false;
                    entrada.Property(e => e.UsuarioCreacionId).IsModified = false;

                    entrada.Entity.FechaModificacion = fecha;
                    entrada.Entity.UsuarioModificacionId = usuarioId;
                }

                var valoresAnteriores = new Dictionary<string, object?>();
                var valoresNuevos = new Dictionary<string, object?>();

                foreach (var propiedad in entrada.Properties)
                {
                    var nombre = propiedad.Metadata.Name;
                    // Registramos la operación, pero no copiamos el archivo al historial.
                    if (entrada.Entity is ComprobantePago &&
                        nombre == nameof(ComprobantePago.Contenido))
                    {
                        continue;
                    }
                    // La fecha y el usuario ya figuran en el encabezado
                    // del registro de auditoría.
                    if (nombre == nameof(AuditableEntity.FechaCreacion) ||
                        nombre == nameof(AuditableEntity.UsuarioCreacionId) ||
                        nombre == nameof(AuditableEntity.FechaModificacion) ||
                        nombre == nameof(AuditableEntity.UsuarioModificacionId))
                    {
                        continue;
                    }

                    if (propiedad.Metadata.IsPrimaryKey())
                        continue;

                    // No copiamos los datos personales de entrega al historial.
                    // Sí registramos que la dirección fue creada, editada o eliminada.
                    if (entrada.Entity is Direccion)
                        continue;

                    if (entrada.Entity is Pedido &&
                        (nombre == nameof(Pedido.DireccionEntrega) ||
                         nombre == nameof(Pedido.Observaciones)))
                    {
                        continue;
                    }

                    var anterior = anteriores?[nombre];
                    var nuevo = propiedad.CurrentValue;

                    if (entrada.State == EntityState.Added)
                    {
                        valoresNuevos[nombre] = nuevo;
                    }
                    else if (entrada.State == EntityState.Deleted)
                    {
                        valoresAnteriores[nombre] = anterior;
                    }
                    else if (propiedad.IsModified && !Equals(anterior, nuevo))
                    {
                        valoresAnteriores[nombre] = anterior;
                        valoresNuevos[nombre] = nuevo;
                    }
                }

                var accion = entrada.State switch
                {
                    EntityState.Added => "Alta",
                    EntityState.Modified => "Modificación",
                    _ => "Eliminación"
                };

                pendientes.Add((
                    entrada,
                    new RegistroAuditoria
                    {
                        Fecha = fecha,
                        UsuarioId = usuarioId,
                        UsuarioNombre = usuarioNombre,
                        Entidad = entrada.Metadata.ClrType.Name,
                        Accion = accion,
                        ValoresAnteriores = JsonSerializer.Serialize(valoresAnteriores),
                        ValoresNuevos = JsonSerializer.Serialize(valoresNuevos)
                    }));
            }

            // Si no hay cambios de entidades del negocio,
            // dejamos que Identity u otras entidades se guarden normalmente.
            if (pendientes.Count == 0)
                return await base.SaveChangesAsync(cancellationToken);

            // Si existe una transacción externa, participamos de ella.
            // Si no existe, creamos una para guardar operación e historial juntos.
            var transaccionPropia = Database.CurrentTransaction == null
                ? await Database.BeginTransactionAsync(cancellationToken)
                : null;

            try
            {
                var resultado = await base.SaveChangesAsync(cancellationToken);

                foreach (var pendiente in pendientes)
                {
                    // Después del primer guardado ya tenemos el ID generado
                    // para las altas.
                    var claves = pendiente.Entrada.Properties
                        .Where(p => p.Metadata.IsPrimaryKey())
                        .Select(p => Convert.ToString(
                            p.CurrentValue,
                            System.Globalization.CultureInfo.InvariantCulture));

                    pendiente.Registro.RegistroId = string.Join(", ", claves);

                    RegistrosAuditoria.Add(pendiente.Registro);
                }

                // Llamamos a base para no volver a ejecutar esta auditoría.
                await base.SaveChangesAsync(cancellationToken);

                if (transaccionPropia != null)
                    await transaccionPropia.CommitAsync(cancellationToken);

                return resultado;
            }
            catch
            {
                if (transaccionPropia != null)
                    await transaccionPropia.RollbackAsync(cancellationToken);

                throw;
            }
            finally
            {
                if (transaccionPropia != null)
                    await transaccionPropia.DisposeAsync();
            }
        }
    }
}