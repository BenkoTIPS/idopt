# Deployment Guide

## Overview

The infrastructure is split into **two independent deployments**:

### 1. Shared Infrastructure (`shared.bicep`) - Deploy Once, Use Many
- **App Service Plan**: Shared compute resources across projects
- **Log Analytics Workspace**: Centralized logging and monitoring
- **Shared Key Vault**: Store secrets that can be shared across applications
- **Resource Group**: `{sharedEnvName}-shared-rg`
- **Independent**: Can be deployed and managed separately from applications

### 2. Application Infrastructure (`main.bicep`) - Project Specific  
- **Web Application**: The myVideos web app
- **Storage Account**: For blob storage
- **Key Vault Access**: Uses shared Key Vault with application-specific secrets
- **Application Insights**: Connected to shared Log Analytics
- **Resource Group**: `{envName}-{appName}-rg`
- **Dependent**: References existing shared infrastructure (will fail if shared doesn't exist)

## Deployment Commands

### 1. Deploy Shared Infrastructure (One Time Per Environment)

```bash
az deployment sub create \
  --location centralus \
  --template-file infra/bicep/shared.bicep \
  --parameters envName=bnk25
```

```powershell-interactive
az deployment sub create `
  --location centralus `
  --template-file infra/bicep/shared.bicep `
  --parameters envName=bnk25
```


### 2. Deploy Application Infrastructure (References Existing Shared)

```bash
az deployment sub create \
  --location centralus \
  --template-file infra/bicep/main.bicep \
  --parameters envName=dad25 sharedEnvName=dad25
```

```powershell-interactive
az deployment sub create `
  --location centralus `
  --template-file infra/bicep/main.bicep `
  --parameters envName=dad25 sharedEnvName=dad25
```


**Important**: The `sharedEnvName` parameter must match the `envName` used when deploying the shared infrastructure. If the shared resources don't exist, the deployment will fail with clear error messages.

## Troubleshooting Common Issues

### 1. Missing sharedEnvName Parameter
**Error**: Missing required parameter when running main.bicep
**Solution**: Always provide both `envName` and `sharedEnvName` parameters:
```bash
az deployment sub create --location centralus --template-file infra/bicep/main.bicep --parameters envName=dad25 sharedEnvName=dad25
```

### 2. Shared Resources Not Found
**Error**: `ParentResourceNotFound` - Key Vault not found
**Solution**: Ensure shared infrastructure is deployed first with the same environment name:
```bash
# Deploy shared first
az deployment sub create --location centralus --template-file infra/bicep/shared.bicep --parameters envName=dad25
# Then deploy application
az deployment sub create --location centralus --template-file infra/bicep/main.bicep --parameters envName=dad25 sharedEnvName=dad25
```

### 3. Parameter Mismatch
**Error**: Resources not found even though shared infrastructure exists
**Solution**: Verify the `sharedEnvName` parameter matches exactly the `envName` used for shared deployment.

## Example: Multiple Applications Sharing Infrastructure

```bash
# Deploy shared infrastructure once
az deployment sub create --location centralus --template-file infra/bicep/shared.bicep --parameters envName=prod

# Deploy multiple applications that use the same shared infrastructure
az deployment sub create --location centralus --template-file infra/bicep/main.bicep --parameters envName=myvideos sharedEnvName=prod
az deployment sub create --location centralus --template-file infra/bicep/main.bicep --parameters envName=photos sharedEnvName=prod
az deployment sub create --location centralus --template-file infra/bicep/main.bicep --parameters envName=docs sharedEnvName=prod
```

## Module Structure
- `appServicePlan.bicep` - App Service Plan module
- `keyVault.bicep` - Key Vault module (used only for shared Key Vault)
- `keyVaultAccessPolicy.bicep` - Key Vault access policy module
- `keyVaultSecret.bicep` - Key Vault secrets module
- `logAnalytics.bicep` - Log Analytics workspace module
- `mySite.bicep` - Main application resources
- `shared.bicep` - Shared infrastructure deployment
- `main.bicep` - Application infrastructure deployment

## Key Changes Made
1. **Separated concerns**: Shared resources vs application-specific resources
2. **Modularized components**: Each Azure service type in its own module
3. **Added monitoring**: Log Analytics workspace and Application Insights
4. **Maintained dependencies**: Proper deployment order and resource references
5. **Minimal changes**: Preserved existing functionality while improving structure
