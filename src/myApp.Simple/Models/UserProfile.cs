using System;

namespace myApp.Simple.Models
{
    public class UserProfile
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PreferredStorageType { get; set; } = string.Empty;
        public string IdentityProvider { get; set; } = string.Empty;
        public bool IsMigrated { get; set; }
        public DateTime MigrationDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
    }
}
