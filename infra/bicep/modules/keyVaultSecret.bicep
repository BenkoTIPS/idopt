// Key Vault Secret module
// Creates a secret in an existing Key Vault

param keyVaultName string
param secretName string
@secure()
param secretValue string

resource keyVault 'Microsoft.KeyVault/vaults@2021-04-01-preview' existing = {
  name: keyVaultName
}

resource secret 'Microsoft.KeyVault/vaults/secrets@2021-04-01-preview' = {
  parent: keyVault
  name: secretName
  properties: {
    value: secretValue
  }
}

// Outputs for use by other modules
output secretUri string = secret.properties.secretUri
output secretName string = secret.name
