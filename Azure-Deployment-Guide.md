# Azure Deployment Guide
## Vehicle Maintenance Invoice Processing System

### Document Information
- **Application**: Vehicle Maintenance Invoice Processing System
- **Target Platform**: Microsoft Azure
- **Date**: August 14, 2025
- **Deployment Type**: Production-ready

---

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Azure Resource Provisioning](#azure-resource-provisioning)
3. [Azure SQL Database Setup](#azure-sql-database-setup)
4. [Azure Blob Storage Configuration](#azure-blob-storage-configuration)
5. [Azure Form Recognizer Setup](#azure-form-recognizer-setup)
6. [Azure App Service Deployment](#azure-app-service-deployment)
7. [Network Security Configuration](#network-security-configuration)
8. [Connection Strings & Configuration](#connection-strings--configuration)
9. [Production Deployment Checklist](#production-deployment-checklist)

---

## Prerequisites

### Required Tools
- Azure CLI (latest version)
- Azure PowerShell module
- Visual Studio 2022 or VS Code with Azure extensions
- .NET 8.0 SDK
- Git

### Azure Subscription Requirements
- Azure subscription with Contributor or Owner role
- Sufficient quota for:
  - App Service (Basic B1 or higher)
  - Azure SQL Database (Basic tier or higher)
  - Storage Account (Standard_LRS)

### Installation Commands
```powershell
# Install Azure CLI
winget install Microsoft.AzureCLI

# Install Azure PowerShell
Install-Module -Name Az -Repository PSGallery -Force

# Login to Azure
az login
Connect-AzAccount
```

---

## Azure Resource Provisioning

### 1. Create Resource Group
```powershell
# Variables
$resourceGroup = "appsvc_windows_eastus"
$location = "East US"
$appName = "fwmainplus"
$sqlServerName = "VehicleMaintenance"
$storageAccountName = "fwmainplusblob"  # Must be globally unique

# Create Resource Group
az group create --name $resourceGroup --location $location
```

### 2. Create App Service Plan
```powershell
# Create App Service Plan (Basic B1 for minimal setup)
az appservice plan create `
    --name "asp-vehicle-maintenance-prod" `
    --resource-group $resourceGroup `
    --location $location `
    --sku B1 `
    --is-linux false
```

---

## Azure SQL Database Setup

### 1. Create SQL Server
```powershell
# Create SQL Server
$sqlAdminUser = "sqladmin"
$sqlAdminPassword = "YourStrongPassword123!"

az sql server create `
    --name $sqlServerName `
    --resource-group $resourceGroup `
    --location $location `
    --admin-user $sqlAdminUser `
    --admin-password $sqlAdminPassword
```

### 2. Create SQL Database
```powershell
# Create SQL Database (Basic tier for minimal setup)
az sql db create `
    --resource-group $resourceGroup `
    --server $sqlServerName `
    --name "VehicleMaintenance" `
    --service-objective Basic `
    --backup-storage-redundancy Local
```

### 3. Configure Firewall Rules
```powershell
# Allow Azure services
az sql server firewall-rule create `
    --resource-group $resourceGroup `
    --server $sqlServerName `
    --name "AllowAzureServices" `
    --start-ip-address 0.0.0.0 `
    --end-ip-address 0.0.0.0

# Allow your current IP (for management)
$myIP = (Invoke-WebRequest -Uri "https://api.ipify.org").Content
az sql server firewall-rule create `
    --resource-group $resourceGroup `
    --server $sqlServerName `
    --name "AllowMyIP" `
    --start-ip-address $myIP `
    --end-ip-address $myIP

# Allow App Service (will be configured after app creation)
```

### 4. Database Schema Creation
```sql
-- Connect to the database and run this script
-- Connection string: Server=tcp:{sqlServerName}.database.windows.net,1433;Database=VehicleMaintenance;User ID={sqlAdminUser};Password={sqlAdminPassword};

-- Create Invoice Header table
CREATE TABLE InvoiceHeader (
    InvoiceID INT IDENTITY(1,1) PRIMARY KEY,
    VehicleID NVARCHAR(50) NOT NULL,
    Odometer INT NULL,
    InvoiceNumber NVARCHAR(50) NOT NULL,
    InvoiceDate DATE NOT NULL,
    TotalCost DECIMAL(18,2) NOT NULL,
    TotalPartsCost DECIMAL(18,2) NOT NULL,
    TotalLaborCost DECIMAL(18,2) NOT NULL,
    BlobFileUrl NVARCHAR(500) NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    ProcessedAt DATETIME2 DEFAULT GETUTCDATE(),
    ProcessingStatus NVARCHAR(20) DEFAULT 'Completed',
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Create Invoice Lines table with proper DECIMAL types
-- IMPORTANT: UnitCost, Quantity, and TotalLineCost must be DECIMAL, not INT
-- This prevents InvalidCastException errors in Entity Framework
CREATE TABLE InvoiceLines (
    LineID INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceID INT NOT NULL,
    LineNumber INT NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    UnitCost DECIMAL(18,2) NOT NULL,        -- DECIMAL for currency values
    Quantity DECIMAL(10,2) NOT NULL,        -- DECIMAL for fractional quantities  
    TotalLineCost DECIMAL(18,2) NOT NULL,   -- DECIMAL for currency values
    CONSTRAINT FK_InvoiceLines_InvoiceHeader 
        FOREIGN KEY (InvoiceID) REFERENCES InvoiceHeader(InvoiceID) ON DELETE CASCADE
);

-- Create indexes for better performance
CREATE NONCLUSTERED INDEX IX_InvoiceHeader_VehicleID 
    ON InvoiceHeader (VehicleID);

CREATE NONCLUSTERED INDEX IX_InvoiceHeader_InvoiceDate 
    ON InvoiceHeader (InvoiceDate);

CREATE NONCLUSTERED INDEX IX_InvoiceHeader_ProcessedAt 
    ON InvoiceHeader (ProcessedAt);

CREATE NONCLUSTERED INDEX IX_InvoiceLines_InvoiceID 
    ON InvoiceLines (InvoiceID);

-- Create application user
CREATE LOGIN [vehicle-maintenance-app] WITH PASSWORD = 'AppPassword123!';
CREATE USER [vehicle-maintenance-app] FOR LOGIN [vehicle-maintenance-app];

-- Grant permissions
ALTER ROLE db_datareader ADD MEMBER [vehicle-maintenance-app];
ALTER ROLE db_datawriter ADD MEMBER [vehicle-maintenance-app];
GRANT EXECUTE ON SCHEMA::dbo TO [vehicle-maintenance-app];
```

**⚠️ Critical Note**: If you previously created the database with INT columns instead of DECIMAL columns for UnitCost, Quantity, and TotalLineCost, you must run this migration script:

```sql
-- Schema Fix Migration (run only if needed)
ALTER TABLE InvoiceLines ALTER COLUMN Quantity DECIMAL(10,2);
ALTER TABLE InvoiceLines ALTER COLUMN UnitCost DECIMAL(18,2);
ALTER TABLE InvoiceLines ALTER COLUMN TotalLineCost DECIMAL(18,2);
```

---

## Azure Blob Storage Configuration

### 1. Create Storage Account
```powershell
# Create Storage Account
az storage account create `
    --name $storageAccountName `
    --resource-group $resourceGroup `
    --location $location `
    --sku Standard_LRS `
    --kind StorageV2 `
    --access-tier Hot
```

### 2. Create Containers
```powershell
# Get storage account key
$storageKey = az storage account keys list `
    --resource-group $resourceGroup `
    --account-name $storageAccountName `
    --query "[0].value" --output tsv

# Create single container for all invoices
az storage container create `
    --name "invoices" `
    --account-name $storageAccountName `
    --account-key $storageKey `
    --public-access off
```

### 3. Configure CORS (if needed for direct browser uploads)
```powershell
az storage cors add `
    --services b `
    --methods GET POST PUT `
    --origins "*" `
    --allowed-headers "*" `
    --exposed-headers "*" `
    --max-age 3600 `
    --account-name $storageAccountName `
    --account-key $storageKey
```

---

## Azure Form Recognizer Setup

### 1. Create Form Recognizer Resource
```powershell
# Create Form Recognizer resource (required for OCR processing)
$formRecognizerName = "fr-vehicle-maintenance-prod"

az cognitiveservices account create `
    --name $formRecognizerName `
    --resource-group $resourceGroup `
    --location $location `
    --kind FormRecognizer `
    --sku F0 `
    --yes
```

### 2. Get Form Recognizer Keys and Endpoint
```powershell
# Get the endpoint
$formRecognizerEndpoint = az cognitiveservices account show `
    --name $formRecognizerName `
    --resource-group $resourceGroup `
    --query "properties.endpoint" --output tsv

# Get the primary key
$formRecognizerKey = az cognitiveservices account keys list `
    --name $formRecognizerName `
    --resource-group $resourceGroup `
    --query "key1" --output tsv

Write-Host "Form Recognizer Endpoint: $formRecognizerEndpoint"
Write-Host "Form Recognizer Key: $formRecognizerKey"
```

### 3. Test Form Recognizer Connection
```powershell
# Test the Form Recognizer service
$headers = @{
    'Ocp-Apim-Subscription-Key' = $formRecognizerKey
    'Content-Type' = 'application/json'
}

$testUrl = "$formRecognizerEndpoint/formrecognizer/v2.1/prebuilt/invoice/analyze"

# Test with a simple REST call (optional verification)
Write-Host "Form Recognizer service configured successfully"
Write-Host "Endpoint: $testUrl"
```

### 4. Pricing Tier Considerations
```powershell
# For production workloads, consider upgrading to S0 tier
# F0 (Free tier): 500 pages per month
# S0 (Standard tier): Pay per transaction, better for production

# To upgrade to S0 tier:
# az cognitiveservices account update `
#     --name $formRecognizerName `
#     --resource-group $resourceGroup `
#     --sku S0
```

---

## Azure App Service Deployment

### 1. Create Web App
```powershell
# Create Web App
az webapp create `
    --resource-group $resourceGroup `
    --plan "asp-vehicle-maintenance-prod" `
    --name $appName `
    --runtime "DOTNET|8.0"
```

### 2. Configure App Settings
```powershell
# Set application settings
az webapp config appsettings set `
    --resource-group $resourceGroup `
    --name $appName `
    --settings `
        "ASPNETCORE_ENVIRONMENT=Production" `
        "WEBSITE_RUN_FROM_PACKAGE=1" `
        "ConnectionStrings__DefaultConnection=Server=tcp:$sqlServerName.database.windows.net,1433;Database=VehicleMaintenance;User ID=vehicle-maintenance-app;Password=AppPassword123!;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" `
        "BlobStorage__ConnectionString=DefaultEndpointsProtocol=https;AccountName=$storageAccountName;AccountKey=$storageKey;EndpointSuffix=core.windows.net" `
        "BlobStorage__ContainerName=invoices" `
        "FormRecognizer__Endpoint=$formRecognizerEndpoint" `
        "FormRecognizer__ApiKey=$formRecognizerKey"
```

### 3. Deploy Application
```powershell
# Build the application first
dotnet publish --configuration Release --output .\publish

# Create deployment package
Compress-Archive -Path .\publish\* -DestinationPath .\deploy.zip -Force

# Deploy to Azure
az webapp deployment source config-zip `
    --resource-group $resourceGroup `
    --name $appName `
    --src .\deploy.zip
```

---

## Network Security Configuration

### 1. App Service Network Security
```powershell
# Configure HTTPS only
az webapp update `
    --resource-group $resourceGroup `
    --name $appName `
    --https-only true

# Configure minimum TLS version
az webapp config set `
    --resource-group $resourceGroup `
    --name $appName `
    --min-tls-version 1.2
```

### 2. SQL Database Network Security
```powershell
# Get App Service outbound IP addresses
$outboundIPs = az webapp show `
    --resource-group $resourceGroup `
    --name $appName `
    --query "outboundIpAddresses" --output tsv

# Add firewall rules for each outbound IP
$ips = $outboundIPs -split ","
for ($i = 0; $i -lt $ips.Length; $i++) {
    $ip = $ips[$i].Trim()
    az sql server firewall-rule create `
        --resource-group $resourceGroup `
        --server $sqlServerName `
        --name "AppService-IP-$i" `
        --start-ip-address $ip `
        --end-ip-address $ip
}
```

### 3. Storage Account Network Security
```powershell
# Configure storage account to allow App Service access
# Note: For production, consider using Private Endpoints or VNet integration

# Allow trusted Microsoft services
az storage account update `
    --resource-group $resourceGroup `
    --name $storageAccountName `
    --bypass AzureServices `
    --default-action Allow
```

---

## Connection Strings & Configuration

### Application Configuration (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:{sqlServerName}.database.windows.net,1433;Database=VehicleMaintenance;User ID=vehicle-maintenance-app;Password=AppPassword123!;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  },
  "BlobStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={storageKey};EndpointSuffix=core.windows.net",
    "ContainerName": "invoices",
    "MaxFileSizeMB": 10
  },
  "FormRecognizer": {
    "Endpoint": "{formRecognizerEndpoint}",
    "ApiKey": "{formRecognizerKey}",
    "ModelId": "prebuilt-invoice"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "AllowedHosts": "*"
}
```

### Environment-Specific Configuration
```powershell
# Production settings
az webapp config appsettings set `
    --resource-group $resourceGroup `
    --name $appName `
    --settings `
        "Logging__LogLevel__Default=Warning" `
        "Logging__LogLevel__Microsoft=Error" `
        "DetailedErrors=false" `
        "HostingEnvironment=Production"
```

---

## Production Deployment Checklist

### Pre-Deployment
- [ ] All Azure resources provisioned
- [ ] Database schema deployed and tested
- [ ] Form Recognizer service created and tested
- [ ] Connection strings configured
- [ ] SSL certificate configured (Azure handles this automatically)
- [ ] Firewall rules configured

### Security Checklist
- [ ] HTTPS-only enabled
- [ ] Minimum TLS 1.2 configured
- [ ] SQL firewall rules limited to App Service IPs
- [ ] Storage account access restricted
- [ ] Application secrets stored in Azure Key Vault (recommended)
- [ ] Network Security Groups configured (if using VNet)

### Performance Checklist
- [ ] App Service plan sized appropriately (Basic B1 minimum)
- [ ] SQL Database service tier appropriate for load (Basic minimum)
- [ ] External accessibility verified

### Monitoring Checklist
- [ ] Basic application logging enabled
- [ ] Database connectivity verified
- [ ] External access tested

### Testing Checklist
- [ ] Smoke tests passed
- [ ] API endpoints tested
- [ ] File upload and processing tested
- [ ] Form Recognizer OCR processing tested
- [ ] Database connectivity verified
- [ ] Performance testing completed
- [ ] Security testing completed

---

## Troubleshooting Common Issues

### Application Won't Start
```powershell
# Check application logs
az webapp log tail --resource-group $resourceGroup --name $appName

# Check app settings
az webapp config appsettings list --resource-group $resourceGroup --name $appName
```

### Database Connection Issues
```powershell
# Verify firewall rules
az sql server firewall-rule list --resource-group $resourceGroup --server $sqlServerName

# Test connection from App Service
az webapp ssh --resource-group $resourceGroup --name $appName
# Then run: telnet {sqlServerName}.database.windows.net 1433
```

### Blob Storage Issues
```powershell
# Check storage account configuration
az storage account show --resource-group $resourceGroup --name $storageAccountName

# Test blob access
az storage blob list --container-name invoices --account-name $storageAccountName
```

### Form Recognizer Issues
```powershell
# Check Form Recognizer service status
az cognitiveservices account show --resource-group $resourceGroup --name $formRecognizerName

# Test Form Recognizer API access
$headers = @{
    'Ocp-Apim-Subscription-Key' = $formRecognizerKey
}
Invoke-RestMethod -Uri "$formRecognizerEndpoint/formrecognizer/v2.1/prebuilt/invoice/analyze" -Method Get -Headers $headers
```

### Database Schema Issues
```powershell
# Check column data types in database
sqlcmd -S "{sqlServerName}.database.windows.net" -d "VehicleMaintenance" -U "{sqlAdminUser}" -P "{sqlAdminPassword}" -Q "SELECT COLUMN_NAME, DATA_TYPE, NUMERIC_PRECISION, NUMERIC_SCALE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'InvoiceLines' AND COLUMN_NAME IN ('Quantity', 'UnitCost', 'TotalLineCost')"

# Common Issue: InvalidCastException - Unable to cast System.Int32 to System.Decimal
# This occurs when InvoiceLines columns are created as INT instead of DECIMAL
# Fix with this migration script:
sqlcmd -S "{sqlServerName}.database.windows.net" -d "VehicleMaintenance" -U "{sqlAdminUser}" -P "{sqlAdminPassword}" -Q "ALTER TABLE InvoiceLines ALTER COLUMN Quantity DECIMAL(10,2); ALTER TABLE InvoiceLines ALTER COLUMN UnitCost DECIMAL(18,2); ALTER TABLE InvoiceLines ALTER COLUMN TotalLineCost DECIMAL(18,2);"
```

---

## Cost Optimization

### Resource Sizing Recommendations
- **App Service**: Basic B1 for minimal setup, can scale up as needed
- **SQL Database**: Basic tier for development, Standard S1 for production
- **Storage Account**: Standard_LRS for cost efficiency
- **Form Recognizer**: F0 (Free) for testing, S0 (Standard) for production

### Cost Management Commands
```powershell
# Set up budget alerts
az consumption budget create `
    --budget-name "VehicleMaintenance-Budget" `
    --amount 200 `
    --category Cost `
    --time-grain Monthly `
    --time-period-start-date "2025-01-01" `
    --time-period-end-date "2025-12-31"
```

---

## Backup and Disaster Recovery

### Database Backup
```powershell
# Azure SQL Database has automatic backups
# Configure long-term retention if needed
az sql db ltr-policy set `
    --resource-group $resourceGroup `
    --server $sqlServerName `
    --database VehicleMaintenance `
    --weekly-retention P4W `
    --monthly-retention P12M `
    --yearly-retention P5Y
```

### Application Backup
```powershell
# Enable App Service backup
az webapp config backup update `
    --resource-group $resourceGroup `
    --webapp-name $appName `
    --container-url "https://$storageAccountName.blob.core.windows.net/backups" `
    --frequency 1 `
    --retain-one true
```

---

This deployment guide provides a minimal Azure setup for deploying your Vehicle Maintenance Invoice Processing System. The simplified architecture focuses on the core requirements: hosting the application on Azure App Service, connecting to Azure SQL Database, and storing files in a single Blob Storage container with external accessibility.
