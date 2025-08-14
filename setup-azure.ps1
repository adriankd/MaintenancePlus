# Azure Services Configuration Script
# This script helps configure Azure services for the Vehicle Maintenance Invoice System

param(
    [Parameter(Mandatory=$true)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$Location = "East US",
    
    [Parameter(Mandatory=$false)]
    [string]$AppName = "vehicle-maintenance-app",
    
    [Parameter(Mandatory=$false)]
    [switch]$ConfigureOnly
)

Write-Host "=== Azure Services Configuration ===" -ForegroundColor Green
Write-Host "Subscription: $SubscriptionId" -ForegroundColor Yellow
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host "Location: $Location" -ForegroundColor Yellow
Write-Host "App Name: $AppName" -ForegroundColor Yellow
Write-Host ""

# Check if Azure CLI is installed
try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-Host "Azure CLI Version: $($azVersion.'azure-cli')" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Azure CLI is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install Azure CLI from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli" -ForegroundColor Yellow
    exit 1
}

# Login check
Write-Host "Checking Azure login status..." -ForegroundColor Blue
try {
    $account = az account show --output json | ConvertFrom-Json
    Write-Host "Logged in as: $($account.user.name)" -ForegroundColor Green
}
catch {
    Write-Host "Please login to Azure first: az login" -ForegroundColor Red
    exit 1
}

# Set subscription
Write-Host "Setting subscription..." -ForegroundColor Blue
az account set --subscription $SubscriptionId

if (-not $ConfigureOnly) {
    # Create resource group
    Write-Host "Creating resource group: $ResourceGroupName..." -ForegroundColor Blue
    az group create --name $ResourceGroupName --location $Location
    
    # Create storage account
    $storageAccountName = $AppName.Replace("-", "") + "storage"
    Write-Host "Creating storage account: $storageAccountName..." -ForegroundColor Blue
    az storage account create `
        --name $storageAccountName `
        --resource-group $ResourceGroupName `
        --location $Location `
        --sku Standard_LRS `
        --kind StorageV2
    
    # Create blob container
    Write-Host "Creating blob container..." -ForegroundColor Blue
    az storage container create `
        --name invoices `
        --account-name $storageAccountName `
        --auth-mode login
    
    # Create SQL Server and Database
    $sqlServerName = "$AppName-sql-server"
    $sqlDatabaseName = "VehicleMaintenance"
    $adminUsername = "sqladmin"
    
    Write-Host "Enter SQL Server admin password (minimum 8 characters, must contain uppercase, lowercase, numbers):" -ForegroundColor Yellow
    $adminPassword = Read-Host -AsSecureString
    $adminPasswordText = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($adminPassword))
    
    Write-Host "Creating SQL Server: $sqlServerName..." -ForegroundColor Blue
    az sql server create `
        --name $sqlServerName `
        --resource-group $ResourceGroupName `
        --location $Location `
        --admin-user $adminUsername `
        --admin-password $adminPasswordText
    
    Write-Host "Creating SQL Database: $sqlDatabaseName..." -ForegroundColor Blue
    az sql db create `
        --resource-group $ResourceGroupName `
        --server $sqlServerName `
        --name $sqlDatabaseName `
        --service-objective Basic
    
    # Create firewall rule for Azure services
    Write-Host "Creating firewall rule for Azure services..." -ForegroundColor Blue
    az sql server firewall-rule create `
        --resource-group $ResourceGroupName `
        --server $sqlServerName `
        --name AllowAzureServices `
        --start-ip-address 0.0.0.0 `
        --end-ip-address 0.0.0.0
    
    # Create Form Recognizer
    $formRecognizerName = "$AppName-form-recognizer"
    Write-Host "Creating Form Recognizer service: $formRecognizerName..." -ForegroundColor Blue
    az cognitiveservices account create `
        --name $formRecognizerName `
        --resource-group $ResourceGroupName `
        --kind FormRecognizer `
        --sku F0 `
        --location $Location `
        --yes
    
    # Create App Service Plan
    $appServicePlanName = "$AppName-plan"
    Write-Host "Creating App Service Plan: $appServicePlanName..." -ForegroundColor Blue
    az appservice plan create `
        --name $appServicePlanName `
        --resource-group $ResourceGroupName `
        --location $Location `
        --sku B1 `
        --is-linux
    
    # Create Web App
    Write-Host "Creating Web App: $AppName..." -ForegroundColor Blue
    az webapp create `
        --resource-group $ResourceGroupName `
        --plan $appServicePlanName `
        --name $AppName `
        --runtime "DOTNET|8.0"
}

# Get connection strings and keys
Write-Host "Retrieving connection strings and keys..." -ForegroundColor Blue

$storageAccountName = $AppName.Replace("-", "") + "storage"
$sqlServerName = "$AppName-sql-server"
$formRecognizerName = "$AppName-form-recognizer"

# Storage connection string
$storageConnectionString = az storage account show-connection-string `
    --name $storageAccountName `
    --resource-group $ResourceGroupName `
    --output tsv

# SQL connection string
$sqlConnectionString = "Server=tcp:$sqlServerName.database.windows.net,1433;Initial Catalog=VehicleMaintenance;Persist Security Info=False;User ID=sqladmin;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# Form Recognizer key and endpoint
$formRecognizerKey = az cognitiveservices account keys list `
    --name $formRecognizerName `
    --resource-group $ResourceGroupName `
    --query key1 `
    --output tsv

$formRecognizerEndpoint = az cognitiveservices account show `
    --name $formRecognizerName `
    --resource-group $ResourceGroupName `
    --query properties.endpoint `
    --output tsv

# Generate appsettings.Production.json
$appSettings = @{
    "ConnectionStrings" = @{
        "DefaultConnection" = $sqlConnectionString
    }
    "BlobStorage" = @{
        "ConnectionString" = $storageConnectionString
        "ContainerName" = "invoices"
    }
    "FormRecognizer" = @{
        "Endpoint" = $formRecognizerEndpoint
        "ApiKey" = $formRecognizerKey
    }
    "Logging" = @{
        "LogLevel" = @{
            "Default" = "Information"
            "Microsoft.AspNetCore" = "Warning"
        }
    }
    "AllowedHosts" = "*"
}

$appSettingsJson = $appSettings | ConvertTo-Json -Depth 4
$appSettingsPath = "src/appsettings.Production.json"

Write-Host "Creating production configuration file..." -ForegroundColor Blue
$appSettingsJson | Out-File -FilePath $appSettingsPath -Encoding UTF8

Write-Host ""
Write-Host "=== Configuration Complete ===" -ForegroundColor Green
Write-Host "✓ Storage Account: $storageAccountName" -ForegroundColor Green
Write-Host "✓ SQL Server: $sqlServerName.database.windows.net" -ForegroundColor Green
Write-Host "✓ Form Recognizer: $formRecognizerName" -ForegroundColor Green
Write-Host "✓ Web App: $AppName" -ForegroundColor Green
Write-Host ""
Write-Host "Configuration file created: $appSettingsPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Update the SQL password in appsettings.Production.json" -ForegroundColor White
Write-Host "2. Deploy the application: dotnet publish -c Release" -ForegroundColor White
Write-Host "3. Deploy to Azure: az webapp deployment source config-zip" -ForegroundColor White
Write-Host ""
Write-Host "Important: Add your local IP to SQL Server firewall rules for development" -ForegroundColor Red
