using System.ComponentModel.DataAnnotations;
using EuforiaEssencePW.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EuforiaEssencePW.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;

        public RegisterModel(
            UserManager<Usuario> userManager,
            SignInManager<Usuario> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Ingrese su nombre.")]
            [StringLength(50)]
            [Display(Name = "Nombre")]
            public string Nombre { get; set; } = string.Empty;

            [Required(ErrorMessage = "Ingrese su apellido.")]
            [StringLength(50)]
            [Display(Name = "Apellido")]
            public string Apellido { get; set; } = string.Empty;

            [Required(ErrorMessage = "Ingrese su correo electrónico.")]
            [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
            [StringLength(256)]
            [Display(Name = "Correo electrónico")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Ingrese una contraseña.")]
            [StringLength(
                100,
                MinimumLength = 8,
                ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Repita la contraseña.")]
            [DataType(DataType.Password)]
            [Compare(
                nameof(Password),
                ErrorMessage = "Las contraseñas no coinciden.")]
            [Display(Name = "Confirmar contraseña")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(
            string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return Page();

            var usuario = new Usuario
            {
                UserName = Input.Email.Trim(),
                Email = Input.Email.Trim(),
                Nombre = Input.Nombre.Trim(),
                Apellido = Input.Apellido.Trim(),
                FechaAlta = DateTime.UtcNow
            };

            var resultado = await _userManager.CreateAsync(
                usuario, Input.Password);

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return Page();
            }

            // El formulario nunca permite elegir Administrador.
            var asignacion = await _userManager.AddToRoleAsync(
                usuario, "Cliente");

            if (!asignacion.Succeeded)
            {
                // Evitamos dejar una cuenta nueva sin su rol.
                await _userManager.DeleteAsync(usuario);

                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo completar el registro. Intente nuevamente.");

                return Page();
            }

            await _signInManager.SignInAsync(
                usuario, isPersistent: false);

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Catalogo");
        }
    }
}