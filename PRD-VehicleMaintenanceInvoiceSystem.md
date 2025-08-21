# Product Requirements Document (PRD)
## Vehicle Maintenance Invoice Processing System

### Document Information
- **Product Name**: Vehicle Maintenance Invoice Processing System
- **Version**: 2.0
- **Date**: August 21, 2025
- **Product Manager**: [To be assigned]
- **Development Team**: 2 Developers (Backend/Frontend)

---

## 1. Executive Summary

### Product Vision
Develop a cloud-native C# web application that automates the extraction and processing of vehicle maintenance invoices from PDF and PNG files, storing structured data in Azure SQL Database while providing RESTful API access for system integration.

### Business Objectives
- **Automation**: Eliminate manual data entry for vehicle maintenance invoices
- **Integration**: Provide API-driven access to structured invoice data
- **Scalability**: Leverage Azure cloud services for enterprise-scale deployment
- **Compliance**: Maintain audit trails with original document storage
- **Intelligence**: Automatically classify invoice line items and normalize field labels
- **Continuous Learning**: Implement feedback loops for improving classification accuracy over time

### Success Metrics
- 95% accuracy in data extraction from supported invoice formats
- 90% accuracy in line item classification (Part vs Labor)
- 85% accuracy in field label normalization
- Processing time < 30 seconds per invoice
- 99.9% API uptime
- Support for 1000+ invoices per day
- Secure file access with audit logging

---

## 2. Product Overview

### Core Functionality
The system processes vehicle maintenance invoices through the following workflow:
1. **Upload**: Users upload PDF/PNG invoice files via web interface
2. **Extract**: Azure Form Recognizer extracts structured data from documents
3. **Normalize**: Field labels are standardized using the Field Label Normalization engine
4. **Classify**: Line items are automatically classified as "Part" or "Labor" using ML classification
5. **Store**: Structured data saved to Azure SQL Database with normalized labels and classifications
6. **Archive**: Original files stored in Azure Blob Storage with secure access
7. **Review**: Users can view invoice details and approve or reject the processed invoice
8. **Feedback**: Users can correct misclassifications to improve future accuracy
9. **Approve/Reject**: Users can approve invoices for final acceptance or reject and delete them
10. **Access**: RESTful API provides data access for external systems
11. **View**: Users can view/download original invoice files through secure links

### Target Users
- **Primary**: Fleet managers and maintenance administrators
- **Secondary**: External systems requiring invoice data integration
- **Tertiary**: Auditors requiring document access and original file review

---

## 3. Functional Requirements

### 3.1 File Processing Module

#### FR-001: File Upload
- **Description**: Support upload of PDF and PNG invoice files
- **Acceptance Criteria**:
  - Accept file sizes up to 10MB
  - Validate file format (PDF/PNG only)
  - Display upload progress
  - Provide error messages for invalid files

#### FR-002: OCR Data Extraction
- **Description**: Extract structured data using Azure Form Recognizer to handle variable vendor invoice formats
- **Acceptance Criteria**:
  - Extract Invoice Header data with 95% accuracy
  - Extract Invoice Line items with 90% accuracy
  - Handle multiple vendor invoice formats and layouts
  - Support varying table structures (separate vs combined line items)
  - Provide confidence scores for extracted data
  - Route low-confidence extractions for manual review

#### FR-003: Numeric Data Extraction with Format Handling
- **Description**: Extract and parse numeric data from invoices, handling various formatting conventions
- **Acceptance Criteria**:
  - **Odometer/Mileage Reading Extraction**:
    - Support standard numeric formats (e.g., "67890", "123456")
    - Support comma-separated numeric formats (e.g., "67,890", "123,456", "1,234,567")
    - Automatically detect and parse both formats without user intervention
    - Convert comma-separated numbers to standard integers for database storage
    - Maintain extraction accuracy of 95% across all numeric formats
    - Handle edge cases such as leading/trailing spaces around numbers
    - Preserve original extracted text alongside parsed numeric value for audit purposes
  - **Currency and Cost Extraction**:
    - Support various currency formats with and without thousand separators
    - Handle decimal precision for monetary values
    - Extract totals, line costs, and tax amounts accurately
  - **General Numeric Field Processing**:
    - Apply consistent formatting rules across all numeric fields
    - Validate extracted numbers are within expected ranges
    - Flag unusual or suspicious numeric values for manual review

