git init
git add .
git commit -m "Initial commit"
git branch -M main
git remote add origin https://github.com/BenkoTIPS/idopt.git
git push -u origin main

# Add branch for Aspire + Identity demos
git checkout -b aspire-identity


## Create Aspire Web App with a solution name $appName and run locally
# Set base app name
$appName = "IdOpt"

# Install Aspire templates if needed
dotnet workload install aspire
## dotnet new install Microsoft.DotNet.Aspire.Templates

# Create Aspire solution in /src
mkdir ./src
cd ./src
dotnet new aspire -n IdOpt  -f net8.0

# Create all projects
dotnet new webapp -n myApp.Simple
dotnet sln add myApp.Simple/myApp.Simple.csproj

dotnet new webapp --auth Individual -n myApp.SqlIdentity
dotnet sln add myApp.SqlIdentity/myApp.SqlIdentity.csproj

dotnet new webapp -n myApp.EasyAuth
dotnet sln add myApp.EasyAuth/myApp.EasyAuth.csproj

dotnet new webapp -n myApp.B2C
dotnet sln add myApp.B2C/myApp.B2C.csproj

dotnet new web -n myApp.KeyCloak
dotnet sln add myApp.KeyCloak/myApp.KeyCloak.csproj

# Add to solution
