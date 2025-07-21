// Key Vault Secrets module
// Manages secrets stored in Key Vault

targetScope = 'resourceGroup'

param keyVaultName string
param secretName string
@secure()
param secretValue string

// Reference the existing Key Vault in the current resource group
resource existingKeyVault 'Microsoft.KeyVault/vaults@2021-04-01-preview' existing = {
  name: keyVaultName
}

resource keyVaultSecret 'Microsoft.KeyVault/vaults/secrets@2021-04-01-preview' = {
  parent: existingKeyVault
  name: secretName
  properties: {
    value: secretValue
  }
}

output secretUri string = keyVaultSecret.properties.secretUri
