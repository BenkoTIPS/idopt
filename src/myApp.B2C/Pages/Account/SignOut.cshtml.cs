using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace myApp.B2C.Pages.Account;

public class SignOutModel : PageModel
{
    public IActionResult OnGet()
    {
        // Sign out from both the application and Azure AD B2C
        return SignOut(
            new AuthenticationProperties 
            { 
                RedirectUri = "/" 
            },
            OpenIdConnectDefaults.AuthenticationScheme
        );
    }
}
