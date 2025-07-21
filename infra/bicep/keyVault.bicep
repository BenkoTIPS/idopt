// Key Vault module
// Provides secure storage for application secrets

param keyVaultName string
param location string = resourceGroup().location
param principalId string = ''
param tenantId string = subscription().tenantId

resource keyVault 'Microsoft.KeyVault/vaults@2021-04-01-preview' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: principalId != '' ? [
      {
        tenantId: tenantId
        objectId: principalId
        permissions: {
          secrets: [
            'get'
          ]
        }
      }
    ] : []
  }
}

// Output for use by other modules
output keyVaultId string = keyVault.id
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
