using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyApp.Namespace
{
    public class myLoginModel : PageModel
    {
        [BindProperty]
        public string? Username { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Username))
            {
                ModelState.AddModelError(string.Empty, "Username is required.");
                return Page();
            }

            // Create user claims
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, Username)
        };

            // Create an identity and principal
            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            // Sign in with cookie authentication
            await HttpContext.SignInAsync("CookieAuth", principal);

            // Redirect to a protected page (e.g., Index)
            return RedirectToPage("/Uploads");
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            // Sign the user out
            await HttpContext.SignOutAsync("CookieAuth");

            // Redirect to home or login page
            return RedirectToPage("/Index");
        }

    }
}
