// Key Vault Access Policy module
// Adds access policy to an existing Key Vault for a managed identity
// This module is scoped to target a specific resource group

targetScope = 'resourceGroup'

param keyVaultName string
param principalId string
param tenantId string = subscription().tenantId
param permissions object = {
  secrets: [
    'get'
  ]
}

// Reference the existing Key Vault in the current resource group
resource existingKeyVault 'Microsoft.KeyVault/vaults@2021-04-01-preview' existing = {
  name: keyVaultName
}

resource keyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2021-04-01-preview' = {
  parent: existingKeyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: tenantId
        objectId: principalId
        permissions: permissions
      }
    ]
  }
}

output policyName string = keyVaultAccessPolicy.name
