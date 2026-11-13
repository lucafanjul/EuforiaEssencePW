using EuforiaEssencePW.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EuforiaEssencePW.Controllers
{
    [AllowAnonymous]
    public class CatalogoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CatalogoController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? buscar,
            int? marcaId,
            int? categoriaId)
        {
            var consulta = _context.Perfumes
                .AsNoTracking()
                .Where(p => p.Activo)
                .Include(p => p.Marca)
                .Include(p => p.Categoria)
                .Include(p => p.Presentaciones)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim();

                consulta = consulta.Where(p =>
                    p.Nombre.Contains(buscar) ||
                    p.Descripcion.Contains(buscar) ||
                    (p.NotasOlfativas != null &&
                     p.NotasOlfativas.Contains(buscar)));
            }

            if (marcaId.HasValue)
                consulta = consulta.Where(p => p.MarcaId == marcaId.Value);

            if (categoriaId.HasValue)
                consulta = consulta.Where(p => p.CategoriaId == categoriaId.Value);

            ViewBag.Buscar = buscar;

            ViewBag.Marcas = new SelectList(
                await _context.Marcas
                    .AsNoTracking()
                    .OrderBy(m => m.Nombre)
                    .ToListAsync(),
                "Id", "Nombre", marcaId);

            ViewBag.Categorias = new SelectList(
                await _context.Categorias
                    .AsNoTracking()
                    .OrderBy(c => c.Nombre)
                    .ToListAsync(),
                "Id", "Nombre", categoriaId);

            return View(await consulta
                .OrderBy(p => p.Nombre)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var perfume = await _context.Perfumes
                .AsNoTracking()
                .Include(p => p.Marca)
                .Include(p => p.Categoria)
                .Include(p => p.Presentaciones)
                .FirstOrDefaultAsync(p => p.Id == id && p.Activo);

            if (perfume == null)
                return NotFound();

            return View(perfume);
        }
    }
}