# GitHub Copilot Instructions for Identity Options Demo Repository

## Project Overview

This is a **BenkoTIPS educational demo repository** showcasing 5 different identity/authentication strategies in .NET applications using **.NET Aspire** for orchestration. Each approach demonstrates a different identity pattern from simple cookie auth to federated identity providers.

## Architecture Pattern: Multi-App Identity Showcase

### Core Structure
- **Aspire Host** (`src/IdOpt.AppHost/`): Orchestrates all demo apps and infrastructure
- **Service Defaults** (`src/IdOpt.ServiceDefaults/`): Shared Aspire configuration for telemetry, health checks
- **5 Demo Apps**: Each implements the same base functionality with different identity strategies

### Identity Strategy Implementations

```csharp
// Key Pattern: Dual-mode database configuration (SqlIdentity app)
var aspireConnectionExists = builder.Configuration.GetConnectionString("identitydb") != null;
if (aspireConnectionExists) {
    builder.AddSqlServerDbContext<ApplicationDbContext>("identitydb"); // Aspire mode
} else {
    builder.Services.AddDbContext<ApplicationDbContext>(options => 
        options.UseSqlite(connectionString)); // Standalone mode
}
```

| Demo App | Pattern | Key Technology |
|----------|---------|----------------|
| `myApp.Simple` | Manual cookie auth | Custom login page with hardcoded claims |
| `myApp.SqlIdentity` | ASP.NET Identity | EF Core + dual DB support (SQLite/SQL Server) |
| `myApp.EasyAuth` | Ambassador pattern | Azure App Service built-in auth headers |
| `myApp.B2C` | Federated identity | Azure AD B2C social/local accounts |
| `myApp.KeyCloak` | Self-hosted IdP | Containerized Keycloak server |

### Aspire Service Dependencies

```csharp
// Infrastructure as Code in Program.cs
var sqlServer = builder.AddSqlServer("sqlserver").WithDataVolume().AddDatabase("identitydb");
builder.AddProject<Projects.myApp_SqlIdentity>("myapp-sqlidentity")
    .WithHttpsEndpoint()
    .WithReference(sqlServer); // Service dependency injection
```

## Critical Development Workflows

### 🚀 **Primary Debug Flow: F5 to Launch Everything**
- Press F5 → Runs "Launch Aspire Host (HTTPS)" configuration
- Automatically builds all projects and starts Aspire dashboard
- All 5 demo apps run simultaneously with infrastructure dependencies
- **Key insight**: Individual apps can run standalone OR within Aspire orchestration

### 🔧 **Dual-Mode Operation Pattern**
Each app detects its execution context:
```csharp
builder.AddServiceDefaults(); // Always call this first for Aspire integration
var aspireMode = builder.Configuration.GetConnectionString("identitydb") != null;
```

### 📊 **Database Migration Strategy**
- **SqlIdentity app**: Separate migration folders for SQLite vs SQL Server
- `Migrations/Sqlite/` - for standalone mode
- `Migrations/SqlServer/` - for Aspire container mode
- Aspire automatically applies migrations on container startup

## Project-Specific Conventions

### Service Discovery Pattern
```csharp
// All apps must call this for Aspire integration
builder.AddServiceDefaults();
// This enables: telemetry, health checks, service discovery, configuration
```

### HTTPS-Only Configuration
All Aspire projects use `.WithHttpsEndpoint()` - no HTTP fallbacks for security demo purposes.

### Naming Convention
- `myApp.*` prefix for demo applications
- `IdOpt.*` prefix for infrastructure (AppHost, ServiceDefaults)
- Resource names in Aspire use kebab-case: `"myapp-simple"`

## Integration Points & Dependencies

### **Aspire Service References**
- SQL Server container shared between AppHost and SqlIdentity app
- Service discovery automatically injects connection strings
- Volume persistence for database data across container restarts

### **Configuration Cascade**
1. Aspire AppHost defines infrastructure and service topology
2. ServiceDefaults provides common telemetry/health check configuration  
3. Individual apps detect Aspire vs standalone via connection string presence
4. Apps gracefully degrade (SQLite) when running outside Aspire

### **Claims & User Context**
Each demo shows different claim sources:
- Simple: Hardcoded claims in cookie
- SqlIdentity: EF Core user store
- EasyAuth: HTTP headers from App Service
- B2C: JWT tokens from federated provider
- KeyCloak: OIDC token claims

## Commands & Tools

```bash
# Build entire solution
dotnet build src/IdOpt25.sln

# Run individual app standalone (uses SQLite)
dotnet run --project src/myApp.SqlIdentity

# Create EF migrations (provider-specific)
dotnet ef migrations add InitialCreate --output-dir Migrations/Sqlite
dotnet ef migrations add InitialCreate --output-dir Migrations/SqlServer

# Aspire dashboard access
# Automatically opens when F5 debugging, or manually browse to Aspire host URL
```

## Key Files to Understand Project Context

- `demos.md` - Session overview and learning objectives
- `src/IdOpt.AppHost/Program.cs` - Complete service topology
- `src/myApp.SqlIdentity/Program.cs` - Dual-mode database pattern example
- `.vscode/launch.json` - F5 debug configuration for full-stack launch
- `src/IdOpt.ServiceDefaults/Extensions.cs` - Shared Aspire services setup

When working on this codebase, always consider whether changes affect standalone mode, Aspire mode, or both execution contexts.
