// Log Analytics module
// Provides monitoring and diagnostics capabilities

param logAnalyticsName string
param location string = resourceGroup().location

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Output for use by other modules
output logAnalyticsId string = logAnalytics.id
output logAnalyticsName string = logAnalytics.name
output customerId string = logAnalytics.properties.customerId
