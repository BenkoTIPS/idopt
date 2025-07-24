// Deployment Slot module
// Creates a deployment slot with configurable app settings and connection strings

param siteName string
param slotName string
param location string = resourceGroup().location
param planId string
param slotAppSettings array = []
param slotConnectionStrings array = []

resource parentSite 'Microsoft.Web/sites@2020-12-01' existing = {
  name: siteName
}

resource deploymentSlot 'Microsoft.Web/sites/slots@2020-12-01' = {
  name: slotName
  parent: parentSite
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: planId
    siteConfig: {
      use32BitWorkerProcess: false
      appSettings: union([
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ], slotAppSettings)
    }
  }
}

// Configure connection strings separately if provided
resource slotConnectionStringsConfig 'Microsoft.Web/sites/slots/config@2015-08-01' = if (length(slotConnectionStrings) > 0) {
  parent: deploymentSlot
  name: 'connectionstrings'
  location: location
  properties: reduce(slotConnectionStrings, {}, (cur, next) => union(cur, {
    '${next.name}': {
      value: next.value
      type: next.type
    }
  }))
}

output slotName string = deploymentSlot.name
output slotUrl string = 'https://${siteName}-${toLower(slotName)}.azurewebsites.net'
output slotPrincipalId string = deploymentSlot.identity.principalId
