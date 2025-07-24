using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace myApp.SqlIdentity.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configure different migration paths for different providers
        if (Database.IsSqlServer())
        {
            builder.HasDefaultSchema("dbo");
        }
    }
}
