# Vehicle Maintenance Invoice Processing System

A comprehensive ASP.NET Core 8.0 application that processes vehicle maintenance invoices using Azure AI services for OCR and data extraction.

## 🚗 Overview

This system automates the processing of vehicle maintenance invoices by:
- Uploading PDF invoices to Azure Blob Storage
- Using Azure Form Recognizer for OCR and data extraction
- Storing structured data in Azure SQL Database
- Providing a web interface for viewing and managing invoices

## 🏗️ Architecture

- **Frontend**: ASP.NET Core MVC with Bootstrap
- **Backend**: ASP.NET Core 8.0 Web API
- **Database**: Azure SQL Database
- **Storage**: Azure Blob Storage
- **AI/ML**: Azure Form Recognizer (Cognitive Services)
- **Hosting**: Azure App Service

## 📋 Features

- ✅ **PDF Upload & Processing**: Drag-and-drop file upload with processing status
- ✅ **OCR Data Extraction**: Automatic invoice data extraction using Azure Form Recognizer
- ✅ **Invoice Management**: View, search, and manage processed invoices
- ✅ **Data Validation**: Confidence scoring and error handling
- ✅ **REST API**: Complete API for programmatic access
- ✅ **Responsive Design**: Mobile-friendly interface
- ✅ **Swagger Documentation**: Interactive API documentation

## 🛠️ Technology Stack

### Backend
- ASP.NET Core 8.0
- Entity Framework Core
- Azure SDK for .NET
- Swagger/OpenAPI

### Frontend
- HTML5/CSS3
- Bootstrap 5
- JavaScript (ES6+)
- Responsive design

### Azure Services
- Azure App Service
- Azure SQL Database
- Azure Blob Storage
- Azure Form Recognizer
- Azure Application Insights (optional)

## 📦 Prerequisites

- .NET 8.0 SDK
- Azure subscription
- Visual Studio 2022 or VS Code
- Azure CLI (for deployment)

## 🚀 Quick Start

### 1. Clone the Repository
```bash
git clone https://github.com/adriankd/MaintenancePlus.git
cd MaintenancePlus
```

### 2. Configure Azure Resources
Follow the [Azure Deployment Guide](Azure-Deployment-Guide.md) to set up:
- Azure SQL Database
- Azure Blob Storage
- Azure Form Recognizer

### 3. Configure Application Settings
```bash
# Copy the example configuration
cp src/appsettings.Example.json src/appsettings.json

# Update the configuration with your Azure resource details
# Edit src/appsettings.json with your connection strings and API keys
```

### 4. Set Up Database
```bash
# Run the database schema setup
sqlcmd -S "your-server.database.windows.net" -d "VehicleMaintenance" -U "your-user" -P "your-password" -i SQL-Database-Schema.sql
```

### 5. Run the Application
```bash
# Navigate to source directory
cd src

# Restore packages
dotnet restore

# Run the application
dotnet run
```

The application will be available at `http://localhost:5000`

## 📖 Documentation

- [Azure Deployment Guide](Azure-Deployment-Guide.md) - Complete Azure setup instructions
- [Application Usage Guide](APPLICATION_USAGE_GUIDE.md) - How to use the application
- [User Stories](UserStories-VehicleMaintenanceInvoiceSystem.md) - Feature requirements
- [Database Schema](SQL-Database-Schema.sql) - Complete database setup

## 🏗️ Project Structure

```
MaintenancePlus/
├── src/                                # Main application source
│   ├── Controllers/                    # MVC Controllers and API endpoints
│   ├── Models/                        # Data models and DTOs
│   ├── Services/                      # Business logic services
│   ├── Data/                          # Entity Framework context
│   ├── Views/                         # MVC Views (Razor pages)
│   ├── wwwroot/                       # Static web assets
│   └── Program.cs                     # Application entry point
├── TestInvoices/                      # Sample invoice files for testing
├── Azure-Deployment-Guide.md          # Azure setup instructions
├── SQL-Database-Schema.sql            # Database schema setup
└── README.md                         # This file
```

## 🔧 Configuration

### Required Environment Variables / App Settings

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your Azure SQL connection string"
  },
  "BlobStorage": {
    "ConnectionString": "Your Azure Storage connection string",
    "ContainerName": "invoices"
  },
  "FormRecognizer": {
    "Endpoint": "Your Azure Form Recognizer endpoint",
    "ApiKey": "Your Azure Form Recognizer API key",
    "ModelId": "prebuilt-invoice"
  }
}
```

## 🧪 Testing

### Sample Test Files
The repository includes sample invoice PDFs in the `TestInvoices/` directory for testing the upload and processing functionality.

### Running Tests
```bash
# Upload a test invoice
curl -X POST http://localhost:5000/Home/Upload \
     -F "file=@TestInvoices/PDF/JiffyLube_OilChange_VEH001_JL2025001.pdf"

# View processed invoices
curl http://localhost:5000/api/Invoices
```

## 🔒 Security Considerations

- **Secrets Management**: Use Azure Key Vault for production secrets
- **Authentication**: Implement Azure AD authentication for production use
- **HTTPS**: Always use HTTPS in production environments
- **SQL Injection**: Entity Framework provides built-in protection
- **File Upload**: Validate file types and implement size limits

## 🚀 Deployment

### Azure App Service Deployment
```bash
# Build and publish
dotnet publish --configuration Release

# Deploy to Azure App Service
az webapp deployment source config-zip \
    --resource-group "your-resource-group" \
    --name "your-app-name" \
    --src "./publish.zip"
```

For detailed deployment instructions, see [Azure-Deployment-Guide.md](Azure-Deployment-Guide.md).

## 🐛 Troubleshooting

### Common Issues

1. **InvalidCastException: Unable to cast System.Int32 to System.Decimal**
   - **Solution**: Run the schema migration script: `Schema-Migration-2025-08-14.sql`

2. **Form Recognizer API Errors**
   - **Solution**: Verify your API key and endpoint in configuration

3. **Blob Storage Upload Failures**
   - **Solution**: Check storage account permissions and connection string

See the [Azure Deployment Guide](Azure-Deployment-Guide.md) for detailed troubleshooting steps.

## 📈 Performance Considerations

- **Database Indexing**: Optimized indexes for common query patterns
- **File Storage**: Efficient blob storage with CDN caching
- **API Rate Limits**: Form Recognizer API has rate limits to consider
- **Pagination**: Large result sets are paginated for performance

## 🔄 Version History

- **v1.0.0** - Initial release with core functionality
- **v1.0.1** - Fixed decimal casting issues in database schema
- **v1.0.2** - Enhanced error handling and logging

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Authors

- **Adrian KD** - *Initial work* - [adriankd](https://github.com/adriankd)

## 🙏 Acknowledgments

- Azure Form Recognizer team for excellent OCR capabilities
- ASP.NET Core team for the robust framework
- Bootstrap team for the UI components

## 📞 Support

For support and questions:
- Create an issue in this repository
- Check the [troubleshooting section](#-troubleshooting)
- Review the [Azure Deployment Guide](Azure-Deployment-Guide.md)
