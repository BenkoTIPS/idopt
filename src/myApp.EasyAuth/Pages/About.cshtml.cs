using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace myApp.EasyAuth.Pages;

[Authorize]
public class AboutModel : PageModel
{
    public string AuthenticationProvider { get; private set; } = string.Empty;
    public string HostingMode { get; private set; } = string.Empty;
    public Dictionary<string, string> EasyAuthHeaders { get; private set; } = new();

    public void OnGet()
    {
        // Check for EasyAuth headers
        var easyAuthHeaders = new Dictionary<string, string>();
        var commonEasyAuthHeaders = new[]
        {
            "X-MS-CLIENT-PRINCIPAL",
            "X-MS-CLIENT-PRINCIPAL-NAME", 
            "X-MS-CLIENT-PRINCIPAL-ID",
            "X-MS-CLIENT-PRINCIPAL-IDP",
            "X-MS-TOKEN-AAD-ID-TOKEN",
            "X-MS-TOKEN-AAD-ACCESS-TOKEN"
        };

        foreach (var headerName in commonEasyAuthHeaders)
        {
            if (HttpContext.Request.Headers.TryGetValue(headerName, out var value) && !string.IsNullOrEmpty(value))
            {
                easyAuthHeaders[headerName] = value.ToString();
            }
        }

        EasyAuthHeaders = easyAuthHeaders;
        
        // Determine authentication provider
        if (EasyAuthHeaders.ContainsKey("X-MS-CLIENT-PRINCIPAL-IDP"))
        {
            AuthenticationProvider = $"Azure App Service EasyAuth ({EasyAuthHeaders["X-MS-CLIENT-PRINCIPAL-IDP"]})";
        }
        else
        {
            AuthenticationProvider = "Azure App Service EasyAuth (Not Active)";
        }
        
        // Determine hosting mode based on Aspire context
        var aspireMode = HttpContext.RequestServices.GetService<IConfiguration>()?
            .GetSection("OTEL_SERVICE_NAME").Exists() == true;
        
        HostingMode = aspireMode ? "Aspire Orchestrated" : "Standalone";
    }
}
