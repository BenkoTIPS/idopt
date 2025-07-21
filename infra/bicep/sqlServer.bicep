
param envName string
param location string
param sqlAdminPassword string

resource sql 'Microsoft.Sql/servers@2021-02-01-preview' = {
  name: '${envName}-shared-sql'
  location: location
  properties: {
    administratorLogin: 'sqladmin'
    administratorLoginPassword: sqlAdminPassword
  }
}