#### FR-004: Data Validation
- **Description**: Validate extracted data before database storage
- **Acceptance Criteria**:
  - Validate required fields are present
  - Check data type consistency
  - Validate numeric data is properly formatted and within expected ranges
  - Flag incomplete extractions for manual review

#### FR-005: Part Number Extraction and Storage
- **Description**: Extract part numbers from parts-only invoice line items and store them in the database for inventory and procurement tracking
- **Acceptance Criteria**:
  - **Line Item Type Filtering**:
    - Only attempt part number extraction for line items classified as "Parts" 
    - Do not extract part numbers from Labor, Tax, Fee, or Service line items
    - Apply part number extraction after line item classification is complete
  - **Extraction Source Limitations**:
    - Extract part numbers ONLY from dedicated part number columns in invoice tables
    - Do not attempt to parse or extract part numbers from item descriptions
    - Do not use regex pattern matching on description text
    - Only use explicitly labeled part number fields (e.g., "Part Number", "Part No", "PN", "Item Number")
  - **Data Processing Requirements**:
    - Store extracted part numbers in the `PartNumber` column of `InvoiceLines` table
    - Preserve original format and casing of part numbers exactly as found
    - Handle missing part number columns gracefully (store as NULL)
    - Store empty/blank part number fields as NULL (not empty string)
  - **Quality and Validation**:
    - Achieve 95% accuracy in part number extraction from dedicated columns when present
    - Only extract data from clearly identified part number table columns
    - Flag invoices without part number columns for manual review if needed
    - Maintain audit trail showing which column was used for part number extraction

#### FR-006: Data Mapping and Transformation
- **Description**: Transform extracted invoice data into normalized database structure
- **Acceptance Criteria**:
  - Map header fields to InvoiceHeader table columns
  - Map all line items to InvoiceLines table with proper categorization
  - **Extract and store part numbers from dedicated part number columns for Parts-only line items**
  - **Only extract part numbers from clearly labeled part number table columns, not descriptions**
  - Calculate and validate totals (parts vs labor vs total cost)
  - Assign sequential line numbers to detail items
  - Classify line items by type (Parts, Labor, Tax, Fees, Services)
  - Maintain referential integrity between header and line records
  - Handle missing or incomplete line item data gracefully
  - **Store part numbers in the PartNumber column only for Parts line items**
  - **Store NULL for PartNumber when no dedicated part number column exists or item is not a Part**

#### FR-007: Line Item Classification
- **Description**: Automatically classify invoice line items as "Part" or "Labor" based on description text analysis
- **Acceptance Criteria**:
  - Implement keyword-based classification rules as initial solution
  - Achieve 85% accuracy on initial keyword-based classification
  - Store classification results in database with confidence scores
  - Support user feedback to correct misclassifications
  - Log all classification decisions for model training
  - Provide fallback classification when confidence is low
  - Handle edge cases (mixed items, unclear descriptions)
  - Support batch reclassification of historical data

#### FR-008: Machine Learning Classification Enhancement
- **Description**: Evolve from rule-based to ML-based line item classification for improved accuracy
- **Acceptance Criteria**:
  - Implement ML.NET text classification model or Azure AI Language custom classifier
  - Train model using accumulated feedback data from keyword-based phase
  - Achieve 90% accuracy on line item classification
  - Support incremental model retraining with new feedback data
  - Provide confidence scores for ML-based classifications
  - Maintain backward compatibility with existing classification data
  - Implement A/B testing framework to compare rule-based vs ML approaches
  - Support model versioning and rollback capabilities

#### FR-009: Field Label Normalization
- **Description**: Standardize inconsistent field labels from various invoice formats to database schema
- **Acceptance Criteria**:
  - Implement dictionary-based lookup for common field variations
  - Normalize "Invoice"/"Invoice No"/"RO#" → "InvoiceNumber"
  - Normalize "Mileage"/"Odometer"/"Miles" → "Mileage"  
  - Normalize "Vehicle ID"/"Vehicle Registration" → "VehicleRegistration"
  - Store original extracted labels alongside normalized versions
  - Support user feedback to improve normalization rules
  - Log all normalization decisions for analysis and improvement
  - Handle new/unseen field label variations gracefully
  - Maintain mapping history for audit purposes

