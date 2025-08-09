az login
az account set --subscription "5ac04c8d-3ed9-4a27-b757-b16b5e9073d7"

cd "C:\JTThomson\AiAudioAzFunc\AiAudioAzureFunc"

dotnet publish -c Release 
Compress-Archive -Path .\bin\Release\net8.0\publish\* -DestinationPath function.zip

az functionapp deployment source config-zip --resource-group AudioAI_RG --name AuidioAI-FA --src function.zip 

az logout
az login --scope https://management.core.windows.net//.default

az account tenant list --output table

az login --tenant 4a23cf59-227e-41a8-8bed-ef3e2b606679