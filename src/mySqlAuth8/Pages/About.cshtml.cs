using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Reflection;

namespace mySqlAuth8.Pages;

[Authorize]
public class AboutModel : PageModel
{
    private readonly ILogger<AboutModel> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IServiceProvider _serviceProvider;

    public AboutModel(ILogger<AboutModel> logger, IWebHostEnvironment environment, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _environment = environment;
        _serviceProvider = serviceProvider;
    }

    public string? UserId { get; set; }
    public string AuthenticationProvider { get; set; } = "Unknown";
    public string? SessionId { get; set; }
    public DateTime? LoginTime { get; set; }
    public string IpAddress { get; set; } = "Unknown";
    public List<Claim> UserClaims { get; set; } = new();
    public Dictionary<string, string> EasyAuthHeaders { get; set; } = new();
    public string Environment { get; set; } = "Unknown";
    public string HostName { get; set; } = "Unknown";
    public string? UserAgent { get; set; }
    public bool HasAntiForgeryToken { get; set; }
    public string DetectedProviderType { get; set; } = "Unknown";
    public string DetectionReason { get; set; } = "";

    // New application-level authentication properties
    public string ApplicationName { get; set; } = "";
    public string ApplicationVersion { get; set; } = "";
    public List<string> ConfiguredAuthenticationSchemes { get; set; } = new();
    public string DefaultAuthenticationScheme { get; set; } = "Unknown";
    public string DefaultChallengeScheme { get; set; } = "Unknown";
    public bool SessionsConfigured { get; set; }
    public bool CookiesConfigured { get; set; }
    public bool JwtConfigured { get; set; }
    public bool IdentityConfigured { get; set; }
    public Dictionary<string, object> AuthenticationConfiguration { get; set; } = new();
    public string DatabaseProvider { get; set; } = "Unknown";
    public bool RequireConfirmedAccount { get; set; }
    public string IdentityOptionsInfo { get; set; } = "";

    public void OnGet()
    {
        PopulateApplicationInfo();
        
        if (User.Identity?.IsAuthenticated == true)
        {
            PopulateUserInformation();
            PopulateAuthenticationContext();
            PopulateClaims();
            PopulateEasyAuthHeaders();
            PopulateEnvironmentInfo();
            DetectIdentityProvider();
        }
    }

