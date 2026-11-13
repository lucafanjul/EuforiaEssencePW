using EuforiaEssencePW.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EuforiaEssencePW.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AuditoriaIAController : Controller
    {
        [HttpGet]
        public IActionResult Index(
            [Bind("Seccion,Desde,Hasta,Entidad,Accion,Usuario")]
            AnalisisAuditoriaViewModel modelo)
        {
            return RedirectToAction("Index", "Auditoria", new
            {
                seccion = modelo.Seccion,
                desde = modelo.Desde?.ToString("yyyy-MM-dd"),
                hasta = modelo.Hasta?.ToString("yyyy-MM-dd"),
                entidad = modelo.Entidad,
                accion = modelo.Accion,
                usuario = modelo.Usuario
            });
        }
    }
}