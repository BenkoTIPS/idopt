# ✨ Identity Options in Action with .NET Aspire - Demo Session Summary

## 🔍 Overview

This session demonstrates a progressive journey through identity options in .NET applications using **.NET Aspire**, showing real working examples of five different identity strategies. We progressively introduce authentication into the same base application, from no auth to self-hosted and cloud-hosted identity providers.

## 📚 Learning Objectives

* Understand identity fundamentals: local, PaaS, federated, and self-hosted options
* Learn how to configure, run, and deploy identity-aware .NET apps
* Use .NET Aspire to model service composition and infrastructure
* Visualize the claims and user context in each identity model

---

## 🎨 Demo Flow Summary

1. **No Auth + Claims Principal**: Start with a basic app that manually adds claims to the user context.
2. **EasyAuth**: Show how Azure App Service's built-in authentication simplifies identity management
1. **SQL Identity**: Introduce ASP.NET Identity with Entity Framework Core, demonstrating local user management.
1. **Azure AD B2C**: Configure federated login with Azure AD B2C, showcasing social and local accounts.
1. **Self-Hosted (Keycloak)**: Deploy a self-hosted Keycloak instance


## Solution structure

code in /src folder. Infrastructure in /infra folder. Create a dotnet aspire solution with projects for each demo.



| Demo | Title                          | Focus                                           | Duration |
| ---- | ------------------------------ | ----------------------------------------------- | -------- |
| 1    | **No Auth + Claims Principal** | Basic login page adds claim manually            | 8 min    |
| 2    | **EasyAuth**                   | Azure App Service built-in auth (headers only)  | 8 min    |
| 3    | **SQL Identity**               | ASP.NET Identity with EF Core + schema demo     | 12 min   |
| 4    | **Azure AD B2C**               | Federated login, social + user flow config      | 12 min   |
| 5    | **Self-Hosted (Keycloak)**     | Token issuing via containerized identity server | 10 min   |

Transition, code views, and wrap-up: **10 minutes**

---

## 🔧 CLI Scripting - Project Setup

```powershell
# Set base app name
$appName = "idopt"

# Install Aspire templates if needed
dotnet workload install aspire
dotnet new install Microsoft.DotNet.Aspire.Templates

# Create Aspire solution
dotnet new aspire -n $appName
cd $appName

# Create all projects
dotnet new web -n App.Basic

dotnet new webapp --auth Individual -n App.SqlIdentity

dotnet new web -n App.EasyAuth

dotnet new webapp -n App.B2C

dotnet new web -n App.SelfHostedAuth

# Add to solution
dotnet sln add App.*
```

---

## 🪤 Azure CLI / AZD Deployment

### For SQL Identity (Optional SQL Deployment)

```powershell
az sql server create `
  --name "$appName-sql-server" `
  --resource-group "rg-$appName" `
  --location "eastus" `
  --admin-user "sqladmin" `
  --admin-password "<Password123>"

az sql db create `
  --name "identitydb" `
  --server "$appName-sql-server" `
  --resource-group "rg-$appName" `
  --service-objective S0
```

### For Azure App Service + EasyAuth

```powershell
az webapp create `
  --name "$appName-easyauth" `
  --resource-group "rg-$appName" `
  --plan "$appName-appservice-plan" `
  --runtime "DOTNET|8.0"

az webapp auth update `
  --name "$appName-easyauth" `
  --resource-group "rg-$appName" `
  --enabled true `
  --action LoginWithAzureActiveDirectory
```

### For Azure AD B2C

```powershell
azd init --template azure-ad-b2c/dotnet
azd up
```

> This sets up a B2C tenant, app registration, and deploys the B2C-ready app with working auth.

---

## 🛫 Infrastructure Checklist

| Component                | Purpose                                              |
| ------------------------ | ---------------------------------------------------- |
| Azure SQL Database       | Optional for deploying SQL Identity app              |
| Azure App Service Plan   | Used for hosting EasyAuth & optionally B2C apps      |
| Azure AD B2C Tenant      | Federated identity & social login provider           |
| Keycloak Docker Image    | Containerized identity server (locally or in Aspire) |
| .NET Aspire Host         | Orchestrates the apps and services together          |
| GitHub Codespaces (opt.) | Optional demo portability                            |

---

Would you like automated Aspire `AppHost` wiring for these services in your final solution?