#### FR-010: Semantic Field Normalization Enhancement
- **Description**: Enhance field normalization with semantic similarity matching for unseen variations
- **Acceptance Criteria**:
  - Implement embedding-based semantic similarity model
  - Handle new field label variations not in the dictionary
  - Achieve 80% accuracy on field label normalization
  - Support user corrections to improve semantic matching
  - Store embeddings for efficient similarity calculations
  - Provide confidence scores for semantic matches
  - Support threshold-based fallback to manual review
  - Enable continuous learning from user feedback

#### FR-011: Unified Processing Pipeline
- **Description**: Integrate normalization and classification into the core processing workflow
- **Acceptance Criteria**:
  - Execute field normalization immediately after Form Recognizer extraction
  - Perform line item classification before database storage
  - Maintain processing order: Extract → Normalize → Classify → Store
  - Support modular architecture for easy classifier/normalizer updates
  - Preserve original extracted data alongside processed versions
  - Handle processing failures gracefully with partial results
  - Support pipeline configuration and feature toggling
  - Maintain processing metadata and audit trails

#### FR-012: Feedback and Continuous Learning System
- **Description**: Implement user feedback mechanisms to improve classification and normalization accuracy
- **Acceptance Criteria**:
  - Provide UI for users to correct line item classifications
  - Enable users to update field label normalizations
  - Store all user corrections with timestamps and user identification
  - Implement feedback aggregation to identify common correction patterns
  - Support periodic model retraining with accumulated feedback
  - Provide feedback statistics and accuracy trend reporting
  - Enable bulk correction capabilities for systematic issues
  - Maintain feedback audit trail for compliance and analysis

### 3.2 Data Storage Module

#### FR-013: Database Storage
- **Description**: Store structured invoice data in Azure SQL Database using normalized table structure with classification and normalization results
- **Acceptance Criteria**:
  - Map extracted header information to InvoiceHeader table
  - Map all detail lines (parts, labor, services) to InvoiceLines table
  - Store line item classifications (Part/Labor) with confidence scores
  - Store field label normalizations (original and normalized versions)
  - Store user feedback and corrections for continuous learning
  - Maintain foreign key relationships between header and lines
  - Store processing metadata and confidence scores
  - Maintain data integrity with transactions
  - Record processing timestamps

#### FR-014: File Archival
- **Description**: Store original files in Azure Blob Storage
- **Acceptance Criteria**:
  - Store all files in a single container
  - Generate unique file identifiers
  - Maintain file metadata
  - Provide secure access URLs

#### FR-015: Original File Access
- **Description**: Provide user interface and API access to original invoice files stored in blob storage
- **Acceptance Criteria**:
  - Display "View Original" button/link on invoice details pages
  - Support both in-browser viewing and file download
  - Generate secure, time-limited access URLs for blob files
  - Handle PDF files with inline browser viewing capability
  - Handle PNG files with inline image display
  - Provide fallback download option for unsupported browsers
  - Log file access attempts for audit purposes

### 3.3 API Module

#### FR-016: RESTful API
- **Description**: Provide RESTful endpoints for data access
- **Acceptance Criteria**:
  - Return JSON responses
  - Support pagination for large result sets
  - Include proper HTTP status codes
  - Provide comprehensive error messages

### 3.4 User Interface Module

#### FR-017: Invoice Details View
- **Description**: Provide comprehensive invoice details page with original file access and classification feedback
- **Acceptance Criteria**:
  - Display all invoice header information in readable format
  - Show line items in organized table format with Part/Labor classifications
  - Include "View Original File" button/link prominently displayed
  - Support in-browser PDF viewing for compatible browsers
  - Support in-browser image viewing for PNG files
  - Provide "Download Original" option as fallback
  - Display file metadata (filename, size, upload date)
  - Show loading indicators during file access operations
  - Enable users to correct line item classifications with feedback buttons
  - Display confidence scores for classifications and normalizations

#### FR-018: File Access Security
- **Description**: Implement secure access controls for original invoice files
- **Acceptance Criteria**:
  - Generate time-limited SAS (Shared Access Signature) URLs for blob access
  - Set 1-hour expiration on file access links
  - Log all file access attempts with user identification
  - Prevent direct blob URL exposure in client-side code
  - Handle expired link scenarios gracefully with user-friendly messages

