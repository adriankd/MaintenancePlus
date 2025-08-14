# Test Invoices for Vehicle Maintenance Invoice Processing System

## Overview
This folder contains 20 test invoices (15 PDF + 5 PNG) from various automotive service vendors. These invoices are designed to test the different format scenarios outlined in the PRD:

1. **Separate Parts and Labor Sections**
2. **Combined Line Items with Type Classification**
3. **Service-Based Invoices (Mixed Parts/Labor per line)**

## Vendor Types Represented

### Quick Lube Chains
- Jiffy Lube (Simple format, basic services)
- Valvoline Instant Oil Change (Standard format)
- Take 5 Oil Change (Minimal format)

### Auto Dealerships  
- Honda Service Center (Detailed format with part numbers)
- Toyota Service Department (Professional format)
- Ford Service Center (Complex multi-service)

### Independent Shops
- Mike's Auto Repair (Handwritten-style format)
- Downtown Automotive (Basic computer format)
- Express Auto Service (Mixed services)

### Chain Repair Shops
- Jiffy Lube+ (Extended services)
- Midas Auto Repair (Comprehensive format)
- Firestone Complete Auto Care (Professional format)

### Specialty Services
- Transmission World (Specialized format)
- Brake Masters (Service-focused)
- Auto Glass Express (Parts-heavy format)

## Invoice Scenarios Tested

### Format Variations
- **Single table with mixed line items** (most common)
- **Separate parts and labor sections**
- **Service packages with bundled pricing**
- **Detailed part numbers vs. descriptive text**
- **Various tax and fee structures**

### Data Extraction Challenges
- Different vendor logos and headers
- Varying table layouts and column orders
- Mixed currency formatting ($XX.XX vs XX.XX)
- Different date formats
- Various vehicle ID formats
- Handwritten vs. printed elements

## File Naming Convention
```
[VendorType]_[ServiceType]_[VehicleID]_[InvoiceNumber].[extension]
```

Example: `JiffyLube_OilChange_VEH001_JL240814001.pdf`

## Expected Extraction Results
Each invoice should extract to:
- **InvoiceHeader**: Vehicle ID, Invoice Number, Date, Totals
- **InvoiceLines**: Individual parts, labor, fees with proper categorization

## Usage Instructions
1. Use these files to test the OCR extraction pipeline
2. Verify the data mapping matches PRD specifications  
3. Test confidence scoring and error handling
4. Validate vendor format detection logic

## Conversion Notes
The HTML files in this folder can be converted to PDF/PNG using:
- **PDF**: Print to PDF from browser or use tools like wkhtmltopdf
- **PNG**: Screenshot capture or HTML to image conversion tools

For testing purposes, these represent realistic invoice formats that would be encountered in production.
