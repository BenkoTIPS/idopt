git init
git add .
git commit -m "Initial commit"
git branch -M main
git remote add origin https://github.com/BenkoTIPS/idopt.git
git push -u origin main

dotnet new webapp -o src/myVideos
dotnet new sln
dotnet sln add src/myVideos

dotnet add src/myVideos package azure.storage.blobs

devenv .\bnk-streamit.sln
dotnet new page -n Videos -o src/myVideos/Pages
dotnet new page -n Upload -o src/myVideos/Pages
dotnet new page -n myLogin -o src/myVideos/Pages

start powerpnt.exe docs/bnk24-streamit.pptx
start powerpnt.exe docs/bnk25-identity.pptx

## Deploy to Azure - Use az webapp up to deploy to Azure
az login --use-device-code

# deploy infrastructure - ARM template
$deploy = az deployment sub create --location centralUs --template-file infra/arm/main.bicep

# Extract outputs from deployment
$values = $deploy | ConvertFrom-Json
$siteName = $values.properties.outputs.siteName.value
$planName = $values.properties.outputs.planName.value
$rgName = $values.properties.outputs.rgName.value
$slotNames = @("sql", "Easy", "B2C", "Keycloak", "Ids")

# Change to the project directory for deployment
cd src/myVideos
az webapp up -b --name $siteName --resource-group $rgName --plan $planName
cd ../..
