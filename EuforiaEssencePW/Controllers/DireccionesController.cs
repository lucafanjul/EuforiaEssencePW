using EuforiaEssencePW.Data;
using EuforiaEssencePW.Models;
using EuforiaEssencePW.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EuforiaEssencePW.Controllers
{
    [Authorize(Roles = "Cliente,Administrador")]
    public class DireccionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public DireccionesController(
            ApplicationDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var usuarioId = _userManager.GetUserId(User);

            return View(await _context.Direcciones
                .AsNoTracking()
                .Where(d => d.UsuarioId == usuarioId)
                .OrderBy(d => d.Id)
                .ToListAsync());
        }

        public IActionResult Create()
        {
            return View(new DireccionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DireccionViewModel modelo)
        {
            if (!ModelState.IsValid)
                return View(modelo);

            var usuarioId = _userManager.GetUserId(User);

            if (usuarioId == null)
                return Challenge();

            var direccion = new Direccion
            {
                UsuarioId = usuarioId
            };

            CopiarDatos(modelo, direccion);

            try
            {
                _context.Direcciones.Add(direccion);
                await _context.SaveChangesAsync();

                TempData["Exito"] = "Dirección guardada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo guardar la dirección. Intente nuevamente.");

                return View(modelo);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var usuarioId = _userManager.GetUserId(User);

            var direccion = await _context.Direcciones
                .AsNoTracking()
                .FirstOrDefaultAsync(d =>
                    d.Id == id && d.UsuarioId == usuarioId);

            if (direccion == null)
                return NotFound();

            return View(new DireccionViewModel
            {
                Id = direccion.Id,
                Destinatario = direccion.Destinatario,
                Calle = direccion.Calle,
                Altura = direccion.Altura,
                PisoDepartamento = direccion.PisoDepartamento,
                Localidad = direccion.Localidad,
                Provincia = direccion.Provincia,
                CodigoPostal = direccion.CodigoPostal
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            DireccionViewModel modelo)
        {
            if (id != modelo.Id)
                return NotFound();

            var usuarioId = _userManager.GetUserId(User);

            var direccion = await _context.Direcciones
                .FirstOrDefaultAsync(d =>
                    d.Id == id && d.UsuarioId == usuarioId);

            if (direccion == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(modelo);

            CopiarDatos(modelo, direccion);

            try
            {
                await _context.SaveChangesAsync();

                TempData["Exito"] = "Dirección actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "La dirección ya no está disponible o cambió. Recargue la página.");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo actualizar la dirección.");
            }

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var usuarioId = _userManager.GetUserId(User);

            var direccion = await _context.Direcciones
                .Include(d => d.Pedidos)
                .FirstOrDefaultAsync(d =>
                    d.Id == id && d.UsuarioId == usuarioId);

            if (direccion == null)
                return NotFound();

            if (!direccion.PuedeEliminarse())
            {
                TempData["Error"] =
                    "Esta dirección está vinculada a pedidos y no puede eliminarse.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Direcciones.Remove(direccion);
                await _context.SaveChangesAsync();

                TempData["Exito"] = "Dirección eliminada correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "No se pudo eliminar. Puede estar vinculada a un pedido.";
            }

            return RedirectToAction(nameof(Index));
        }

        private static void CopiarDatos(
            DireccionViewModel modelo,
            Direccion direccion)
        {
            direccion.Destinatario = modelo.Destinatario.Trim();
            direccion.Calle = modelo.Calle.Trim();
            direccion.Altura = modelo.Altura.Trim();
            direccion.PisoDepartamento = modelo.PisoDepartamento?.Trim();
            direccion.Localidad = modelo.Localidad.Trim();
            direccion.Provincia = modelo.Provincia.Trim();
            direccion.CodigoPostal = modelo.CodigoPostal.Trim();
        }
    }
}