# Vehicle Maintenance Invoice Processing System - Usage Guide

## Overview
The Vehicle Maintenance Invoice Processing System is a web application that uses Azure AI Form Recognizer to extract structured data from vehicle maintenance invoices. It processes both PDF and image files, storing the extracted data in an Azure SQL Database.

## Getting Started

### 1. Application Access
- **Web Interface**: http://localhost:5000
- **API Documentation (Swagger)**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health

### 2. Main Features
- Upload and process vehicle maintenance invoices (PDF/PNG/JPG)
- Automatic data extraction using Azure AI Form Recognizer
- View processed invoices by vehicle or date
- Export data to Excel
- RESTful API for integration

## Using the Web Interface

### Home Page (http://localhost:5000)
The main dashboard displays:
- Recent invoices processed
- Quick stats (total invoices, vehicles tracked)
- Navigation to key features

### Upload Invoice
1. Click **"Upload Invoice"** from the main menu
2. Select your invoice file (PDF, PNG, or JPG)
3. The system will:
   - Upload the file to Azure Blob Storage
   - Process it with Azure Form Recognizer
   - Extract structured data
   - Store results in the database

### View Invoices
- **By Vehicle**: Browse invoices for a specific vehicle ID
- **By Date**: View all invoices processed on a specific date
- **Recent**: See the most recently processed invoices

### Invoice Details
Each invoice shows:
- **Header Information**: Vehicle ID, Invoice Number, Date, Totals
- **Line Items**: Individual parts, labor, and fees
- **Confidence Scores**: OCR accuracy indicators
- **Original File**: Link to view the original uploaded document

## API Usage (Swagger Documentation)

### Key API Endpoints

#### POST /api/invoices/upload
Upload and process a new invoice file.

**Request**: Multipart form data with invoice file
**Response**: Processing status and invoice ID

```json
{
  "invoiceId": 123,
  "status": "Processing",
  "message": "Invoice uploaded successfully",
  "processingTime": 2500
}
```

#### GET /api/invoices/{id}
Get detailed information for a specific invoice.

**Response**:
```json
{
  "invoiceId": 123,
  "vehicleId": "VEH-001",
  "invoiceNumber": "JL2025001",
  "invoiceDate": "2025-08-14",
  "totalCost": 89.95,
  "totalPartsCost": 45.00,
  "totalLaborCost": 35.00,
  "confidenceScore": 96.5,
  "lineItems": [
    {
      "lineNumber": 1,
      "description": "Motor Oil - 5W30",
      "unitCost": 25.00,
      "quantity": 1,
      "totalLineCost": 25.00,
      "category": "Parts",
      "confidenceScore": 98.0
    }
  ]
}
```

#### GET /api/invoices/vehicle/{vehicleId}
Get all invoices for a specific vehicle.

#### GET /api/invoices/date/{date}
Get all invoices for a specific date (YYYY-MM-DD format).

## Test Data

### Available Test Invoices
The system includes 20 test invoices in the `TestInvoices` folder:

**PDF Format (15 files)**:
- Jiffy Lube Oil Change (VEH001)
- Honda Multi-Service (VEH002)
- Mike's Auto Brake Service (VEH003)
- Valvoline Service Package (VEH004)
- Midas Brake Service (VEH005)
- Ford Transmission Repair (VEH006)
- Take 5 Simple Oil (VEH007)
- Toyota 30K Service (VEH008)
- Downtown Timing Belt (VEH009)
- Firestone Tire Service (VEH010)
- Transmission World Rebuild (VEH011)
- Brake Masters Front Brakes (VEH012)
- Auto Glass Windshield (VEH013)
- Express Multi-Service (VEH014)
- Jiffy Lube+ Extended Service (VEH015)

**PNG Format (5 files)**:
- QuickStop Handwritten (VEH016)
- Mobile Mike On-Site (VEH017)
- Classic Car Restoration (VEH018)
- RV Service Winterization (VEH019)
- Fleet Mobile On-Site (VEH020)

