// Key Vault Access Policy module
// Adds access policy to an existing Key Vault

param keyVaultName string
param tenantId string
param objectId string
param permissions object = {
  keys: []
  secrets: ['get', 'list']
  certificates: []
}

resource keyVault 'Microsoft.KeyVault/vaults@2021-04-01-preview' existing = {
  name: keyVaultName
}

resource accessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2021-04-01-preview' = {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: tenantId
        objectId: objectId
        permissions: permissions
      }
    ]
  }
}

// Outputs for use by other modules
output keyVaultName string = keyVault.name
