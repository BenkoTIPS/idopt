// Deploy Azure App Service with modular infrastructure
// References existing shared infrastructure, then deploys application-specific resources
// az deployment sub create --location centralus -f infra/bicep/idopt.bicep --parameters envName=bnk

targetScope = 'subscription'

param envName string // EnvName used for the shared infrastructure deployment

// Application and naming configuration
var appName string = 'idopt'
var loc string = 'CentralUS'

// Resource naming variables
var rgName = '${envName}-${appName}-rg'
var sharedRgName = '${envName}-shared-rg'
var sqlServerName = '${envName}-${appName}-sql'
var sqlDatabaseName = '${appName}-db'
var siteName = '${envName}-${appName}-site'
var storageName = '${toLower(replace('${envName}${appName}','-',''))}store'
var appInsightsName = '${envName}-${appName}-insights'
var storageSecretName = 'storage-connection-${envName}-${appName}'
var sqlAdminPasswordSecretName = 'sql-admin-password-${envName}-${appName}'
var sqlConnectionSecretName = 'sql-connection-${envName}-${appName}'

// Reference existing shared infrastructure
resource sharedRg 'Microsoft.Resources/resourceGroups@2021-04-01' existing = {
  name: sharedRgName
}

// Reference existing App Service Plan
resource existingPlan 'Microsoft.Web/serverfarms@2020-12-01' existing = {
  scope: sharedRg
  name: '${envName}-shared-plan'
}

// Reference existing Log Analytics Workspace
resource existingLogAnalytics 'Microsoft.OperationalInsights/workspaces@2021-06-01' existing = {
  scope: sharedRg
  name: '${envName}-shared-logs'
}

// Reference existing shared Key Vault
resource existingSharedKeyVault 'Microsoft.KeyVault/vaults@2021-04-01-preview' existing = {
  scope: sharedRg
  name: '${envName}-shared-kv'
}

// Resource Group for application-specific resources
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: rgName
  location: loc
}

// Deploy SQL Server in the app resource group
module sqlServer 'modules/sqlServer.bicep' = {
  name: 'sqlServerDeployment'
  scope: rg
  params: {
    serverName: sqlServerName
    location: loc
    sqlAdminPassword: 'P@ssw0rd123!${uniqueString(envName, appName, subscription().subscriptionId)}'
  }
}

// Store SQL Admin password in shared Key Vault
module sqlAdminPasswordSecret 'modules/keyVaultSecret.bicep' = {
  name: 'sqlAdminPasswordSecretDeployment'
  scope: resourceGroup(sharedRg.name)
  params: {
    keyVaultName: existingSharedKeyVault.name
    secretName: sqlAdminPasswordSecretName
    secretValue: 'P@ssw0rd123!${uniqueString(envName, appName, subscription().subscriptionId)}'
  }
}

// Deploy application-specific infrastructure (moved from mySite.bicep module)
module appResources 'modules/appResources.bicep' = {
  name: 'appResourcesDeployment'
  scope: rg
  params: {
    siteName: siteName
    storageName: storageName
    appInsightsName: appInsightsName
    storageSecretName: storageSecretName
    planId: existingPlan.id
    logAnalyticsId: existingLogAnalytics.id
    sharedKeyVaultName: existingSharedKeyVault.name
    sharedResourceGroupName: sharedRg.name
    location: loc
  }
}

// Deploy SQL Database for Identity demos (in same RG as SQL Server)
module sqlDatabase 'modules/sqlDatabase.bicep' = {
  name: 'sqlDatabaseDeployment'
  scope: rg
  params: {
    databaseName: sqlDatabaseName
    sqlServerName: sqlServer.outputs.serverName
    location: loc
    skuName: 'S0'
    skuTier: 'Standard'
  }
}

// Create connection string secret for SQL Database
module sqlConnectionSecret 'modules/keyVaultSecret.bicep' = {
  name: 'sqlConnectionSecretDeployment'
  scope: resourceGroup(sharedRg.name)
  params: {
    keyVaultName: existingSharedKeyVault.name
    secretName: sqlConnectionSecretName
    secretValue: 'Server=${sqlServer.outputs.serverFqdn};Database=${sqlDatabase.outputs.databaseName};User Id=sqladmin;Password=@Microsoft.KeyVault(VaultName=${existingSharedKeyVault.name};SecretName=${sqlAdminPasswordSecretName});Encrypt=True;Connection Timeout=30;'
  }
}