### Testing Workflow
1. Start with simple invoices like "Jiffy Lube Oil Change"
2. Try more complex ones like "Honda Multi-Service"
3. Test different formats with handwritten elements
4. Verify data extraction accuracy

## PowerShell Testing Scripts

### Quick Test Script
```powershell
# Test a single invoice upload
$filePath = "C:\Training\fwmainplus\TestInvoices\PDF\html2pdf\JiffyLube_OilChange_VEH001_JL2025001.pdf"
$uri = "http://localhost:5000/api/invoices/upload"
$form = @{
    file = Get-Item $filePath
}
Invoke-RestMethod -Uri $uri -Method Post -Form $form
```

### Batch Testing
```powershell
# Test multiple invoices
$testFiles = Get-ChildItem "C:\Training\fwmainplus\TestInvoices\PDF\html2pdf\*.pdf"
foreach ($file in $testFiles) {
    Write-Host "Processing: $($file.Name)"
    $form = @{ file = $file }
    try {
        $result = Invoke-RestMethod -Uri "http://localhost:5000/api/invoices/upload" -Method Post -Form $form
        Write-Host "Success: Invoice ID $($result.invoiceId)" -ForegroundColor Green
    }
    catch {
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    Start-Sleep -Seconds 2
}
```

## Data Validation

### Confidence Scores
- **90-100%**: High confidence, data likely accurate
- **80-89%**: Good confidence, manual review recommended
- **Below 80%**: Low confidence, manual verification required

### Common Extraction Patterns
The system recognizes these invoice formats:
- **Single table**: Mixed parts and labor in one table
- **Separate sections**: Parts table + Labor table
- **Service packages**: Bundled pricing with detailed breakdowns

### Data Categories
- **Parts**: Physical components and fluids
- **Labor**: Service time and diagnostic work
- **Tax**: Sales tax and fees
- **Supplies**: Shop supplies and environmental fees

## Troubleshooting

### Common Issues
1. **Low confidence scores**: Try higher resolution images or clearer PDFs
2. **Missing data**: Ensure invoice has clear table structure
3. **Wrong totals**: Check for handwritten modifications or unusual formats

### Error Messages
- **"File format not supported"**: Use PDF, PNG, or JPG files
- **"Processing failed"**: Check Azure Form Recognizer service status
- **"Database connection error"**: Verify Azure SQL Database connectivity

## Integration Examples

### REST API Integration
```csharp
// C# example for uploading invoice
using var client = new HttpClient();
using var form = new MultipartFormDataContent();
using var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
form.Add(fileContent, "file", fileName);

var response = await client.PostAsync("http://localhost:5000/api/invoices/upload", form);
var result = await response.Content.ReadAsStringAsync();
```

### PowerBI Integration
Use the REST API endpoints to create PowerBI reports:
- Vehicle maintenance costs by month
- Part vs. labor cost analysis
- Vendor comparison reports
- Maintenance frequency tracking

## Best Practices

### File Preparation
- Use high-resolution scans (300 DPI minimum)
- Ensure text is clearly readable
- Avoid skewed or rotated images
- Keep file sizes reasonable (< 50MB)

### Data Management
- Regularly review low-confidence extractions
- Validate totals against line item sums
- Monitor processing logs for errors
- Archive processed files appropriately

## Support and Monitoring

### Health Monitoring
- Check `/health` endpoint for system status
- Monitor Azure services through Azure portal
- Review application logs for errors

### Performance Optimization
- Process invoices during off-peak hours for better performance
- Use batch processing for multiple files
- Monitor Azure Form Recognizer usage limits

---

## Quick Start Checklist

✅ Application running on http://localhost:5000  
✅ Azure SQL Database connected  
✅ Azure Blob Storage configured  
✅ Azure Form Recognizer service ready  
✅ Test invoices available in TestInvoices folder  

**Ready to process your first invoice!**

Visit http://localhost:5000 to get started, or try the Swagger API at http://localhost:5000/swagger for API testing.
