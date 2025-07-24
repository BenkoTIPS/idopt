// Shared infrastructure deployment
// - App Service Plan
// - Key Vault (shared)
// - Log Analytics Workspace

targetScope = 'subscription'

param envName string
param location string = 'CentralUS'

// Resource Group for shared resources
resource sharedRg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: '${envName}-shared-rg'
  location: location
}

// Deploy App Service Plan
module appServicePlan 'modules/appServicePlan.bicep' = {
  name: 'appServicePlanDeployment'
  scope: sharedRg
  params: {
    planName: '${envName}-shared-plan'
    location: location
    skuName: 'S1'
    skuCapacity: 1
  }
}

// Deploy Log Analytics Workspace
module logAnalytics 'modules/logAnalytics.bicep' = {
  name: 'logAnalyticsDeployment'
  scope: sharedRg
  params: {
    logAnalyticsName: '${envName}-shared-logs'
    location: location
  }
}

// Deploy shared Key Vault
module sharedKeyVault 'modules/keyVault.bicep' = {
  name: 'sharedKeyVaultDeployment'
  scope: sharedRg
  params: {
    keyVaultName: '${envName}-shared-kv'
    location: location
    tenantId: subscription().tenantId
  }
}

// Outputs for use by main deployment
output sharedRgName string = sharedRg.name
output planId string = appServicePlan.outputs.planId
output planName string = appServicePlan.outputs.planName
output logAnalyticsId string = logAnalytics.outputs.logAnalyticsId
output logAnalyticsName string = logAnalytics.outputs.logAnalyticsName
output customerId string = logAnalytics.outputs.customerId
output sharedKeyVaultId string = sharedKeyVault.outputs.keyVaultId
output sharedKeyVaultName string = sharedKeyVault.outputs.keyVaultName
output sharedKeyVaultUri string = sharedKeyVault.outputs.keyVaultUri

