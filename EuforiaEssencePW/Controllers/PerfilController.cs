using EuforiaEssencePW.Models;
using EuforiaEssencePW.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EuforiaEssencePW.Controllers
{
    [Authorize]
    public class PerfilController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;

        public PerfilController(
            UserManager<Usuario> userManager,
            SignInManager<Usuario> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuario = await _userManager.GetUserAsync(User);

            if (usuario == null)
                return Challenge();

            return View(new PerfilViewModel
            {
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Telefono = usuario.PhoneNumber,
                Correo = usuario.Email ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            [Bind("Nombre,Apellido,Telefono")] PerfilViewModel modelo)
        {
            var usuario = await _userManager.GetUserAsync(User);

            if (usuario == null)
                return Challenge();

            modelo.Correo = usuario.Email ?? string.Empty;

            if (!ModelState.IsValid)
                return View(modelo);

            usuario.Nombre = modelo.Nombre.Trim();
            usuario.Apellido = modelo.Apellido.Trim();

            var telefono = string.IsNullOrWhiteSpace(modelo.Telefono)
                ? null
                : modelo.Telefono.Trim();

            if (usuario.PhoneNumber != telefono)
            {
                usuario.PhoneNumber = telefono;
                usuario.PhoneNumberConfirmed = false;
            }

            var resultado = await _userManager.UpdateAsync(usuario);

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty, error.Description);
                }

                return View(modelo);
            }

            await _signInManager.RefreshSignInAsync(usuario);

            TempData["Exito"] = "Datos del perfil actualizados.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult CambiarClave()
        {
            return View(new CambiarClaveViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarClave(
            CambiarClaveViewModel modelo)
        {
            if (!ModelState.IsValid)
                return View(modelo);

            var usuario = await _userManager.GetUserAsync(User);

            if (usuario == null)
                return Challenge();

            var resultado = await _userManager.ChangePasswordAsync(
                usuario,
                modelo.Actual,
                modelo.Nueva);

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty, error.Description);
                }

                return View(modelo);
            }

            await _signInManager.RefreshSignInAsync(usuario);

            TempData["Exito"] = "Contraseña actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}