#### FR-019: Enhanced Invoice Details UI
- **Description**: Update invoice details page to support approval workflow and feedback collection
- **Acceptance Criteria**:
  - Display approval status badge prominently at the top of invoice details
  - Show "Pending Approval" badge in yellow/orange for unapproved invoices
  - Show "Approved" badge in green with approval date for approved invoices
  - Position "Approve" and "Reject" buttons prominently near the top of the page
  - Use distinct colors: Green for "Approve" button, Red for "Reject" button
  - Include icons on buttons for better UX (checkmark for approve, X for reject)
  - Show approval details (date, user) when invoice is approved
  - Disable/hide approval buttons for already approved invoices
  - Maintain existing "View Original File" functionality
  - Responsive design for mobile and desktop viewing
  - Add "Correct Classification" buttons next to line items for feedback collection

#### FR-020: Confirmation Dialog System
- **Description**: Implement user-friendly confirmation dialogs for critical actions
- **Acceptance Criteria**:
  - **Approve Confirmation**:
    - Title: "Approve Invoice"
    - Message: "Are you sure you want to approve this invoice? Once approved, it cannot be rejected or modified."
    - Buttons: "Yes, Approve" (green), "Cancel" (gray)
  - **Reject Confirmation**:
    - Title: "Reject Invoice"  
    - Message: "⚠️ Are you sure you want to reject this invoice? This will permanently delete the invoice data and original file. This action cannot be undone."
    - Buttons: "Yes, Delete Forever" (red), "Cancel" (gray)
  - **Success Messages**:
    - Approval: "✓ Invoice has been approved successfully"
    - Rejection: "✓ Invoice has been rejected and removed from the system"
  - **Error Handling**: Display user-friendly error messages if operations fail
  - Use modal dialogs that overlay the current page
  - Implement proper focus management and keyboard navigation

### 3.5 Invoice Approval Module

#### FR-021: Invoice Approval Process
- **Description**: Provide approval workflow for processed invoices with database status tracking
- **Acceptance Criteria**:
  - Display "Approve" button on invoice details page for unapproved invoices
  - Update `Approved` column in database to `true` when invoice is approved
  - Show visual confirmation when invoice status changes to approved
  - Display approval status clearly on invoice details page
  - Provide confirmation popup before approving: "Are you sure you want to approve this invoice?"
  - Only show "Approve" button for invoices with `Approved = false`
  - Track approval timestamp and user information

#### FR-022: Invoice Rejection Process
- **Description**: Provide rejection workflow that removes rejected invoices and files completely
- **Acceptance Criteria**:
  - Display "Reject" button on invoice details page for unapproved invoices
  - Show confirmation popup before rejecting: "Are you sure you want to reject this invoice? This will permanently delete the invoice data and original file."
  - When rejected, perform the following actions atomically:
    - Delete original file from Azure Blob Storage
    - Delete all invoice line items from InvoiceLines table
    - Delete invoice header record from InvoiceHeader table
  - Redirect user to upload page after successful rejection
  - Display success message: "Invoice has been rejected and removed from the system"
  - Handle errors gracefully if deletion fails (e.g., file already deleted, database constraints)
  - Log all rejection actions for audit purposes
  - Only show "Reject" button for invoices with `Approved = false`

#### FR-023: Approval Status Management
- **Description**: Manage invoice approval states and user interface updates
- **Acceptance Criteria**:
  - Add `Approved` boolean column to InvoiceHeader table with default value `false`
  - Display approval status badge on invoice details page ("Pending Approval" / "Approved")
  - Hide both "Approve" and "Reject" buttons for already approved invoices (`Approved = true`)
  - Show approval date and user information for approved invoices
  - Include approval status in API responses
  - Add approval status filter to invoice list views

---

## 4. API Specification

### Base URL
```
https://[app-name].azurewebsites.net/api
```

### Core Invoice Endpoints

#### GET /invoices
- **Purpose**: Retrieve all invoices with pagination and enhanced filtering
- **Parameters**: 
  - `page` (optional): Page number (default: 1)
  - `pageSize` (optional): Items per page (default: 20, max: 100)
  - `classification` (optional): Filter by line item classification status
  - `approvalStatus` (optional): Filter by approval status
