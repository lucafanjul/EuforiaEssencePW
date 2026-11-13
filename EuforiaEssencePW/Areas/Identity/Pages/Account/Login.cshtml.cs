using System.ComponentModel.DataAnnotations;
using EuforiaEssencePW.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EuforiaEssencePW.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;

        public LoginModel(
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
            [Required(ErrorMessage = "Ingrese su correo.")]
            [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
            [Display(Name = "Correo electrónico")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Ingrese su contraseña.")]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Mantener la sesión iniciada")]
            public bool RememberMe { get; set; }
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

            var usuario = await _userManager.FindByEmailAsync(
                Input.Email.Trim());

            if (usuario == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "El correo o la contraseña son incorrectos.");

                return Page();
            }

            var resultado = await _signInManager.PasswordSignInAsync(
                usuario,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: true);

            if (resultado.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }

                return RedirectToAction("Index", "Catalogo");
            }

            ModelState.AddModelError(
                string.Empty,
                resultado.IsLockedOut
                    ? "La cuenta está bloqueada temporalmente por intentos fallidos. Intente más tarde."
                    : "No se pudo iniciar sesión. Verifique el correo y la contraseña.");

            return Page();
        }
    }
}