// Identity Provider Demo Slots
module simpleSlot 'modules/deploymentSlot.bicep' = {
  name: 'simpleSlotDeployment'
  scope: rg
  params: {
    siteName: appResources.outputs.siteName
    slotName: 'Simple'
    location: loc
    planId: existingPlan.id
    slotAppSettings: [
      {
        name: 'EnvName'
        value: 'Simple'
      }
      {
        name: 'FavoriteColor'
        value: 'Blue'
      }
      {
        name: 'IdentityProvider'
        value: 'None'
      }
      {
        name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
        value: appResources.outputs.appInsightsInstrumentationKey
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: appResources.outputs.appInsightsConnectionString
      }
      {
        name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
        value: '~3'
      }
    ]
    slotConnectionStrings: [
      {
        name: 'myStorage'
        value: appResources.outputs.storageConnection
        type: 'Custom'
      }
    ]
  }
}

module easySlot 'modules/deploymentSlot.bicep' = {
  name: 'easySlotDeployment'
  scope: rg
  params: {
    siteName: appResources.outputs.siteName
    slotName: 'Easy'
    location: loc
    planId: existingPlan.id
    slotAppSettings: [
      {
        name: 'EnvName'
        value: 'Easy'
      }
      {
        name: 'FavoriteColor'
        value: 'Green'
      }
      {
        name: 'IdentityProvider'
        value: 'ASP.NET Core Identity'
      }
      {
        name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
        value: appResources.outputs.appInsightsInstrumentationKey
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: appResources.outputs.appInsightsConnectionString
      }
      {
        name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
        value: '~3'
      }
    ]
    slotConnectionStrings: [
      {
        name: 'myStorage'
        value: appResources.outputs.storageConnection
        type: 'Custom'
      }
      // Future: Add SQL Database connection for ASP.NET Core Identity
      // {
      //   name: 'DefaultConnection'
      //   value: '@Microsoft.KeyVault(VaultName=${existingSharedKeyVault.name};SecretName=sql-connection-easy)'
      //   type: 'SQLAzure'
      // }
    ]
  }
}

module b2cSlot 'modules/deploymentSlot.bicep' = {
  name: 'b2cSlotDeployment'
  scope: rg
  params: {
    siteName: appResources.outputs.siteName
    slotName: 'B2C'
    location: loc
    planId: existingPlan.id
    slotAppSettings: [
      {
        name: 'EnvName'
        value: 'B2C'
      }
      {
        name: 'FavoriteColor'
        value: 'Red'
      }
      {
        name: 'IdentityProvider'
        value: 'Azure AD B2C'
      }
      {
        name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
        value: appResources.outputs.appInsightsInstrumentationKey
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: appResources.outputs.appInsightsConnectionString
      }
      {
        name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
        value: '~3'
      }
      // Future: Add B2C specific settings
      // {
      //   name: 'AzureAdB2C:Instance'
      //   value: 'https://your-tenant.b2clogin.com/'
      // }
      // {
      //   name: 'AzureAdB2C:ClientId'
      //   value: '@Microsoft.KeyVault(VaultName=${existingSharedKeyVault.name};SecretName=b2c-client-id)'
      // }
    ]
    slotConnectionStrings: [
      {
        name: 'myStorage'
        value: appResources.outputs.storageConnection
        type: 'Custom'
      }
    ]
  }
}

module keycloakSlot 'modules/deploymentSlot.bicep' = {
  name: 'keycloakSlotDeployment'
  scope: rg
  params: {
    siteName: appResources.outputs.siteName
    slotName: 'Keycloak'
    location: loc
    planId: existingPlan.id
    slotAppSettings: [
      {
        name: 'EnvName'
        value: 'Keycloak'
      }
      {
        name: 'FavoriteColor'
        value: 'Purple'
      }
      {
        name: 'IdentityProvider'
        value: 'Keycloak'
      }
      {
        name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
        value: appResources.outputs.appInsightsInstrumentationKey
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: appResources.outputs.appInsightsConnectionString
      }
      {
        name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
        value: '~3'
      }
      // Future: Add Keycloak specific settings
      // {
      //   name: 'Keycloak:ServerUrl'
      //   value: 'https://your-keycloak-server.com/'
      // }
      // {
      //   name: 'Keycloak:Realm'
      //   value: 'your-realm'
      // }
    ]
    slotConnectionStrings: [
      {
        name: 'myStorage'
        value: appResources.outputs.storageConnection
        type: 'Custom'
      }
      // Future: Add Keycloak database connection if needed
      // {
      //   name: 'KeycloakConnection'
      //   value: '@Microsoft.KeyVault(VaultName=${existingSharedKeyVault.name};SecretName=keycloak-db-connection)'
      //   type: 'SQLAzure'
      // }
    ]
  }
}

