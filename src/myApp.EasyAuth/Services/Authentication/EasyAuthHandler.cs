using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace myTrackr.Services.Authentication;

public class EasyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public EasyAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for EasyAuth headers (in production Azure App Service)
        if (TryGetEasyAuthHeaders(out var principal))
        {
            Logger.LogInformation("EasyAuth headers found, authenticating user");
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "EasyAuth")));
        }

        // For local development, check if we have a simulated login in session
        if (TryGetLocalSimulatedAuth(out principal))
        {
            Logger.LogInformation("Local simulated EasyAuth found, authenticating user");
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "EasyAuth")));
        }

        Logger.LogDebug("No EasyAuth headers or local simulation found");
        return Task.FromResult(AuthenticateResult.NoResult());
    }

    private bool TryGetEasyAuthHeaders(out ClaimsPrincipal principal)
    {
        principal = new ClaimsPrincipal();

        // Check for the main EasyAuth header
        if (!Request.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL", out var principalHeader) ||
            string.IsNullOrEmpty(principalHeader))
        {
            return false;
        }

        try
        {
            // Decode the base64 principal data
            var principalData = Convert.FromBase64String(principalHeader!);
            var principalJson = System.Text.Encoding.UTF8.GetString(principalData);
            var principalInfo = JsonSerializer.Deserialize<EasyAuthPrincipal>(principalJson);

            if (principalInfo?.Claims == null || !principalInfo.Claims.Any())
            {
                return false;
            }

            // Convert EasyAuth claims to ClaimsPrincipal
            var claims = principalInfo.Claims.Select(c => new Claim(c.Type, c.Value)).ToList();
            
            // Ensure we have required claims
            if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, principalInfo.UserId ?? "unknown"));
            }
            
            if (!claims.Any(c => c.Type == ClaimTypes.Name))
            {
                var nameFromHeader = Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"].FirstOrDefault();
                claims.Add(new Claim(ClaimTypes.Name, nameFromHeader ?? principalInfo.UserId ?? "Unknown User"));
            }

            var identity = new ClaimsIdentity(claims, "EasyAuth");
            principal = new ClaimsPrincipal(identity);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to parse EasyAuth headers");
            return false;
        }
    }

    private bool TryGetLocalSimulatedAuth(out ClaimsPrincipal principal)
    {
        principal = new ClaimsPrincipal();

        // Check for local simulation marker in session or query string
        var provider = Request.Query["simulate_provider"].FirstOrDefault();
        var userId = Request.Query["simulate_user"].FirstOrDefault();

        if (string.IsNullOrEmpty(provider) && Context.Session.IsAvailable)
        {
            provider = Context.Session.GetString("SimulatedAuthProvider");
            userId = Context.Session.GetString("SimulatedUserId");
        }

        if (string.IsNullOrEmpty(provider))
        {
            return false;
        }

        // Create simulated claims based on provider
        var claims = new List<Claim>();
        
        switch (provider.ToLowerInvariant())
        {
            case "aad":
                claims.AddRange([
                    new Claim(ClaimTypes.NameIdentifier, userId ?? "aad_user_12345"),
                    new Claim(ClaimTypes.Name, "John Doe"),
                    new Claim(ClaimTypes.Email, "john.doe@contoso.com"),
                    new Claim("idp", "aad"),
                    new Claim("ver", "2.0")
                ]);
                break;
                
            case "google":
                claims.AddRange([
                    new Claim(ClaimTypes.NameIdentifier, userId ?? "google_user_67890"),
                    new Claim(ClaimTypes.Name, "Jane Smith"),
                    new Claim(ClaimTypes.Email, "jane.smith@gmail.com"),
                    new Claim("idp", "google"),
                    new Claim("picture", "https://lh3.googleusercontent.com/a/default-user=s96-c")
                ]);
                break;
                
            case "facebook":
                claims.AddRange([
                    new Claim(ClaimTypes.NameIdentifier, userId ?? "facebook_user_11111"),
                    new Claim(ClaimTypes.Name, "Bob Johnson"),
                    new Claim(ClaimTypes.Email, "bob.johnson@example.com"),
                    new Claim("idp", "facebook"),
                    new Claim("picture", "https://graph.facebook.com/v12.0/me/picture")
                ]);
                break;
                
            default:
                // Generic EasyAuth user
                claims.AddRange([
                    new Claim(ClaimTypes.NameIdentifier, userId ?? "easyauth_user_99999"),
                    new Claim(ClaimTypes.Name, "Demo User"),
                    new Claim(ClaimTypes.Email, "demo@example.com"),
                    new Claim("idp", provider)
                ]);
                break;
        }

        var identity = new ClaimsIdentity(claims, "EasyAuth");
        principal = new ClaimsPrincipal(identity);
        
        Logger.LogInformation("Simulated EasyAuth login for provider: {Provider}", provider);
        return true;
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // In local development, redirect to our login page
        var loginUrl = "/Login";
        
        // Preserve the original return URL
        if (!string.IsNullOrEmpty(properties?.RedirectUri))
        {
            loginUrl += $"?returnUrl={Uri.EscapeDataString(properties.RedirectUri)}";
        }

        Response.Redirect(loginUrl);
        return Task.CompletedTask;
    }
}

// Helper class for deserializing EasyAuth principal data
public class EasyAuthPrincipal
{
    public string? AuthType { get; set; }
    public string? UserId { get; set; }
    public string? UserIdClaimType { get; set; }
    public string? RoleClaimType { get; set; }
    public List<EasyAuthClaim> Claims { get; set; } = new();
}

public class EasyAuthClaim
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
