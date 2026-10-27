<#
.SYNOPSIS
Deploy Checkout API demo app to Microsoft Azure using Azure Container Apps.

.DESCRIPTION
This script provisions:
1. Azure Resource Group: rg-checkout-demo
2. Azure Container Registry (ACR) or builds directly with az containerapp
3. Azure Container App: app-checkout-api
4. Azure Database for PostgreSQL Flexible Server: psql-checkout-db (Optional)

Prerequisites:
- Azure CLI installed (az)
- Active Azure Subscription with credits
#>

param(
    [string]$ResourceGroup = "rg-checkout-demo",
    [string]$Location = "eastus",
    [string]$AppName = "app-checkout-api"
)

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Deploying Demo App to Microsoft Azure   " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. Login check
$account = az account show --output json 2>$null
if (-not $account) {
    Write-Host "Please login to Azure:" -ForegroundColor Yellow
    az login
}

# 2. Create Resource Group
Write-Host "`n1. Creating Resource Group: $ResourceGroup ($Location)..." -ForegroundColor Green
az group create --name $ResourceGroup --location $Location --output table

# 3. Create Container App Environment
$envName = "env-checkout-demo"
Write-Host "`n2. Creating Container App Environment: $envName..." -ForegroundColor Green
az containerapp env create --name $envName --resource-group $ResourceGroup --location $Location --output table

# 4. Build and deploy container directly from source
Write-Host "`n3. Building and deploying $AppName from local source..." -ForegroundColor Green
az containerapp up `
    --name $AppName `
    --resource-group $ResourceGroup `
    --environment $envName `
    --source . `
    --ingress external `
    --target-port 8080 `
    --env-vars "Payment__TimeoutSeconds=3"

# 5. Display output URL
$fqdn = az containerapp show --name $AppName --resource-group $ResourceGroup --query "properties.configuration.ingress.fqdn" -o tsv
Write-Host "`n==========================================" -ForegroundColor Green
Write-Host " Deployment Complete!" -ForegroundColor Green
Write-Host " App URL: https://$fqdn" -ForegroundColor Cyan
Write-Host " Swagger: https://$fqdn/swagger" -ForegroundColor Cyan
Write-Host " Metrics: https://$fqdn/metrics" -ForegroundColor Cyan
Write-Host " Health:  https://$fqdn/health" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Green