- **Response**: Paginated list of invoice headers with classification metadata

#### GET /invoices/{id}
- **Purpose**: Retrieve specific invoice with line items and classification data
- **Parameters**: `id` (required): Invoice ID
- **Response**: Complete invoice details including line items with Part/Labor classifications, confidence scores, and normalization results

#### POST /invoices/upload
- **Purpose**: Upload and process new invoice
- **Body**: Multipart form data with file
- **Response**: Processing status and invoice ID

#### PUT /invoices/{id}/approve
- **Purpose**: Approve a processed invoice
- **Parameters**: `id` (required): Invoice ID
- **Response**: Success status and updated invoice data
- **Validation**: Only invoices with `Approved = false` can be approved

#### DELETE /invoices/{id}/reject
- **Purpose**: Reject and permanently delete an invoice
- **Parameters**: `id` (required): Invoice ID  
- **Response**: Success status confirmation
- **Actions**: Deletes blob file, invoice lines, and invoice header
- **Validation**: Only invoices with `Approved = false` can be rejected

#### GET /invoices/{id}/file
- **Purpose**: Access original invoice file from blob storage
- **Parameters**: `id` (required): Invoice ID
- **Response**: Secure redirect to blob storage URL or file stream
- **Security**: Time-limited access URL with 1-hour expiration

### Classification and Feedback Endpoints

#### POST /invoices/{id}/lines/{lineId}/feedback
- **Purpose**: Submit user feedback for line item classification correction
- **Parameters**: 
  - `id` (required): Invoice ID
  - `lineId` (required): Invoice line item ID
- **Body**: JSON with corrected classification and user information
- **Response**: Confirmation of feedback submission and updated line item data

#### GET /classifications/accuracy
- **Purpose**: Retrieve classification accuracy metrics and trends
- **Parameters**: 
  - `dateFrom` (optional): Start date for metrics
  - `dateTo` (optional): End date for metrics
  - `classifierType` (optional): Filter by classification type
- **Response**: Accuracy statistics, confidence distributions, and improvement trends

---

## 5. Non-Functional Requirements

### Performance
- **Response Time**: API responses < 2 seconds
- **Processing Time**: Invoice processing < 30 seconds (including classification and normalization)
- **Classification Time**: Line item classification < 5 seconds per invoice
- **Throughput**: Support 100 concurrent users
- **File Processing**: Handle up to 1000 invoices/day

### Security
- **Authentication**: Future implementation (Phase 2)
- **Data Encryption**: HTTPS for all communications
- **Access Control**: Azure AD integration capability
- **Audit Logging**: Track all data access and modifications
- **File Access Logging**: Log original file view/download activities

### Reliability
- **Availability**: 99.9% uptime SLA
- **Backup**: Daily automated database backups
- **Disaster Recovery**: Cross-region backup storage
- **Error Handling**: Graceful failure with detailed logging

### Scalability
- **Horizontal Scaling**: Azure App Service auto-scaling
- **Database Scaling**: Azure SQL elastic pools
- **Storage**: Unlimited blob storage capacity

### Data Extraction Accuracy
- **Numeric Field Extraction**: 95% accuracy for all numeric fields including comma-formatted numbers
- **Text Field Extraction**: 95% accuracy for standard text fields
- **Field Format Handling**: Support multiple formatting conventions without manual configuration
- **Error Recovery**: Graceful handling of unexpected data formats with fallback mechanisms

---

## 6. Technical Architecture

### Technology Stack
- **Frontend**: ASP.NET Core MVC / Blazor Server
- **Backend**: ASP.NET Core Web API
- **Database**: Azure SQL Database
- **Storage**: Azure Blob Storage
- **OCR**: Azure Form Recognizer
- **Hosting**: Azure App Service

### Azure Services Architecture
```
Internet → Azure App Service → Azure SQL Database
              ↓
         Azure Blob Storage
```

---

## 7. Database Schema

