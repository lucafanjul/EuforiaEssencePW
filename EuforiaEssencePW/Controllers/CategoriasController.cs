using EuforiaEssencePW.Data;
using EuforiaEssencePW.Models;
using EuforiaEssencePW.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EuforiaEssencePW.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class CategoriasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriasController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool EsAjax =>
            Request.Headers["X-Requested-With"].ToString()
                == "XMLHttpRequest";

        public async Task<IActionResult> Index(string? buscar, int pagina = 1)
        {
            buscar = buscar?.Trim();
            var consulta = _context.Categorias.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                consulta = consulta.Where(c =>
                    c.Nombre.Contains(buscar) ||
                    (c.Descripcion != null && c.Descripcion.Contains(buscar)));
            }

            return View(await PaginacionAdmin.CargarAsync(
                consulta.OrderBy(c => c.Nombre).ThenBy(c => c.Id),
                pagina,
                ViewData));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var categoria = await _context.Categorias
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categoria == null)
                return NotFound();

            return View(categoria);
        }

        public IActionResult Create()
        {
            var categoria = new Categoria();

            if (EsAjax)
                return PartialView("_FormularioCategoria", categoria);

            return View(categoria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Nombre,Descripcion")] Categoria categoria)
        {
            await ValidarNombreAsync(categoria, 0);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Categorias.Add(categoria);
                    await _context.SaveChangesAsync();

                    TempData["Exito"] = "Categoría creada correctamente.";

                    if (EsAjax)
                        return Json(new { success = true });

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo guardar. Verifique que el nombre no esté repetido.");
                }
            }

            if (EsAjax)
                return PartialView("_FormularioCategoria", categoria);

            return View(categoria);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var categoria = await _context.Categorias.FindAsync(id);

            if (categoria == null)
                return NotFound();

            if (EsAjax)
                return PartialView("_FormularioCategoria", categoria);

            return View(categoria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Nombre,Descripcion")] Categoria categoria)
        {
            if (id != categoria.Id)
                return NotFound();

            var existente = await _context.Categorias.FindAsync(id);

            if (existente == null)
                return NotFound();

            await ValidarNombreAsync(categoria, id);

            if (ModelState.IsValid)
            {
                try
                {
                    existente.Nombre = categoria.Nombre;
                    existente.Descripcion = categoria.Descripcion;

                    await _context.SaveChangesAsync();

                    TempData["Exito"] = "Categoría modificada correctamente.";

                    if (EsAjax)
                        return Json(new { success = true });

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Categorias.AnyAsync(c => c.Id == id))
                        return NotFound();

                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo actualizar. Recargue la página e intente nuevamente.");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo guardar. Verifique que el nombre no esté repetido.");
                }
            }

            if (EsAjax)
                return PartialView("_FormularioCategoria", categoria);

            return View(categoria);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var categoria = await _context.Categorias
                .Include(c => c.Perfumes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categoria == null)
                return NotFound();

            if (!categoria.PuedeEliminarse())
            {
                TempData["Error"] =
                    "No se puede eliminar porque tiene perfumes asociados.";

                return RedirectToAction(nameof(Index));
            }

            return View(categoria);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoria = await _context.Categorias
                .Include(c => c.Perfumes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categoria == null)
                return NotFound();

            if (!categoria.PuedeEliminarse())
            {
                TempData["Error"] =
                    "No se puede eliminar porque tiene perfumes asociados.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Categorias.Remove(categoria);
                await _context.SaveChangesAsync();

                TempData["Exito"] = "Categoría eliminada correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "No se pudo eliminar. Compruebe si tiene registros asociados.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task ValidarNombreAsync(
            Categoria categoria,
            int idExcluir)
        {
            categoria.Nombre =
                (categoria.Nombre ?? string.Empty).Trim();

            categoria.Descripcion = categoria.Descripcion?.Trim();

            if (string.IsNullOrWhiteSpace(categoria.Nombre))
            {
                ModelState.AddModelError(
                    nameof(Categoria.Nombre),
                    "El nombre es obligatorio.");
            }
            else if (await _context.Categorias.AnyAsync(c =>
                c.Nombre == categoria.Nombre && c.Id != idExcluir))
            {
                ModelState.AddModelError(
                    nameof(Categoria.Nombre),
                    "Ya existe una categoría con ese nombre.");
            }
        }
    }
}