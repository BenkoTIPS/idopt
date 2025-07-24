// App Service Plan module
// Provides compute resources for web applications

param planName string
param location string = resourceGroup().location
param skuName string = 'S1'
param skuCapacity int = 1

resource plan 'Microsoft.Web/serverfarms@2020-12-01' = {
  name: planName
  location: location
  kind: 'app'
  sku: {
    name: skuName
    capacity: skuCapacity
  }
  properties: {
    reserved: false
  }
}

// Output for use by other modules
output planId string = plan.id
output planName string = plan.name
