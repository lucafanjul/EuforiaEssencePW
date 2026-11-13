using EuforiaEssencePW.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace EuforiaEssencePW.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class GestionPedidosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GestionPedidosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? estado)
        {
            var consulta = _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Usuario)
                .Include(p => p.Detalles)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
                consulta = consulta.Where(p => p.Estado == estado);

            ViewBag.Estado = estado;

            return View(await consulta
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Usuario)
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return NotFound();

            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(
            int id,
            string nuevoEstado)
        {
            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var pedido = await _context.Pedidos.FindAsync(id);

                if (pedido == null)
                    return NotFound();

                if (!pedido.PuedeCambiarA(nuevoEstado))
                {
                    TempData["Error"] =
"El cambio de estado no está permitido. Para preparar un pedido nuevo, su pago debe estar aprobado.";

                    return RedirectToAction(nameof(Details), new { id });
                }

                pedido.Estado = nuevoEstado;

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                TempData["Exito"] =
                    "Estado del pedido actualizado correctamente.";
            }
            catch (Exception ex) when (
                ex is DbUpdateException ||
                ex is Microsoft.Data.SqlClient.SqlException)
            {
                await transaccion.RollbackAsync();
                _context.ChangeTracker.Clear();

                TempData["Error"] =
                    "No se pudo actualizar el pedido. Recargue e intente nuevamente.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}