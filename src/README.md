# Vehicle Maintenance Invoice Processing System

A comprehensive C# ASP.NET Core application that automates the extraction and processing of vehicle maintenance invoices from PDF and PNG files using Azure AI services.

## Features

- **File Upload**: Web interface and API endpoints for uploading PDF/PNG invoices
- **OCR Processing**: Azure Form Recognizer integration for data extraction
- **Data Storage**: Azure SQL Database with normalized schema
- **File Archival**: Azure Blob Storage for original document storage
- **RESTful API**: Complete API for external system integration
- **Web Interface**: User-friendly web application for manual operations

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- Azure SQL Database (or SQL Server LocalDB for development)
- Azure Storage Account
- Azure Form Recognizer resource

### Configuration

1. Update `appsettings.json` with your Azure connection strings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-sql-connection-string"
  },
  "BlobStorage": {
    "ConnectionString": "your-blob-storage-connection-string",
    "ContainerName": "invoices"
  },
  "FormRecognizer": {
    "Endpoint": "your-form-recognizer-endpoint",
    "ApiKey": "your-form-recognizer-api-key"
  }
}
```

### Running the Application

```bash
cd src
dotnet restore
dotnet run
```

Navigate to:
- Web Interface: `https://localhost:5001`
- API Documentation: `https://localhost:5001/swagger`

## API Endpoints

### POST /api/invoices/upload
Upload and process a new invoice file.

**Request**: Multipart form data with file
**Response**: Processing status and invoice ID

### GET /api/invoices
Retrieve all invoices with pagination.

**Parameters**:
- `page` (optional): Page number
- `pageSize` (optional): Items per page (max 100)

### GET /api/invoices/{id}
Retrieve specific invoice with all line items.

### GET /api/invoices/vehicle/{vehicleId}
Search invoices by vehicle ID.

### GET /api/invoices/date/{date}
Retrieve invoices by processing date (YYYY-MM-DD format).

## Database Schema

### InvoiceHeader Table
- Stores invoice summary information
- Links to original file in blob storage
- Includes OCR confidence scores

### InvoiceLines Table
- Stores individual line items
- Categorizes items as Parts, Labor, Tax, etc.
- Links to parent invoice header

## Architecture

```
Web Interface / API
        ↓
Business Logic Services
        ↓
┌─────────────────┬─────────────────┬─────────────────┐
│ Azure Blob      │ Azure Form      │ Azure SQL       │
│ Storage         │ Recognizer      │ Database        │
│ (File Storage)  │ (OCR Service)   │ (Data Storage)  │
└─────────────────┴─────────────────┴─────────────────┘
```

## Development

### Project Structure

```
src/
├── Controllers/          # MVC and API controllers
├── Data/                # Entity Framework DbContext
├── Models/              # Data models and DTOs
├── Services/            # Business logic services
├── Views/               # Razor views for web interface
└── Program.cs          # Application startup
```

### Key Services

- **BlobStorageService**: Handles file upload/download operations
- **FormRecognizerService**: OCR processing and data extraction
- **InvoiceProcessingService**: Orchestrates the complete processing workflow

## Deployment

See [Azure-Deployment-Guide.md](../Azure-Deployment-Guide.md) for detailed deployment instructions.

## User Stories Implementation

This implementation covers all user stories from the PRD:

✅ **US-001**: Invoice File Upload  
✅ **US-002**: Original File Storage  
✅ **US-003**: Invoice Header Data Extraction  
✅ **US-004**: Invoice Line Items Extraction  
✅ **US-005**: Database Storage of Extracted Data  
✅ **US-006**: Data Validation and Quality Assurance  
✅ **US-007**: Retrieve All Invoices API  
✅ **US-008**: Retrieve Single Invoice API  
✅ **US-009**: Search Invoices by Vehicle API  
✅ **US-010**: Retrieve Invoices by Date API  
✅ **US-011**: Processing Status Tracking  
✅ **US-012**: Error Handling and Recovery  
✅ **US-013**: Asynchronous Processing (via background services)

## Testing

Use the provided test invoices in the `TestInvoices/` folder to validate the system:

```bash
# Test with sample invoices
curl -X POST "https://localhost:5001/api/invoices/upload" \
     -H "Content-Type: multipart/form-data" \
     -F "file=@TestInvoices/PDF/sample_invoice.pdf"
```

## Monitoring

- Application logs are written to `logs/` directory
- Swagger UI provides API testing interface
- Web interface shows processing confidence scores
- Database includes audit timestamps for all operations

## Support

For issues or questions:
1. Check the logs in `logs/app-*.txt`
2. Verify Azure service connectivity
3. Review API documentation at `/swagger`
4. Check database connection and schema
