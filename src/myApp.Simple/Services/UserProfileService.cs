using myApp.Simple.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace myApp.Simple.Services
{
    public interface IUserProfileService
    {
        Task<UserProfile?> GetUserProfileAsync(string userId);
        Task SaveUserProfileAsync(UserProfile profile);
        Task UpdateLastLoginDateAsync(string userId);
    }

    public class UserProfileService : IUserProfileService
    {
        private readonly ILogger<UserProfileService> _logger;
        // In-memory storage for the simple demo - in a real app this would be a database
        private static readonly Dictionary<string, UserProfile> _profiles = new();

        public UserProfileService(ILogger<UserProfileService> logger)
        {
            _logger = logger;
        }

        public Task<UserProfile?> GetUserProfileAsync(string userId)
        {
            _logger.LogInformation("Retrieving user profile for {UserId}", userId);
            
            _profiles.TryGetValue(userId, out var profile);
            return Task.FromResult(profile);
        }

        public Task SaveUserProfileAsync(UserProfile profile)
        {
            _logger.LogInformation("Saving user profile for {UserId}", profile.UserId);
            
            _profiles[profile.UserId] = profile;
            return Task.CompletedTask;
        }

        public async Task UpdateLastLoginDateAsync(string userId)
        {
            _logger.LogInformation("Updating last login date for {UserId}", userId);
            
            var profile = await GetUserProfileAsync(userId);
            if (profile != null)
            {
                profile.LastLoginDate = DateTime.UtcNow;
                await SaveUserProfileAsync(profile);
            }
        }
    }
}
