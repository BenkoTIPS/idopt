using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MyApp.EasyAuth.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly ILogger<LoginModel> _logger;
        
        public LoginModel(IConfiguration config, ILogger<LoginModel> logger)
        {
            _config = config;
            _logger = logger;
        }
          [TempData]
        public string? ErrorMessage { get; set; }
        
        public bool LocalAuthEnabled => _config.GetValue<bool>("UseLocalAuth", false);
        
        public IActionResult OnGet()
        {
            // Clear any previous error messages
            ErrorMessage = null;
            
            // Display the authentication provider selection page
            return Page();
        }        public async Task<IActionResult> OnGetInitiateLoginAsync(string? provider = null)
        {
            _logger.LogInformation("Initiating new login process with provider: {Provider}", provider ?? "default");
            
            // Set cache control headers - but do NOT use Clear-Site-Data which removes cookies needed for correlation
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");
            
            // Only sign out from application authentication, but keep correlation cookies
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            // If no provider is specified, use the configured default
            if (string.IsNullOrEmpty(provider))
            {
                provider = _config.GetValue<string>("AuthProvider", "EasyAuth");
            }
            
            // Convert the provider to lowercase for case-insensitive comparison
            provider = provider.ToLowerInvariant();
            
            if (provider == "b2c")
            {
                _logger.LogInformation("Redirecting to B2C authentication scheme");
                
                try
                {
                    // For B2C, redirect to the challenge endpoint with proper properties
                    // Critical: Use properly configured AuthenticationProperties for B2C flow
                    var authProps = new AuthenticationProperties
                    {
                        RedirectUri = Url.Page("/About"),
                        // Ensure we allow refresh for the claims
                        AllowRefresh = true,
                        // This ensures user gets the proper policy/flow in B2C
                        IsPersistent = true
                    };
                    
                    _logger.LogInformation("Initiating B2C Challenge with RedirectUri: {RedirectUri}", authProps.RedirectUri);
                    return Challenge(authProps, "AzureB2C");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error initiating B2C authentication");
                    ErrorMessage = "Authentication service unavailable. Please try again later.";
                    return RedirectToPage();
                }
            }
            else if (provider == "simple")
            {
                _logger.LogInformation("Redirecting to local login page");
                return RedirectToPage("/LocalLogin");
            }            else if (provider == "aad" || provider == "google" || provider == "facebook" || provider == "x")
            {
                _logger.LogInformation("Redirecting to {Provider} EasyAuth login", provider);
                
                // Map 'x' to 'twitter' for EasyAuth compatibility
                var authProvider = provider == "x" ? "twitter" : provider;
                
                // Check if we're running locally (development) or in Azure
                var isLocal = HttpContext.Request.Host.Host.Contains("localhost") || 
                             HttpContext.Request.Host.Host.StartsWith("127.0.0.1") ||
                             string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
                
                if (isLocal)
                {
                    // For local development, redirect to our simulation endpoint
                    var returnUrl = Url.Page("/About") ?? "/";
                    var simulationUrl = $"/LocalSimulation?provider={authProvider}&returnUrl={Uri.EscapeDataString(returnUrl)}";
                    _logger.LogInformation("Local development detected, redirecting to simulation: {Url}", simulationUrl);
                    return Redirect(simulationUrl);
                }
                else
                {
                    // For Azure App Service, use the real EasyAuth endpoints
                    var cacheBuster = DateTime.UtcNow.Ticks;
                    var loginUrl = $"{Request.Scheme}://{Request.Host}/.auth/login/{authProvider}?post_login_redirect_uri={Url.Page("/About")}&t={cacheBuster}";
                    _logger.LogInformation("Azure deployment detected, redirecting to EasyAuth URL: {Url}", loginUrl);
                    return Redirect(loginUrl);
                }
            }
            else if (provider == "github")
            {
                // GitHub is not enabled
                _logger.LogWarning("Attempted to use disabled provider: {Provider}", provider);
                ErrorMessage = "This authentication provider is not currently available.";
                return RedirectToPage();
            }
            else
            {
                _logger.LogInformation("Redirecting to standard EasyAuth login");
                
                // Check if we're running locally or in Azure
                var isLocal = HttpContext.Request.Host.Host.Contains("localhost") || 
                             HttpContext.Request.Host.Host.StartsWith("127.0.0.1") ||
                             string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
                
                if (isLocal)
                {
                    // For local development, redirect to our simulation endpoint with default provider
                    var returnUrl = Url.Page("/About") ?? "/";
                    var simulationUrl = $"/LocalSimulation?provider=aad&returnUrl={Uri.EscapeDataString(returnUrl)}";
                    _logger.LogInformation("Local development detected, redirecting to simulation: {Url}", simulationUrl);
                    return Redirect(simulationUrl);
                }
                else
                {
                    // For Azure App Service, use the real EasyAuth endpoints
                    var cacheBuster = DateTime.UtcNow.Ticks;
                    var loginUrl = $"{Request.Scheme}://{Request.Host}/.auth/login?post_login_redirect_uri={Url.Page("/About")}&t={cacheBuster}";
                    return Redirect(loginUrl);
                }
            }
        }
    }
}
