// mySite.bicep - Application-specific infrastructure
// - Web Site
// - Storage Account 
// - Application Insights
// - Configuration (uses shared Key Vault)

param appName string
param envName string
param planId string
param logAnalyticsId string
param sharedKeyVaultName string
param sharedResourceGroupName string

var prefix = '${envName}-${appName}'
var siteName = '${prefix}-site'
var storageName = '${toLower(replace(prefix,'-',''))}lrs'
var appInsightsName = '${prefix}-insights'

resource site 'Microsoft.Web/sites@2020-12-01' = {
  name: siteName
  location: resourceGroup().location
  kind: 'app'
  tags: {
    displayName: siteName
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: planId
    siteConfig: {
      // For .NET 9 on Windows
      use32BitWorkerProcess: false
      appSettings: [
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
}

// Application Insights for monitoring
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: resourceGroup().location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsId
  }
}

// Grant web app access to shared Key Vault
module keyVaultAccess 'keyVaultAccessPolicy.bicep' = {
  name: 'keyVaultAccessDeployment'
  scope: resourceGroup(sharedResourceGroupName)
  params: {
    keyVaultName: sharedKeyVaultName
    principalId: site.identity.principalId
  }
}

resource siteName_appsettings 'Microsoft.Web/sites/config@2015-08-01' = {
  parent: site
  name: 'appsettings'
  location: resourceGroup().location
  tags: {
    displayName: 'config'
  }
  properties: {
    APPINSIGHTS_INSTRUMENTATIONKEY: appInsights.properties.InstrumentationKey
    APPLICATIONINSIGHTS_CONNECTION_STRING: appInsights.properties.ConnectionString
    ApplicationInsightsAgent_EXTENSION_VERSION: '~3'
    XDT_MicrosoftApplicationInsights_Mode: 'Recommended'
    XDT_MicrosoftApplicationInsights_PreemptSdk: 'Disabled'
    ANCM_ADDITIONAL_ERROR_PAGE_LINK: 'https://${siteName}.scm.azurewebsites.net/detectors'
  }
  dependsOn: [
    storageSecret
    //dbSecret
  ]
}

resource siteName_connectionstrings 'Microsoft.Web/sites/config@2015-08-01' = {
  parent: site
  name: 'connectionstrings'
  location: resourceGroup().location
  tags: {
    displayName: 'connectionstrings'
  }
  properties: {
    myStorage: {
      value: '@Microsoft.KeyVault(VaultName=${sharedKeyVaultName};SecretName=storage-connection-${envName}-${appName})'
      type: 'Custom'
    }
    // myDBMS: {
    //   value: '@Microsoft.KeyVault(VaultName=${sharedKeyVaultName};SecretName=db-connection-${envName}-${appName})'
    //   type: 'SQLAzure'
    // }
  }
  dependsOn: [
    storageSecret
    // dbSecret
  ]
}

resource storage 'Microsoft.Storage/storageAccounts@2021-04-01' = {
  name: storageName
  location: resourceGroup().location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: true
  }
}

// Deploy storage connection secret to shared Key Vault
module storageSecret 'keyVaultSecret.bicep' = {
  name: 'storageSecretDeployment'
  scope: resourceGroup(sharedResourceGroupName)
  params: {
    keyVaultName: sharedKeyVaultName
    secretName: 'storage-connection-${envName}-${appName}'
    secretValue: 'DefaultEndpointsProtocol=https;AccountName=${storageName};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
  }
  dependsOn: [
    keyVaultAccess
  ]
}

//// Deploy database connection secret to shared Key Vault (placeholder for future use)
// module dbSecret 'keyVaultSecret.bicep' = {
//   name: 'dbSecretDeployment'
//   scope: resourceGroup(sharedResourceGroupName)
//   params: {
//     keyVaultName: sharedKeyVaultName
//     secretName: 'db-connection-${envName}-${appName}'
//     secretValue: 'Server=placeholder;Database=placeholder;Trusted_Connection=True;'
//   }
//   dependsOn: [
//     keyVaultAccess
//   ]
// }

output siteName string = site.name
output storageName string = storage.name
output storageConnection string = storageSecret.outputs.secretUri
output siteUrl string = 'https://${site.name}.azurewebsites.net'
output appInsightsName string = appInsights.name
