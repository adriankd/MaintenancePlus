# Changelog

All notable changes to the Vehicle Maintenance Invoice Processing System will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Phase 2 Intelligence System implementation
- Rule-based line item classification (Part vs Labor)
- Field label normalization with dictionary-based lookup
- User feedback collection for classification corrections
- Invoice approval/rejection workflow
- Intelligence processing API endpoints
- Classification accuracy monitoring

### Changed
- Enhanced database schema with intelligence tracking tables
- Updated UI to display classification results with confidence scores
- Improved invoice details page with approval controls

### Fixed
- **Odometer/Mileage Parsing**: Fixed extraction of comma-separated mileage readings (e.g., `67,890 miles`)
  - Added regex pattern `\d{1,3}(?:,\d{3})+` to detect comma-formatted numbers
  - Remove commas before parsing to integer
  - Maintains backward compatibility with non-comma numbers
  - Tested with various formats: 67,890; 123,456; 1,234,567
- **Code Quality**: Implemented CodeRabbit setup and applied code review recommendations
  - Enhanced error handling and validation
  - Improved code documentation and comments
  - Applied best practices for C# development

## [2.0.0] - 2025-08-19

### Added
- **Phase 2 Intelligence System**
  - Automatic line item classification as "Part" or "Labor"
  - Field label normalization for consistent data mapping
  - User feedback collection system for continuous improvement
  - Intelligence API endpoints for classification management
  - Accuracy tracking and monitoring capabilities

### Changed
- **Database Schema Enhancements**
  - Added classification columns to InvoiceLines table
  - Added normalization columns to InvoiceHeader table
  - Created ClassificationFeedback table for user corrections
  - Created FieldNormalizationFeedback table for field mapping improvements
  - Added ClassificationAccuracyLog for performance monitoring

### Enhanced
- **User Interface Improvements**
  - Display classification results with confidence scores
  - Show normalized field labels alongside original extractions
  - Add feedback buttons for correcting misclassifications
  - Enhanced invoice details page with intelligence data

## [1.0.0] - 2025-08-14

### Added
- **Core Invoice Processing System**
  - PDF and PNG invoice file upload and processing
  - Azure Form Recognizer integration for OCR data extraction
  - Multi-vendor invoice format support
  - Azure SQL Database storage with normalized schema
  - Azure Blob Storage for original file archival
  - RESTful API for invoice data access
  - Web interface for invoice management

### Features
- **File Processing**
  - Support for PDF and PNG formats up to 10MB
  - Multi-vendor invoice layout detection and handling
  - Automatic data extraction with confidence scoring
  - Error handling and manual review workflows

- **Data Management**
  - Normalized database schema for invoice headers and line items
  - Secure file storage in Azure Blob Storage
  - API endpoints for invoice CRUD operations
  - Invoice approval/rejection workflow

- **User Interface**
  - Web-based invoice upload interface
  - Invoice listing with pagination and filtering
  - Detailed invoice view with original file access
  - Approval workflow with confirmation dialogs

### Security
- HTTPS encryption for all communications
- Secure blob storage access with time-limited URLs
- Audit logging for all file access operations
- Data validation and sanitization

---

## Technical Notes

### Bug Fix Details

#### Odometer/Mileage Parsing Fix (2025-08-21)
**Problem**: The system was failing to extract odometer readings containing comma separators (e.g., "67,890 miles").

**Root Cause**: The existing regex pattern `\d+` only matched continuous digits and would stop at the comma, extracting only "67" instead of "67890".

**Solution**: 
- Added prioritized regex pattern `\d{1,3}(?:,\d{3})+` to specifically match comma-separated numbers
- Remove commas before integer parsing
- Maintained fallback to original logic for non-comma numbers
- Added comprehensive test coverage

**Impact**: 
- Fixes data extraction accuracy for invoices with formatted odometer readings
- Improves overall system reliability for numeric data extraction
- No breaking changes to existing functionality

**Files Modified**:
- `src/Services/FormRecognizerService.cs` - Enhanced odometer extraction logic

**Testing**:
- ✅ Unit tests with 9/9 test cases passing (100% success rate)
- ✅ Integration testing with real invoice processing
- ✅ Backward compatibility verified for existing formats

#### CodeRabbit Integration (2025-08-19)
**Enhancement**: Set up automated code review and quality analysis

**Improvements Applied**:
- Enhanced error handling patterns
- Improved code documentation and XML comments
- Applied C# coding best practices
- Standardized naming conventions
- Added input validation improvements

**Tools Configured**:
- CodeRabbit AI code review integration
- Automated pull request analysis
- Code quality metrics tracking

---

## Migration Notes

### Database Schema Changes

When upgrading to version 2.0.0, run the following migration scripts:

```sql
-- Add intelligence columns to existing tables
ALTER TABLE InvoiceHeader 
ADD OriginalVehicleLabel NVARCHAR(100) NULL,
    OriginalOdometerLabel NVARCHAR(100) NULL,
    OriginalInvoiceLabel NVARCHAR(100) NULL,
    NormalizationVersion NVARCHAR(20) NULL;

ALTER TABLE InvoiceLines
ADD ClassifiedCategory NVARCHAR(50) NOT NULL DEFAULT 'Unclassified',
    ClassificationConfidence DECIMAL(5,2) NULL,
    ClassificationMethod NVARCHAR(50) NOT NULL DEFAULT 'Rule-based',
    ClassificationVersion NVARCHAR(20) NULL,
    OriginalCategory NVARCHAR(100) NULL,
    ExtractionConfidence DECIMAL(5,2) NULL;
```

### Configuration Updates

Ensure the following configuration settings are updated:
- Azure Form Recognizer endpoint and keys
- Intelligence service feature flags
- Classification model settings
- Database connection strings

---

## Support

For technical support or questions about these changes:
- Review the [Development Guidelines](DEVELOPMENT_GUIDELINES.md)
- Check the [Phase 2 Testing Guide](Phase2-Testing-Guide.md)
- Refer to the [Application Usage Guide](APPLICATION_USAGE_GUIDE.md)
