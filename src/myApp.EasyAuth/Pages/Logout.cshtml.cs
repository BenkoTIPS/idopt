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
    public class LogoutModel : PageModel
    {
        private readonly IConfiguration _config;
        
        public LogoutModel(IConfiguration config)
        {
            _config = config;
        }
        
        public async Task<IActionResult> OnGetAsync()
        {
            // Support GET requests for logout as well
            return await HandleLogout();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            return await HandleLogout();
        }
        
        private async Task<IActionResult> HandleLogout()
        {
            // Get and log the user Id being logged out
            var userId = User.Identity?.Name;
            var logger = HttpContext.RequestServices.GetService<ILogger<LogoutModel>>();
            logger?.LogInformation("Processing logout for user: {UserId}", userId);

            var authProvider = _config.GetValue<string>("AuthProvider", "EasyAuth");
            
            // Set security headers regardless of auth provider
            // These headers prevent browsers from caching the response
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");
            Response.Headers.Append("Clear-Site-Data", "\"cache\", \"cookies\", \"storage\"");
            
            // Add a random cookie that expires immediately to force cookie jar refresh
            Response.Cookies.Append(
                "logout-trigger", 
                Guid.NewGuid().ToString(), 
                new CookieOptions { 
                    Expires = DateTimeOffset.UtcNow.AddDays(-1),
                    SameSite = SameSiteMode.Strict,
                    Secure = true,
                    HttpOnly = true
                });
            
            // Log the auth provider used for debugging
            logger?.LogInformation("Using authentication provider: {AuthProvider}", authProvider);
            
            // IMPORTANT: For Simple auth, we should ONLY use cookie auth signout
            // Put Simple auth first in the condition chain since that's what we're running with
            if (authProvider.Equals("Simple", StringComparison.OrdinalIgnoreCase))
            {
                logger?.LogInformation("Executing simple authentication logout for user: {UserId}", userId);
                
                try {
                    // Sign out from the cookie authentication scheme ONLY
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    
                    // For extra safety, delete all auth cookies manually
                    foreach (var cookie in Request.Cookies.Keys)
                    {
                        if (cookie.StartsWith(".AspNetCore.") || 
                            cookie.Contains("Identity") || 
                            cookie.Contains("Auth"))
                        {
                            Response.Cookies.Delete(cookie);
                        }
                    }
                    
                    // Return to the home page
                    return RedirectToPage("/Index");
                }
                catch (Exception ex) {
                    logger?.LogError(ex, "Error during Simple auth logout for user {UserId}", userId);
                    // Try to delete cookies and redirect anyway
                    return RedirectToPage("/Index");
                }
            }
            else if (authProvider.Equals("B2C", StringComparison.OrdinalIgnoreCase))
            {
                logger?.LogInformation("Executing B2C logout procedure for user: {UserId}", userId);
                
                try
                {
                    // Sign out from the cookies authentication scheme first
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    
                    // Get the sign-out redirect URI from config if available
                    var signOutRedirectUri = _config.GetValue<string>("AzureB2C:SignOutUrl");
                    
                    // Sign out from B2C with all appropriate properties set
                    var authProps = new AuthenticationProperties
                    {
                        RedirectUri = Url.Page("/Index"),
                        AllowRefresh = true
                    };
                    
                    if (!string.IsNullOrEmpty(signOutRedirectUri))
                    {
                        authProps.RedirectUri = signOutRedirectUri;
                    }
                    
                    // Log the sign out action
                    logger?.LogInformation("Signing out from AzureB2C scheme for user: {UserId}", userId);
                    
                    // This will redirect to B2C's logout endpoint
                    return SignOut(authProps, "AzureB2C", CookieAuthenticationDefaults.AuthenticationScheme);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error during B2C logout for user {UserId}", userId);
                    // Fall back to cookie deletion and redirect
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToPage("/Index");
                }
            }
            else if (authProvider.Equals("Easy", StringComparison.OrdinalIgnoreCase))
            {
                logger?.LogInformation("Executing Easy Auth logout for user: {UserId}", userId);
                
                // Check if we're running locally or in Azure
                var isLocal = HttpContext.Request.Host.Host.Contains("localhost") || 
                             HttpContext.Request.Host.Host.StartsWith("127.0.0.1") ||
                             string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
                
                if (isLocal)
                {
                    // For local development, clear session and redirect to home
                    if (HttpContext.Session.IsAvailable)
                    {
                        HttpContext.Session.Clear();
                    }
                    // Only sign out from Cookie scheme - EasyAuth doesn't support sign-out
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    logger?.LogInformation("Local logout completed for user: {UserId}", userId);
                    return RedirectToPage("/Index");
                }
                else
                {
                    // For Azure App Service, use the real EasyAuth logout endpoint
                    return Redirect("/.auth/logout?post_logout_redirect_uri=/");
                }
            }
            else
            {
                logger?.LogInformation("Executing EasyAuth logout for user: {UserId}", userId);
                
                // Check if we're running locally or in Azure
                var isLocal = HttpContext.Request.Host.Host.Contains("localhost") || 
                             HttpContext.Request.Host.Host.StartsWith("127.0.0.1") ||
                             string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
                
                if (isLocal)
                {
                    // For local development, clear session and redirect to home
                    if (HttpContext.Session.IsAvailable)
                    {
                        HttpContext.Session.Clear();
                    }
                    // Only sign out from Cookie scheme - EasyAuth doesn't support sign-out
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    logger?.LogInformation("Local logout completed for user: {UserId}", userId);
                    return RedirectToPage("/Index");
                }
                else
                {
                    // EasyAuth logout - first sign out locally then redirect to EasyAuth endpoint
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    
                    // Add cache-busting parameter and post_logout_redirect_uri to the logout URL
                    var cacheBuster = DateTime.UtcNow.Ticks;
                    var logoutUrl = $"{Request.Scheme}://{Request.Host}/.auth/logout?post_logout_redirect_uri=/&t={cacheBuster}";
                    
                    return Redirect(logoutUrl);
                }
            }
        }
    }
}