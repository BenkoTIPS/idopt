using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace myApp.Simple.Pages;

[Authorize]
public class AboutModel : PageModel
{
    public string AuthenticationScheme { get; private set; } = string.Empty;
    public string HostingMode { get; private set; } = string.Empty;

    public void OnGet()
    {
        // Determine authentication scheme
        AuthenticationScheme = "Cookie Authentication";
        
        // Determine hosting mode based on Aspire context
        var aspireMode = HttpContext.RequestServices.GetService<IConfiguration>()?
            .GetSection("OTEL_SERVICE_NAME").Exists() == true;
        
        HostingMode = aspireMode ? "Aspire Orchestrated" : "Standalone";
    }
}
