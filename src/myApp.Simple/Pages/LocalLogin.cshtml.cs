using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using myApp.Simple.Models;
using myApp.Simple.Services;

namespace myApp.Simple.Pages
{
    public class LocalLoginModel : PageModel
    {
        private readonly IUserProfileService _userProfileService;
        private readonly ILogger<LocalLoginModel> _logger;

        public LocalLoginModel(
            IUserProfileService userProfileService,
            ILogger<LocalLoginModel> logger)
        {
            _userProfileService = userProfileService;
            _logger = logger;
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                ModelState.AddModelError("Username", "Username is required.");
                return Page();
            }

            // If no email is provided, construct one from the username
            string userEmail = !string.IsNullOrWhiteSpace(Email) ? 
                Email : 
                Username.Contains("@") ? Username : Username + "@local.com";

            // Create the authentication cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, Username),
                new Claim(ClaimTypes.Email, userEmail)
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("CookieAuth", principal);
            
            // Ensure user profile exists in Azure Table Storage BEFORE redirecting
            _logger.LogInformation("Creating or verifying UserProfile for {Username} with email {Email}", 
                Username, userEmail);
                
            bool isNewUser = false;
            
            try
            {
                if (_userProfileService != null)
                {
                    // Check if user profile exists
                    var profile = await _userProfileService.GetUserProfileAsync(Username);
                    isNewUser = profile == null;
                    
                    if (isNewUser)
                    {
                        // Create a new profile for first-time users
                        var newProfile = new UserProfile
                        {
                            UserId = Username,
                            DisplayName = Username,
                            Email = userEmail,
                            PhoneNumber = string.Empty,
                            PreferredStorageType = "Azure",
                            IdentityProvider = "SimpleAuth",
                            IsMigrated = true,
                            MigrationDate = DateTime.UtcNow,
                            CreatedDate = DateTime.UtcNow,
                            LastLoginDate = DateTime.UtcNow,
                            PartitionKey = "Users",
                            RowKey = Username
                        };
                        
                        await _userProfileService.SaveUserProfileAsync(newProfile);
                        _logger.LogInformation("Created new user profile for {Username}", Username);
                    }
                    else if (profile != null)
                    {
                        // Update the existing profile if needed
                        if (string.IsNullOrEmpty(profile.Email))
                        {
                            profile.Email = userEmail;
                            await _userProfileService.SaveUserProfileAsync(profile);
                            _logger.LogInformation("Updated email for existing user {Username}", Username);
                        }
                        
                        // Update the last login date
                        await _userProfileService.UpdateLastLoginDateAsync(Username);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring user profile exists for {Username}", Username);
                // Continue to redirect even if there's an issue with the profile
            }

            // Redirect new users to complete their profile, others go to the main page
            if (isNewUser)
            {
                return RedirectToPage("/About", new { newUser = true });
            }
            
            return RedirectToPage("/Index");
        }
    }
}