### InvoiceHeader Table
Stores one record per invoice with summary information extracted from invoice header.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| InvoiceID | INT IDENTITY | PRIMARY KEY | Auto-generated |
| VehicleID | NVARCHAR(50) | NOT NULL | Vehicle identifier on invoice (normalized) |
| OriginalVehicleLabel | NVARCHAR(100) | NULL | Original extracted vehicle field label |
| Odometer | INT | NULL | Mileage reading from invoice (supports comma-separated format) |
| OriginalOdometerLabel | NVARCHAR(100) | NULL | Original extracted odometer field label |
| OriginalOdometerText | NVARCHAR(50) | NULL | Original extracted odometer text (e.g., "67,890") |
| InvoiceNumber | NVARCHAR(50) | NOT NULL, UNIQUE | Invoice number from header (normalized) |
| OriginalInvoiceLabel | NVARCHAR(100) | NULL | Original extracted invoice field label |
| InvoiceDate | DATE | NOT NULL | Service/invoice date |
| TotalCost | DECIMAL(18,2) | NOT NULL | Grand total from invoice |
| TotalPartsCost | DECIMAL(18,2) | NOT NULL | Calculated sum of parts lines |
| TotalLaborCost | DECIMAL(18,2) | NOT NULL | Calculated sum of labor lines |
| BlobFileUrl | NVARCHAR(255) | NOT NULL | Azure Blob Storage URL |
| Approved | BIT | NOT NULL, DEFAULT 0 | Approval status |
| ApprovedAt | DATETIME2 | NULL | Timestamp when invoice was approved |
| ApprovedBy | NVARCHAR(100) | NULL | User who approved the invoice |
| ExtractedData | NVARCHAR(MAX) | NULL | Raw JSON of extracted data |
| ConfidenceScore | DECIMAL(5,2) | NULL | Overall extraction confidence |
| NormalizationVersion | NVARCHAR(20) | NULL | Version of normalization rules applied |
| NumericParsingVersion | NVARCHAR(20) | NULL | Version of numeric parsing logic applied |
| CreatedAt | DATETIME | DEFAULT GETDATE() | Record creation time |

### InvoiceLines Table
Stores multiple records per invoice - one for each line item with classification results.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| LineID | INT IDENTITY | PRIMARY KEY | Auto-generated |
| InvoiceID | INT | FOREIGN KEY | Links to InvoiceHeader |
| LineNumber | INT | NOT NULL | Sequential order in invoice |
| Description | NVARCHAR(500) | NOT NULL | Item/service description |
| UnitCost | DECIMAL(18,2) | NOT NULL | Price per unit/hour |
| Quantity | DECIMAL(10,2) | NOT NULL | Number of units/hours |
| TotalLineCost | DECIMAL(18,2) | NOT NULL | Extended line total |
| PartNumber | NVARCHAR(100) | NULL | Extracted part number if available |
| Category | NVARCHAR(100) | NULL | Parts, Labor, Tax, Fee, Service |
| ClassifiedCategory | NVARCHAR(50) | NOT NULL | Part or Labor (ML classification) |
| ClassificationConfidence | DECIMAL(5,2) | NULL | ML classification confidence score |
| ClassificationMethod | NVARCHAR(50) | NOT NULL | Rule-based, ML.NET, Azure-AI, Manual |
| ClassificationVersion | NVARCHAR(20) | NULL | Version of classification model used |
| OriginalCategory | NVARCHAR(100) | NULL | Original extracted category before classification |
| ExtractionConfidence | DECIMAL(5,2) | NULL | Line extraction confidence from OCR |
| CreatedAt | DATETIME2 | DEFAULT GETUTCDATE() | Record creation time |

---

## 8. Development Timeline (65 Working Days)

### Phase 1: Foundation & Core Features (Days 1-20)
- Azure resource provisioning
- Enhanced database schema implementation with numeric parsing support
- Basic web application structure
- File upload functionality
- Blob storage integration
- Azure Form Recognizer integration with robust numeric extraction
- Data extraction pipeline with format-aware numeric parsing
- API endpoints implementation
- Invoice approval/rejection workflow

### Phase 2: Intelligence Features - Rule-Based (Days 21-35)
- Rule-based classification engine
- Dictionary-based field normalization
- User feedback collection system
- Unified processing pipeline
- End-to-end testing with various numeric formats

### Phase 3: Machine Learning Enhancement (Days 36-50)
- ML.NET text classification model
- Model training and validation
- A/B testing system
- Batch reclassification capabilities
- Classification accuracy monitoring

### Phase 4: Advanced Intelligence (Days 51-65)
- Semantic similarity for field normalization
- Azure OpenAI integration
- Comprehensive monitoring dashboard
- Performance analytics
- System optimization