module sqlSlot 'modules/deploymentSlot.bicep' = {
  name: 'sqlSlotDeployment'
  scope: rg
  params: {
    siteName: appResources.outputs.siteName
    slotName: 'SQL'
    location: loc
    planId: existingPlan.id
    slotAppSettings: [
      {
        name: 'EnvName'
        value: 'SQL'
      }
      {
        name: 'FavoriteColor'
        value: 'Orange'
      }
      {
        name: 'IdentityProvider'
        value: 'SQL Server Authentication'
      }
      {
        name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
        value: appResources.outputs.appInsightsInstrumentationKey
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: appResources.outputs.appInsightsConnectionString
      }
      {
        name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
        value: '~3'
      }
    ]
    slotConnectionStrings: [
      {
        name: 'myStorage'
        value: appResources.outputs.storageConnection
        type: 'Custom'
      }
      {
        name: 'DefaultConnection'
        value: '@Microsoft.KeyVault(VaultName=${existingSharedKeyVault.name};SecretName=${sqlConnectionSecretName})'
        type: 'SQLAzure'
      }
    ]
  }
}

// Grant Key Vault access to all deployment slots (need to read storage connection and other secrets)
module simpleSlotKeyVaultAccess 'modules/keyVaultAccessPolicy.bicep' = {
  name: 'simpleSlotKeyVaultAccessDeployment'
  scope: resourceGroup(sharedRg.name)
  params: {
    keyVaultName: existingSharedKeyVault.name
    tenantId: subscription().tenantId
    objectId: simpleSlot.outputs.slotPrincipalId
  }
}

module easySlotKeyVaultAccess 'modules/keyVaultAccessPolicy.bicep' = {
  name: 'easySlotKeyVaultAccessDeployment'
  scope: resourceGroup(sharedRg.name)
  params: {
    keyVaultName: existingSharedKeyVault.name
    tenantId: subscription().tenantId
    objectId: easySlot.outputs.slotPrincipalId
  }
}

module b2cSlotKeyVaultAccess 'modules/keyVaultAccessPolicy.bicep' = {
  name: 'b2cSlotKeyVaultAccessDeployment'
  scope: resourceGroup(sharedRg.name)
  params: {
    keyVaultName: existingSharedKeyVault.name
    tenantId: subscription().tenantId
    objectId: b2cSlot.outputs.slotPrincipalId
  }
}

module keycloakSlotKeyVaultAccess 'modules/keyVaultAccessPolicy.bicep' = {
  name: 'keycloakSlotKeyVaultAccessDeployment'
  scope: resourceGroup(sharedRg.name)
  params: {
    keyVaultName: existingSharedKeyVault.name
    tenantId: subscription().tenantId
    objectId: keycloakSlot.outputs.slotPrincipalId
  }
}

// Grant Key Vault access to SQL slot (needs to read SQL connection string secret)
module sqlSlotKeyVaultAccess 'modules/keyVaultAccessPolicy.bicep' = {
  name: 'sqlSlotKeyVaultAccessDeployment'
  scope: resourceGroup(sharedRg.name)
  params: {
    keyVaultName: existingSharedKeyVault.name
    tenantId: subscription().tenantId
    objectId: sqlSlot.outputs.slotPrincipalId
  }
}

// Output key information
output sharedRgName string = sharedRg.name
output rgName string = rg.name
output siteName string = appResources.outputs.siteName
output planName string = existingPlan.name
output storageName string = appResources.outputs.storageName
output sharedKeyVaultName string = existingSharedKeyVault.name
output appInsightsName string = appResources.outputs.appInsightsName
output siteUrl string = appResources.outputs.siteUrl
