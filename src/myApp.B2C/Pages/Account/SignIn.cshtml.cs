using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace myApp.B2C.Pages.Account;

public class SignInModel : PageModel
{
    public IActionResult OnGet()
    {
        // Redirect to Azure AD B2C for authentication
        return Challenge(new AuthenticationProperties 
        { 
            RedirectUri = "/" 
        }, OpenIdConnectDefaults.AuthenticationScheme);
    }
}
