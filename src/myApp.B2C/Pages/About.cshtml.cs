using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace myApp.B2C.Pages;

[Authorize]
public class AboutModel : PageModel
{
    private readonly IConfiguration _configuration;

    public AboutModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string AuthenticationProvider { get; private set; } = string.Empty;
    public string B2CTenant { get; private set; } = string.Empty;
    public string B2CPolicy { get; private set; } = string.Empty;
    public string MaskedClientId { get; private set; } = string.Empty;
    public string Authority { get; private set; } = string.Empty;
    public string CallbackPath { get; private set; } = string.Empty;
    public string HostingMode { get; private set; } = string.Empty;

    public void OnGet()
    {
        // Get B2C configuration
        var azureAdB2C = _configuration.GetSection("AzureAdB2C");

        AuthenticationProvider = "Azure AD B2C";
        B2CTenant = azureAdB2C["Domain"] ?? "Not configured";
        B2CPolicy = azureAdB2C["SignUpSignInPolicyId"] ?? "Not configured";

        var clientId = azureAdB2C["ClientId"];
        MaskedClientId = !string.IsNullOrEmpty(clientId) && clientId.Length > 8
            ? $"{clientId[..4]}...{clientId[^4..]}"
            : "Not configured";

        Authority = azureAdB2C["Instance"] != null && azureAdB2C["Domain"] != null && azureAdB2C["SignUpSignInPolicyId"] != null
            ? $"{azureAdB2C["Instance"]}{azureAdB2C["Domain"]}/{azureAdB2C["SignUpSignInPolicyId"]}/v2.0"
            : "Not configured";

        CallbackPath = azureAdB2C["CallbackPath"] ?? "/signin-oidc";

        // Determine hosting mode based on Aspire context
        var aspireMode = HttpContext.RequestServices.GetService<IConfiguration>()?
            .GetSection("OTEL_SERVICE_NAME").Exists() == true;

        HostingMode = aspireMode ? "Aspire Orchestrated" : "Standalone";
    }
}
