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

$appName = "idopt"
$envName = "cnf"
$rgName = "$envName-$appName-rg"
$siteName = "$envName-$appName-site"
$planName = "$envName-shared-plan"

# deploy infrastructure - Bicep templates
$envName = "bnk"
$sharedDeploy = az deployment sub create --location centralus --template-file infra/bicep/shared.bicep --parameters envName=$envName

$mainDeploy = az deployment sub create --location centralUs --template-file infra/bicep/idopt.bicep --parameters envName=$envName 


# Extract outputs from deployment
$values = $mainDeploy | ConvertFrom-Json
$siteName = $values.properties.outputs.siteName.value
$planName = $values.properties.outputs.planName.value
$rgName = $values.properties.outputs.rgName.value

## build the app
dotnet publish src/myVideos -c Release -o src/myVideos/bin/publish
mkdir src/myVideos/bin/deploy -Force
Compress-Archive -Path src/myVideos/bin/publish\* -DestinationPath src/myVideos/bin/deploy/MyVideos.zip -Force
## Deploy the application
az webapp deploy --resource-group $rgName --name $siteName --src-path src/myVideos/bin/deploy/MyVideos.zip --type zip

# 

