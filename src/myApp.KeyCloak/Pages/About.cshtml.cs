using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace myApp.KeyCloak.Pages;

[Authorize]
public class AboutModel : PageModel
{
    private readonly IConfiguration _configuration;

    public AboutModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string AuthenticationProvider { get; private set; } = string.Empty;
    public string Authority { get; private set; } = string.Empty;
    public string MaskedClientId { get; private set; } = string.Empty;
    public string Realm { get; private set; } = string.Empty;
    public string CallbackPath { get; private set; } = string.Empty;
    public string SignedOutCallbackPath { get; private set; } = string.Empty;
    public string ResponseType { get; private set; } = string.Empty;
    public string KeyCloakAdminUrl { get; private set; } = string.Empty;
    public string HostingMode { get; private set; } = string.Empty;

    public void OnGet()
    {
        // Get OIDC configuration
        var oidcSection = _configuration.GetSection("Authentication:Schemes:OpenIdConnect");
        
        AuthenticationProvider = "KeyCloak (Self-Hosted)";
        Authority = oidcSection["Authority"] ?? "Not configured";
        
        var clientId = oidcSection["ClientId"];
        MaskedClientId = !string.IsNullOrEmpty(clientId) && clientId.Length > 8 
            ? $"{clientId[..4]}...{clientId[^4..]}" 
            : "Not configured";
            
        // Extract realm from authority
        if (!string.IsNullOrEmpty(Authority) && Authority.Contains("/realms/"))
        {
            var realmPart = Authority.Split("/realms/");
            Realm = realmPart.Length > 1 ? realmPart[1].Split('/')[0] : "Not configured";
        }
        else
        {
            Realm = "Not configured";
        }
        
        CallbackPath = oidcSection["CallbackPath"] ?? "/signin-oidc";
        SignedOutCallbackPath = oidcSection["SignedOutCallbackPath"] ?? "/signout-callback-oidc";
        ResponseType = oidcSection["ResponseType"] ?? "code";
        
        // Construct KeyCloak admin URL
        if (!string.IsNullOrEmpty(Authority) && Authority.Contains("/realms/"))
        {
            var baseUrl = Authority.Split("/realms/")[0];
            KeyCloakAdminUrl = $"{baseUrl}/admin";
        }
        
        // Determine hosting mode based on Aspire context
        var aspireMode = HttpContext.RequestServices.GetService<IConfiguration>()?
            .GetSection("OTEL_SERVICE_NAME").Exists() == true;
        
        HostingMode = aspireMode ? "Aspire Orchestrated" : "Standalone";
    }
}
