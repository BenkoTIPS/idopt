using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyApp.EasyAuth.Pages;

public class LocalSimulationModel : PageModel
{
    public IActionResult OnGet(string provider = "aad", string returnUrl = "/")
    {
        if (!HttpContext.Session.IsAvailable)
        {
            return BadRequest("Session not available");
        }

        // Store the simulated authentication in session
        HttpContext.Session.SetString("SimulatedAuthProvider", provider);
        HttpContext.Session.SetString("SimulatedUserId", $"{provider}_user_{DateTime.Now.Ticks}");

        // Redirect with simulation parameters
        var redirectUrl = $"{returnUrl}?simulate_provider={provider}&simulate_user={HttpContext.Session.GetString("SimulatedUserId")}";
        return Redirect(redirectUrl);
    }
}
