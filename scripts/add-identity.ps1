# add-identity.ps1

## if starting with new app...simple use the --auth individual option

dotnet new webapp -o src/mySqlAuth8 -f net8.0 --auth individual
dotnet sln add src/mySqlAuthApp



# Add SQL Identity to app

dotnet add src/myVideos package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add src/myVideos package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/myVideos package Microsoft.EntityFrameworkCore.Tools

dotnet add src/myVideos package Microsoft.AspNetCore.Identity.UI

