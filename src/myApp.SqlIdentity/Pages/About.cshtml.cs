using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using myApp.SqlIdentity.Data;
using Microsoft.AspNetCore.Authorization;

namespace myApp.SqlIdentity.Pages;

[Authorize]
public class AboutModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AboutModel(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public string DatabaseProvider { get; set; } = string.Empty;
    public string HostingMode { get; set; } = string.Empty;
    public string MaskedConnectionString { get; set; } = string.Empty;

    public void OnGet()
    {
        // Determine database provider
        DatabaseProvider = _context.Database.IsSqlServer() ? "SQL Server (Container)" : "SQLite (Local File)";

        // Determine hosting mode
        var aspireConnectionExists = _configuration.GetConnectionString("identitydb") != null;
        HostingMode = aspireConnectionExists ? "Aspire Orchestrated" : "Standalone";

        // Get masked connection string
        var connectionString = aspireConnectionExists
            ? _configuration.GetConnectionString("identitydb")
            : _configuration.GetConnectionString("DefaultConnection");

        MaskedConnectionString = MaskConnectionString(connectionString ?? "Not configured");
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "Not configured";

        // Mask sensitive information in connection string
        var masked = connectionString;

        // Mask passwords
        if (masked.Contains("Password=", StringComparison.OrdinalIgnoreCase))
        {
            var passwordIndex = masked.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
            var endIndex = masked.IndexOfAny([';', ' '], passwordIndex);
            if (endIndex == -1) endIndex = masked.Length;

            var passwordPart = masked.Substring(passwordIndex, endIndex - passwordIndex);
            masked = masked.Replace(passwordPart, "Password=***");
        }

        // Mask user IDs for SQL Server
        if (masked.Contains("User Id=", StringComparison.OrdinalIgnoreCase))
        {
            var userIndex = masked.IndexOf("User Id=", StringComparison.OrdinalIgnoreCase);
            var endIndex = masked.IndexOfAny([';', ' '], userIndex);
            if (endIndex == -1) endIndex = masked.Length;

            var userPart = masked.Substring(userIndex, endIndex - userIndex);
            var splitParts = userPart.Split('=');
            if (splitParts.Length > 1)
            {
                var userValue = splitParts[1];
                masked = masked.Replace(userPart, $"User Id={userValue[..Math.Min(2, userValue.Length)]}***");
            }
        }

        return masked;
    }
}
