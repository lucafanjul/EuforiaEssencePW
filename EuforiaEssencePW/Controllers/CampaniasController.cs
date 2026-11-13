using EuforiaEssencePW.Data;
using EuforiaEssencePW.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EuforiaEssencePW.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class CampaniasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CampaniasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var campanias = await _context.Campanias
                .AsNoTracking()
                .OrderByDescending(c => c.Prioridad)
                .ThenByDescending(c => c.Id)
                .ToListAsync();

            return View(campanias);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            Campania campania;

            if (id.HasValue)
            {
                var existente = await _context.Campanias
                    .AsNoTracking()
                    .Include(c => c.Productos)
                    .FirstOrDefaultAsync(c => c.Id == id.Value);

                if (existente == null)
                    return NotFound();

                campania = existente;
            }
            else
            {
                campania = new Campania();
            }

            await CargarPerfumes(
                campania.Productos.Select(p => p.PerfumeId));

            return View(campania);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int? id,
            [Bind("Id,Titulo,Descripcion,Desde,Hasta,Prioridad,Activa")]
            Campania modelo,
            int[] perfumesSeleccionados)
        {
            if ((id ?? 0) != modelo.Id)
                return BadRequest();

            var seleccion = perfumesSeleccionados
                .Distinct()
                .ToArray();

            if (seleccion.Length == 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Seleccioná al menos un perfume.");
            }
            else
            {
                var cantidadValidos = await _context.Perfumes
                    .CountAsync(p => seleccion.Contains(p.Id));

                if (cantidadValidos != seleccion.Length)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Uno de los perfumes seleccionados ya no existe.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarPerfumes(seleccion);
                return View(modelo);
            }

            Campania campania;

            if (modelo.Id == 0)
            {
                campania = new Campania();
                _context.Campanias.Add(campania);
            }
            else
            {
                var existente = await _context.Campanias
                    .Include(c => c.Productos)
                    .FirstOrDefaultAsync(c => c.Id == modelo.Id);

                if (existente == null)
                    return NotFound();

                campania = existente;
            }

            campania.Titulo = modelo.Titulo.Trim();
            campania.Descripcion = modelo.Descripcion.Trim();
            campania.Desde = modelo.Desde.Date;
            campania.Hasta = modelo.Hasta.Date;
            campania.Prioridad = modelo.Prioridad;
            campania.Activa = modelo.Activa;

            var seleccionIds = seleccion.ToHashSet();

            var asociacionesAnteriores = campania.Productos
                .Select(p => p.PerfumeId)
                .ToHashSet();

            var quitar = campania.Productos
                .Where(p => !seleccionIds.Contains(p.PerfumeId))
                .ToList();

            _context.CampaniasPerfumes.RemoveRange(quitar);

            foreach (var perfumeId in seleccionIds)
            {
                if (!asociacionesAnteriores.Contains(perfumeId))
                {
                    campania.Productos.Add(new CampaniaPerfume
                    {
                        PerfumeId = perfumeId
                    });
                }
            }

            try
            {
                await _context.SaveChangesAsync();

                TempData["Exito"] = "Campaña guardada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo guardar la campaña. Volvé a intentarlo.");

                await CargarPerfumes(seleccion);
                return View(modelo);
            }
        }

        private async Task CargarPerfumes(IEnumerable<int> seleccionados)
        {
            var ids = seleccionados.ToHashSet();

            var perfumes = await _context.Perfumes
                .AsNoTracking()
                .OrderBy(p => p.Nombre)
                .Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    p.Activo
                })
                .ToListAsync();

            ViewBag.Perfumes = perfumes.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Nombre + (p.Activo ? "" : " (inactivo)"),
                Selected = ids.Contains(p.Id)
            }).ToList();
        }
    }
}