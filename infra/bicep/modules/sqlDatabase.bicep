// SQL Database module
// Creates an Azure SQL Database on an existing SQL Server

param databaseName string
param sqlServerName string
param location string = resourceGroup().location
param skuName string = 'S0'
param skuTier string = 'Standard'

resource sqlServer 'Microsoft.Sql/servers@2021-02-01-preview' existing = {
  name: sqlServerName
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-02-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

output databaseId string = sqlDatabase.id
output databaseName string = sqlDatabase.name
