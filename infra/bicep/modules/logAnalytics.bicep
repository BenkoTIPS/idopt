// Log Analytics Workspace module
// Provides centralized logging for applications and infrastructure

param logAnalyticsName string
param location string = resourceGroup().location
param skuName string = 'PerGB2018'
param retentionInDays int = 30

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: skuName
    }
    retentionInDays: retentionInDays
  }
}

// Outputs for use by other modules
output logAnalyticsId string = logAnalytics.id
output logAnalyticsName string = logAnalytics.name
output customerId string = logAnalytics.properties.customerId