    private void PopulateApplicationInfo()
    {
        // Get application name and version
        var assembly = Assembly.GetExecutingAssembly();
        ApplicationName = assembly.GetName().Name ?? "mySqlAuth8";
        ApplicationVersion = assembly.GetName().Version?.ToString() ?? "Unknown";

        // Check what authentication schemes are configured
        try
        {
            var authSchemeProvider = _serviceProvider.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
            if (authSchemeProvider != null)
            {
                var schemes = authSchemeProvider.GetAllSchemesAsync().Result;
                ConfiguredAuthenticationSchemes = schemes.Select(s => s.Name).ToList();
                
                var defaultScheme = authSchemeProvider.GetDefaultAuthenticateSchemeAsync().Result;
                DefaultAuthenticationScheme = defaultScheme?.Name ?? "None";
                
                var defaultChallengeScheme = authSchemeProvider.GetDefaultChallengeSchemeAsync().Result;
                DefaultChallengeScheme = defaultChallengeScheme?.Name ?? "None";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not retrieve authentication schemes: {Error}", ex.Message);
        }

        // Check if various authentication features are configured
        SessionsConfigured = CheckIfSessionsConfigured();
        CookiesConfigured = ConfiguredAuthenticationSchemes.Any(s => s.Contains("Cookie"));
        JwtConfigured = ConfiguredAuthenticationSchemes.Any(s => s.Contains("Bearer") || s.Contains("JWT"));
        IdentityConfigured = _serviceProvider.GetService<UserManager<IdentityUser>>() != null;

        // Get Identity options if configured
        if (IdentityConfigured)
        {
            try
            {
                var userManager = _serviceProvider.GetService<UserManager<IdentityUser>>();
                if (userManager != null)
                {
                    var identityOptions = _serviceProvider.GetService<IOptions<IdentityOptions>>();
                    if (identityOptions?.Value != null)
                    {
                        var options = identityOptions.Value;
                        RequireConfirmedAccount = options.SignIn.RequireConfirmedAccount;
                        IdentityOptionsInfo = $"Password: RequireDigit={options.Password.RequireDigit}, " +
                                            $"RequireLength={options.Password.RequiredLength}, " +
                                            $"RequireUppercase={options.Password.RequireUppercase}; " +
                                            $"Lockout: MaxFailedAttempts={options.Lockout.MaxFailedAccessAttempts}, " +
                                            $"DefaultLockoutTimeSpan={options.Lockout.DefaultLockoutTimeSpan.TotalMinutes}min";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not retrieve Identity options: {Error}", ex.Message);
                IdentityOptionsInfo = "Could not retrieve Identity configuration";
            }
        }

        // Determine database provider
        try
        {
            var dbContext = _serviceProvider.GetService<mySqlAuth8.Data.ApplicationDbContext>();
            if (dbContext != null)
            {
                var providerName = dbContext.Database.ProviderName;
                DatabaseProvider = providerName switch
                {
                    "Microsoft.EntityFrameworkCore.SqlServer" => "SQL Server",
                    "Microsoft.EntityFrameworkCore.Sqlite" => "SQLite",
                    "Microsoft.EntityFrameworkCore.InMemory" => "In-Memory",
                    "Npgsql.EntityFrameworkCore.PostgreSQL" => "PostgreSQL",
                    "Pomelo.EntityFrameworkCore.MySql" => "MySQL",
                    _ => providerName ?? "Unknown"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not determine database provider: {Error}", ex.Message);
            DatabaseProvider = "Could not determine";
        }
    }

    private bool CheckIfSessionsConfigured()
    {
        try
        {
            var sessionId = HttpContext.Session.Id;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void PopulateUserInformation()
    {
        // Get user ID from various possible claim types
        UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                 User.FindFirst("sub")?.Value ??
                 User.FindFirst("oid")?.Value ??
                 User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
    }

    private void PopulateAuthenticationContext()
    {
        // Determine authentication provider
        var authType = User.Identity?.AuthenticationType;
        AuthenticationProvider = authType switch
        {
            "Identity.Application" => "ASP.NET Core Identity",
            "AuthenticationTypes.Federation" => "Federation",
            "Cookies" => "Cookie Authentication",
            "Bearer" => "Bearer Token",
            "Basic" => "Basic Authentication",
            _ => authType ?? "Unknown"
        };

        // Get session information (only if sessions are configured)
        try
        {
            SessionId = HttpContext.Session.Id;
        }
        catch (InvalidOperationException)
        {
            SessionId = "Sessions not configured";
        }
        
        // Try to get login time from auth timestamp claim
        var authTimeClaim = User.FindFirst("auth_time") ?? User.FindFirst(ClaimTypes.AuthenticationInstant);
        if (authTimeClaim != null && DateTime.TryParse(authTimeClaim.Value, out var authTime))
        {
            LoginTime = authTime;
        }

        // Get IP address
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private void PopulateClaims()
    {
        UserClaims = User.Claims.ToList();
    }

    private void PopulateEasyAuthHeaders()
    {
        // EasyAuth headers that Azure App Service provides
        var easyAuthHeaderNames = new[]
        {
            "X-MS-CLIENT-PRINCIPAL",
            "X-MS-CLIENT-PRINCIPAL-NAME",
            "X-MS-CLIENT-PRINCIPAL-ID",
            "X-MS-CLIENT-PRINCIPAL-IDP",
            "X-MS-TOKEN-AAD-ID-TOKEN",
            "X-MS-TOKEN-AAD-ACCESS-TOKEN",
            "X-MS-TOKEN-AAD-EXPIRES-ON",
            "X-MS-TOKEN-AAD-REFRESH-TOKEN"
        };

        foreach (var headerName in easyAuthHeaderNames)
        {
            if (Request.Headers.ContainsKey(headerName))
            {
                EasyAuthHeaders[headerName] = Request.Headers[headerName].ToString();
            }
        }
    }

    private void PopulateEnvironmentInfo()
    {
        Environment = _environment.EnvironmentName;
        HostName = Request.Host.ToString();
        UserAgent = Request.Headers["User-Agent"].ToString();
        
        // Check for anti-forgery token (safely handle form access)
        try
        {
            HasAntiForgeryToken = Request.Headers.ContainsKey("RequestVerificationToken") ||
                                 (Request.HasFormContentType && Request.Form.ContainsKey("__RequestVerificationToken"));
        }
        catch (InvalidOperationException)
        {
            // This is expected for GET requests without form content
            HasAntiForgeryToken = Request.Headers.ContainsKey("RequestVerificationToken");
        }
    }

    private void DetectIdentityProvider()
    {
        var reasons = new List<string>();

        // Check for EasyAuth
        if (EasyAuthHeaders.Any())
        {
            DetectedProviderType = "Azure App Service EasyAuth";
            reasons.Add("EasyAuth headers detected");
            
            if (EasyAuthHeaders.ContainsKey("X-MS-CLIENT-PRINCIPAL-IDP"))
            {
                var idp = EasyAuthHeaders["X-MS-CLIENT-PRINCIPAL-IDP"];
                DetectedProviderType += $" ({idp})";
                reasons.Add($"Identity provider: {idp}");
            }
        }
        // Check for Azure B2C
        else if (User.Claims.Any(c => c.Type == "tfp" || c.Type == "acr" || c.Issuer.Contains("b2clogin.com")))
        {
            DetectedProviderType = "Azure AD B2C";
            
            var policy = User.FindFirst("tfp")?.Value ?? User.FindFirst("acr")?.Value;
            if (!string.IsNullOrEmpty(policy))
            {
                DetectedProviderType += $" (Policy: {policy})";
                reasons.Add($"B2C policy detected: {policy}");
            }
            
            reasons.Add("B2C-specific claims found (tfp/acr)");
        }
        // Check for Azure AD
        else if (User.Claims.Any(c => c.Issuer.Contains("sts.windows.net") || 
                                     c.Issuer.Contains("login.microsoftonline.com") ||
                                     c.Type == "tid"))
        {
            DetectedProviderType = "Azure Active Directory";
            
            var tenant = User.FindFirst("tid")?.Value;
            if (!string.IsNullOrEmpty(tenant))
            {
                reasons.Add($"Azure AD tenant: {tenant}");
            }
            
            reasons.Add("Azure AD issuer or tenant claim detected");
        }
        // Check for ASP.NET Core Identity
        else if (User.Identity?.AuthenticationType == "Identity.Application")
        {
            DetectedProviderType = "ASP.NET Core Identity (Local)";
            reasons.Add("Identity.Application authentication type");
            reasons.Add("Likely using Entity Framework with IdentityUser");
        }
        // Check for other OAuth providers
        else if (User.Claims.Any(c => c.Type == "iss"))
        {
            var issuer = User.FindFirst("iss")?.Value;
            DetectedProviderType = issuer switch
            {
                var i when i?.Contains("google") == true => "Google OAuth",
                var i when i?.Contains("facebook") == true => "Facebook",
                var i when i?.Contains("github") == true => "GitHub",
                var i when i?.Contains("twitter") == true => "Twitter",
                _ => $"OAuth Provider ({issuer})"
            };
            reasons.Add($"OAuth issuer: {issuer}");
        }
        else
        {
            DetectedProviderType = "Unknown Provider";
            reasons.Add("Could not determine provider from available claims and headers");
        }

        DetectionReason = string.Join("; ", reasons);
    }

    public bool IsWellKnownClaim(string claimType)
    {
        var wellKnownClaims = new[]
        {
            ClaimTypes.Name,
            ClaimTypes.NameIdentifier,
            ClaimTypes.Email,
            ClaimTypes.Role,
            ClaimTypes.GivenName,
            ClaimTypes.Surname,
            "sub", "email", "name", "given_name", "family_name",
            "oid", "tid", "upn", "unique_name", "tfp", "acr"
        };

        return wellKnownClaims.Contains(claimType);
    }
}