---

## 9. Success Criteria

### Phase 1 (MVP - Core Features)
- [ ] Process PDF and PNG invoices successfully
- [ ] Extract header and line item data with 90%+ accuracy
- [ ] **Extract numeric fields with 95% accuracy across all formats (standard and comma-separated)**
- [ ] **Extract part numbers from invoice line items with 90% accuracy when present**
- [ ] Handle at least 3 different vendor invoice formats
- [ ] Store data in Azure SQL Database with enhanced schema including part numbers
- [ ] Provide basic API endpoints
- [ ] Deploy to Azure App Service
- [ ] Implement secure original file access functionality
- [ ] Implement invoice approval/rejection workflow
- [ ] Add approval status tracking
- [ ] Provide confirmation dialogs for all approval/rejection actions

### Phase 2 (Intelligence Features)
- [ ] Implement rule-based line item classification
- [ ] Achieve 75% accuracy on line item classification
- [ ] Implement field label normalization
- [ ] Store original labels alongside normalized versions
- [ ] Display classification results with confidence scores
- [ ] Implement user feedback collection

### Phase 3 (Machine Learning Enhancement)
- [ ] Implement ML.NET text classification model
- [ ] Achieve 85% accuracy on line item classification
- [ ] Implement semantic similarity matching
- [ ] Support model versioning and rollback
- [ ] Implement A/B testing framework

### Phase 4 (Continuous Learning)
- [ ] Implement automated model retraining
- [ ] Achieve 90% accuracy on line item classification
- [ ] Create accuracy tracking dashboard
- [ ] Implement confidence-based manual review workflows

---

## 10. Risk Assessment

### High Priority Risks
1. **Vendor Format Variability**: Different vendors may have significantly different invoice layouts
   - *Mitigation*: Implement Azure Form Recognizer custom models per vendor
2. **OCR Accuracy**: Variable invoice formats may impact extraction accuracy
   - *Mitigation*: Use Azure Form Recognizer's adaptive learning
3. **Numeric Format Variations**: Invoices may use different numeric formatting conventions
   - *Mitigation*: Implement comprehensive regex patterns and parsing logic for multiple formats
4. **Azure Service Limits**: Form Recognizer API rate limits and costs
   - *Mitigation*: Implement queuing system and cost monitoring
5. **Data Privacy**: Sensitive financial information handling
   - *Mitigation*: Implement encryption at rest and in transit

---

## 11. Quality Assurance and Testing

### Test Coverage Requirements
- **Unit Tests**: 90% code coverage for data extraction and parsing logic
- **Integration Tests**: End-to-end invoice processing with various formats
- **Regression Tests**: Automated tests for numeric parsing edge cases
- **Performance Tests**: Load testing with 1000+ invoices
- **Security Tests**: Vulnerability scanning and penetration testing

### Numeric Extraction Test Cases
- Standard integers (e.g., "67890", "123456")
- Comma-separated numbers (e.g., "67,890", "123,456", "1,234,567")
- Numbers with leading/trailing spaces
- Invalid formats and edge cases
- Mixed format invoices
- Currency values with various formatting

---

## 12. Appendices

### A. Glossary
- **OCR**: Optical Character Recognition
- **API**: Application Programming Interface
- **MVP**: Minimum Viable Product
- **ML**: Machine Learning
- **Field Normalization**: Process of standardizing inconsistent field labels
- **Line Item Classification**: Process of categorizing invoice line items as Parts or Labor
- **Confidence Score**: Numerical score indicating system's certainty in classification
- **Comma-Separated Numbers**: Numeric format using commas as thousand separators (e.g., "1,234,567")

### B. References
- Azure Form Recognizer Documentation
- Azure SQL Database Best Practices
- ASP.NET Core API Guidelines
- Azure App Service Deployment Guide
- ML.NET Text Classification Documentation
- Regular Expression Patterns for Numeric Data

### C. Technical Implementation Notes
- **Regex Pattern for Comma-Separated Numbers**: `\d{1,3}(?:,\d{3})+`
- **Fallback Logic**: Always attempt standard integer parsing if comma-separated parsing fails
- **Audit Trail**: Maintain original extracted text alongside parsed values
- **Performance**: Numeric parsing should not add more than 100ms to total processing time
