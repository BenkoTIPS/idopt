// Deploy Azure App Service with modular infrastructure
// References existing shared infrastructure, then deploys application-specific resources
// az deployment sub create --location centralus -f infra/bicep/main.bicep --parameters envName=bnk25

targetScope = 'subscription'

param envName string // EnvName used for the shared infrastructure deployment

var appName string = 'idopt'
var loc string = 'CentralUS'

// Reference existing shared infrastructure
resource sharedRg 'Microsoft.Resources/resourceGroups@2021-04-01' existing = {
  name: '${envName}-shared-rg'
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
  name: '${envName}-${appName}-rg'
  location: loc
}

// Deploy application-specific infrastructure
module site 'mySite.bicep' = {
  name: 'siteDeployment'
  scope: rg
  params: {
    appName: appName
    envName: envName
    planId: existingPlan.id
    logAnalyticsId: existingLogAnalytics.id
    sharedKeyVaultName: existingSharedKeyVault.name
    sharedResourceGroupName: sharedRg.name
  }
}

// Output key information
output sharedRgName string = sharedRg.name
output rgName string = rg.name
output siteName string = site.outputs.siteName
output planName string = existingPlan.name
output storageName string = site.outputs.storageName
output sharedKeyVaultName string = existingSharedKeyVault.name
output appInsightsName string = site.outputs.appInsightsName
output siteUrl string = site.outputs.siteUrl
