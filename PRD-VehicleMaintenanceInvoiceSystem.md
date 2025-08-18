# Product Requirements Document (PRD)
## Vehicle Maintenance Invoice Processing System

### Document Information
- **Product Name**: Vehicle Maintenance Invoice Processing System
- **Version**: 1.1
- **Date**: August 18, 2025
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

### Success Metrics
- 95% accuracy in data extraction from supported invoice formats
- Processing time < 30 seconds per invoice
- 99.9% API uptime
- Support for 1000+ invoices per day
- Secure file access with audit logging

---

## 2. Product Overview

### Core Functionality
The system processes vehicle maintenance invoices through the following workflow:
1. **Upload**: Users upload PDF/PNG invoice files via web interface
2. **Extract**: OCR technology extracts structured data from documents
3. **Store**: Structured data saved to Azure SQL Database
4. **Archive**: Original files stored in Azure Blob Storage with secure access
5. **Review**: Users can view invoice details and approve or reject the processed invoice
6. **Approve/Reject**: Users can approve invoices for final acceptance or reject and delete them
7. **Access**: RESTful API provides data access for external systems
8. **View**: Users can view/download original invoice files through secure links

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
  - Automatically classify line items as Parts, Labor, or Mixed
  - Support varying table structures (separate vs combined line items)
  - Provide confidence scores for extracted data
  - Route low-confidence extractions for manual review

#### FR-003: Data Validation
- **Description**: Validate extracted data before database storage
- **Acceptance Criteria**:
  - Validate required fields are present
  - Check data type consistency
  - Flag incomplete extractions for manual review

#### FR-004: Data Mapping and Transformation
- **Description**: Transform extracted invoice data into normalized database structure
- **Acceptance Criteria**:
  - Map header fields to InvoiceHeader table columns
  - Map all line items to InvoiceLines table with proper categorization
  - Calculate and validate totals (parts vs labor vs total cost)
  - Assign sequential line numbers to detail items
  - Classify line items by type (Parts, Labor, Tax, Fees, Services)
  - Maintain referential integrity between header and line records
  - Handle missing or incomplete line item data gracefully

### 3.2 Data Storage Module

#### FR-005: Database Storage
- **Description**: Store structured invoice data in Azure SQL Database using normalized table structure
- **Acceptance Criteria**:
  - Map extracted header information to InvoiceHeader table
  - Map all detail lines (parts, labor, services) to InvoiceLines table
  - Maintain foreign key relationships between header and lines
  - Store processing metadata and confidence scores
  - Maintain data integrity with transactions
  - Record processing timestamps

#### FR-006: File Archival
- **Description**: Store original files in Azure Blob Storage
- **Acceptance Criteria**:
  - Store all files in a single container
  - Generate unique file identifiers
  - Maintain file metadata
  - Provide secure access URLs

#### FR-007: Original File Access
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

#### FR-008: RESTful API
- **Description**: Provide RESTful endpoints for data access
- **Acceptance Criteria**:
  - Return JSON responses
  - Support pagination for large result sets
  - Include proper HTTP status codes
  - Provide comprehensive error messages

### 3.4 User Interface Module

#### FR-009: Invoice Details View
- **Description**: Provide comprehensive invoice details page with original file access
- **Acceptance Criteria**:
  - Display all invoice header information in readable format
  - Show line items in organized table format
  - Include "View Original File" button/link prominently displayed
  - Support in-browser PDF viewing for compatible browsers
  - Support in-browser image viewing for PNG files
  - Provide "Download Original" option as fallback
  - Display file metadata (filename, size, upload date)
  - Show loading indicators during file access operations

#### FR-010: File Access Security
- **Description**: Implement secure access controls for original invoice files
- **Acceptance Criteria**:
  - Generate time-limited SAS (Shared Access Signature) URLs for blob access
  - Set 1-hour expiration on file access links
  - Log all file access attempts with user identification
  - Prevent direct blob URL exposure in client-side code
  - Handle expired link scenarios gracefully with user-friendly messages

#### FR-014: Enhanced Invoice Details UI
- **Description**: Update invoice details page to support approval workflow
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

#### FR-015: Confirmation Dialog System
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

#### FR-011: Invoice Approval Process
- **Description**: Provide approval workflow for processed invoices with database status tracking
- **Acceptance Criteria**:
  - Display "Approve" button on invoice details page for unapproved invoices
  - Update `Approved` column in database to `true` when invoice is approved
  - Show visual confirmation when invoice status changes to approved
  - Display approval status clearly on invoice details page
  - Provide confirmation popup before approving: "Are you sure you want to approve this invoice?"
  - Only show "Approve" button for invoices with `Approved = false`
  - Track approval timestamp and user information

#### FR-012: Invoice Rejection Process
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

#### FR-013: Approval Status Management
- **Description**: Manage invoice approval states and user interface updates
- **Acceptance Criteria**:
  - Add `Approved` boolean column to InvoiceHeader table with default value `false`
  - Display approval status badge on invoice details page ("Pending Approval" / "Approved")
  - Hide both "Approve" and "Reject" buttons for already approved invoices (`Approved = true`)
  - Show approval date and user information for approved invoices
  - Include approval status in API responses
  - Add approval status filter to invoice list views

### Base URL
```
https://[app-name].azurewebsites.net/api
```

### Endpoints

#### GET /invoices
- **Purpose**: Retrieve all invoices with pagination
- **Parameters**: 
  - `page` (optional): Page number (default: 1)
  - `pageSize` (optional): Items per page (default: 20, max: 100)
- **Response**: Paginated list of invoice headers

#### GET /invoices/{id}
- **Purpose**: Retrieve specific invoice with line items
- **Parameters**: `id` (required): Invoice ID
- **Response**: Complete invoice details including line items

#### GET /invoices/vehicle/{vehicleId}
- **Purpose**: Search invoices by vehicle ID
- **Parameters**: 
  - `vehicleId` (required): Vehicle identifier
  - `page` (optional): Page number
  - `pageSize` (optional): Items per page
- **Response**: Filtered invoice list

#### GET /invoices/date/{date}
- **Purpose**: Retrieve invoices by creation date
- **Parameters**: 
  - `date` (required): Date in YYYY-MM-DD format
  - `page` (optional): Page number
  - `pageSize` (optional): Items per page
- **Response**: Date-filtered invoice list

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
- **Content-Type**: Preserves original file MIME type (application/pdf or image/png)

---

## 5. Non-Functional Requirements

### Performance
- **Response Time**: API responses < 2 seconds
- **Processing Time**: Invoice processing < 30 seconds
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
- **CDN**: Azure CDN for static content delivery

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

### Table Structure Overview
The system uses a normalized two-table structure to store invoice data:

### InvoiceHeader Table
Stores one record per invoice with summary information extracted from invoice header.

| Column | Type | Constraints | Maps From |
|--------|------|-------------|-----------|
| InvoiceID | INT IDENTITY | PRIMARY KEY | Auto-generated |
| VehicleID | NVARCHAR(50) | NOT NULL | Vehicle identifier on invoice |
| Odometer | INT | NULL | Mileage reading from invoice |
| InvoiceNumber | NVARCHAR(50) | NOT NULL, UNIQUE | Invoice number from header |
| InvoiceDate | DATE | NOT NULL | Service/invoice date |
| TotalCost | DECIMAL(18,2) | NOT NULL | Grand total from invoice |
| TotalPartsCost | DECIMAL(18,2) | NOT NULL | Calculated sum of parts lines |
| TotalLaborCost | DECIMAL(18,2) | NOT NULL | Calculated sum of labor lines |
| BlobFileUrl | NVARCHAR(255) | NOT NULL | Azure Blob Storage URL (used for generating secure access links) |
| Approved | BIT | NOT NULL, DEFAULT 0 | Approval status (false=pending, true=approved) |
| ApprovedAt | DATETIME2 | NULL | Timestamp when invoice was approved |
| ApprovedBy | NVARCHAR(100) | NULL | User who approved the invoice |
| ExtractedData | NVARCHAR(MAX) | NULL | Raw JSON of extracted data |
| ConfidenceScore | DECIMAL(5,2) | NULL | Overall extraction confidence |
| CreatedAt | DATETIME | DEFAULT GETDATE() | Record creation time |

### InvoiceLines Table
Stores multiple records per invoice - one for each line item (parts, labor, fees, etc.).

| Column | Type | Constraints | Maps From |
|--------|------|-------------|-----------|
| LineID | INT IDENTITY | PRIMARY KEY | Auto-generated |
| InvoiceID | INT | FOREIGN KEY | Links to InvoiceHeader |
| LineNumber | INT | NOT NULL | Sequential order in invoice |
| Description | NVARCHAR(500) | NOT NULL | Item/service description |
| UnitCost | DECIMAL(18,2) | NOT NULL | Price per unit/hour |
| Quantity | DECIMAL(10,2) | NOT NULL | Number of units/hours |
| TotalLineCost | DECIMAL(18,2) | NOT NULL | Extended line total |
| PartNumber | NVARCHAR(100) | NULL | Optional: Extracted part number if available separately from description |
| Category | NVARCHAR(100) | NULL | Parts, Labor, Tax, Fee, Service |
| ConfidenceScore | DECIMAL(5,2) | NULL | Line extraction confidence |
| CreatedAt | DATETIME2 | DEFAULT GETUTCDATE() | Record creation time |

### Data Relationship
```
InvoiceHeader (1) -----> (Many) InvoiceLines
     │                        │
     │                        ├── Parts lines
     │                        ├── Labor lines  
     │                        ├── Tax lines
     │                        ├── Fee lines
     │                        └── Service lines
     │
     └── Summary totals calculated from line items
```

---

## 8. Technical Implementation Notes

### 8.1 Database Schema Changes
The approval feature requires adding new columns to the existing `InvoiceHeader` table:

```sql
-- Add approval columns to existing InvoiceHeader table
ALTER TABLE InvoiceHeader 
ADD Approved BIT NOT NULL DEFAULT 0,
    ApprovedAt DATETIME2 NULL,
    ApprovedBy NVARCHAR(100) NULL;

-- Create index for approval status queries
CREATE NONCLUSTERED INDEX IX_InvoiceHeader_Approved 
    ON InvoiceHeader (Approved);
```

### 8.2 API Implementation Notes
- **Approval Endpoint**: `PUT /api/invoices/{id}/approve` should be idempotent
- **Rejection Endpoint**: `DELETE /api/invoices/{id}/reject` must handle cascading deletions
- **Transaction Management**: Rejection process requires database transaction to ensure atomicity
- **Blob Deletion**: Must handle cases where blob file may already be deleted or inaccessible
- **Error Handling**: Provide detailed error responses for partial failures

### 8.3 Frontend Implementation Notes
- **JavaScript Confirmation Dialogs**: Use modern modal dialogs instead of basic `confirm()`
- **State Management**: Update UI state immediately after successful operations
- **Loading States**: Show loading indicators during async operations
- **Error Display**: Implement toast notifications or inline error messages
- **Accessibility**: Ensure confirmation dialogs are screen reader accessible

### 8.4 Security Considerations
- **Authorization**: Implement proper authorization checks for approval/rejection actions
- **Audit Logging**: Log all approval/rejection actions with user identification
- **Input Validation**: Validate invoice ID and approval state before processing
- **Rate Limiting**: Consider implementing rate limits on approval/rejection endpoints

---

## 9. Multi-Vendor Invoice Format Strategy

### Supported Invoice Variations

#### Format Type 1: Separate Parts and Labor Sections
- Parts listed in one table section
- Labor listed in separate table section
- Each section may have different column structures

#### Format Type 2: Combined Line Items with Type Classification
- Single table with mixed parts and labor
- Type identified by description or category column
- Unified column structure across line items

#### Format Type 3: Service-Based Invoices
- Labor and parts combined per service line
- Each line represents a complete service (e.g., "Oil Change - includes filter and oil")
- May require parsing descriptions to separate parts from labor costs

### Azure Form Recognizer Implementation Strategy

#### Phase 1: General Invoice Model
- Use pre-built invoice model for basic structure detection
- Extract common fields (totals, dates, vendor info)
- Identify table regions for line item processing

#### Phase 2: Custom Models by Vendor
- Train vendor-specific models for improved accuracy
- Handle unique layouts and terminology
- Optimize for each vendor's specific format quirks

#### Phase 3: Intelligent Classification
- Automatically detect invoice format type
- Route to appropriate extraction logic
- Fall back to manual review for unknown formats

### Data Mapping Strategy

#### Invoice Header Extraction → InvoiceHeader Table
| Extracted Field | Database Column | Description |
|-----------------|-----------------|-------------|
| Vehicle ID | VehicleID | Vehicle identifier from invoice |
| Odometer Reading | Odometer | Mileage at time of service |
| Invoice Number | InvoiceNumber | Unique invoice identifier |
| Invoice Date | InvoiceDate | Date of service/invoice |
| Total Amount | TotalCost | Grand total of invoice |
| Parts Subtotal | TotalPartsCost | Sum of all parts costs |
| Labor Subtotal | TotalLaborCost | Sum of all labor costs |
| Original File | BlobFileUrl | URL to stored PDF/PNG |

#### Invoice Line Items → InvoiceLines Table
| Extracted Field | Database Column | Description |
|-----------------|-----------------|-------------|
| Line Description | Description | Part name, service description, etc. |
| Unit Price | UnitCost | Price per unit/hour |
| Quantity | Quantity | Number of parts or hours |
| Line Total | TotalLineCost | Extended cost for this line |
| Item Type | Category | Parts, Labor, Tax, Misc, etc. |
| Line Number | LineNumber | Sequential order in invoice |

#### Data Processing Flow
```
PDF/PNG Invoice
        ↓
Azure Form Recognizer Extraction
        ↓
┌─────────────────┬─────────────────┐
│   Header Data   │   Line Items    │
│                 │                 │
│ • Vehicle ID    │ • Description   │
│ • Invoice #     │ • Unit Cost     │
│ • Date          │ • Quantity      │
│ • Totals        │ • Line Total    │
│                 │ • Category      │
└─────────────────┴─────────────────┘
        ↓                 ↓
  InvoiceHeader      InvoiceLines
     Table             Table
        ↓                 ↓
    One Record    Multiple Records
                  (linked by InvoiceID)
```

#### Classification Rules
1. **Parts Detection**: Keywords like "part", "filter", "oil", part numbers → Category = "Parts"
2. **Labor Detection**: Keywords like "labor", "service", "diagnostic", hourly rates → Category = "Labor"
3. **Tax/Fees**: Keywords like "tax", "fee", "disposal" → Category = "Tax"
4. **Mixed Items**: Combined descriptions requiring parsing → Category = "Service"
5. **Confidence Scoring**: Track extraction confidence per line item

#### Part Number Handling Strategy
Most vehicle maintenance invoices handle part numbers inconsistently:

**Scenario 1: Part Number in Separate Column**
```
Description          | Part Number | Price
Oil Filter          | AC-PF52     | $24.99
```
→ PartNumber field populated with "AC-PF52"

**Scenario 2: Part Number in Description (Most Common)**
```
Description                    | Price
Oil Filter - AC Delco PF52    | $24.99
```
→ PartNumber field remains NULL, full description stored in Description field

**Scenario 3: No Part Number Available**
```
Description        | Price
Standard Oil Filter| $24.99
```
→ PartNumber field remains NULL

**Implementation Approach:**
- PartNumber field is **optional** and may be NULL for most records
- If Form Recognizer detects a separate part number column, populate PartNumber field
- If no separate column exists, store complete description (including any embedded part numbers)
- Do not attempt to parse part numbers from descriptions (too error-prone)

#### Example Data Mapping

**Sample Invoice Input:**
```
Vehicle: VEH-12345        Invoice: INV-2025-001
Date: 2025-08-14         Total: $285.50

Line Items:
1. Oil Filter - AC Delco PF52    $24.99  x1  = $24.99
2. 5W-30 Motor Oil (5 Quarts)    $34.99  x1  = $34.99
3. Labor - Oil Change Service    $75.00  x2  = $150.00
4. Shop Supplies                 $12.50  x1  = $12.50
5. Environmental Disposal Fee    $8.00   x1  = $8.00
6. Sales Tax                     $25.02  x1  = $25.02
```

**Database Output:**

*InvoiceHeader Table:*
```
InvoiceID: 1001
VehicleID: VEH-12345
InvoiceNumber: INV-2025-001
InvoiceDate: 2025-08-14
TotalCost: 285.50
TotalPartsCost: 59.98
TotalLaborCost: 150.00
```

*InvoiceLines Table:*
```
LineID: 1, InvoiceID: 1001, LineNumber: 1, Description: "Oil Filter - AC Delco PF52", 
UnitCost: 24.99, Quantity: 1, TotalLineCost: 24.99, Category: "Parts", PartNumber: NULL

LineID: 2, InvoiceID: 1001, LineNumber: 2, Description: "5W-30 Motor Oil (5 Quarts)", 
UnitCost: 34.99, Quantity: 1, TotalLineCost: 34.99, Category: "Parts", PartNumber: NULL

LineID: 3, InvoiceID: 1001, LineNumber: 3, Description: "Labor - Oil Change Service", 
UnitCost: 75.00, Quantity: 2, TotalLineCost: 150.00, Category: "Labor", PartNumber: NULL

LineID: 4, InvoiceID: 1001, LineNumber: 4, Description: "Shop Supplies", 
UnitCost: 12.50, Quantity: 1, TotalLineCost: 12.50, Category: "Supplies", PartNumber: NULL

LineID: 5, InvoiceID: 1001, LineNumber: 5, Description: "Environmental Disposal Fee", 
UnitCost: 8.00, Quantity: 1, TotalLineCost: 8.00, Category: "Fee", PartNumber: NULL

LineID: 6, InvoiceID: 1001, LineNumber: 6, Description: "Sales Tax", 
UnitCost: 25.02, Quantity: 1, TotalLineCost: 25.02, Category: "Tax", PartNumber: NULL
```

**Alternative Scenario - Invoice with Separate Part Number Column:**
```
Description     | Part Number | Price | Qty | Total
Oil Filter      | AC-PF52     | 24.99 | 1   | 24.99
Motor Oil       | VAL-120     | 34.99 | 1   | 34.99
```

*Would result in:*
```
Description: "Oil Filter", PartNumber: "AC-PF52", Category: "Parts"
Description: "Motor Oil", PartNumber: "VAL-120", Category: "Parts"
```

---

## 10. Development Timeline (20 Working Days)

### Sprint 1: Foundation (Days 1-5)
**Objectives**: Infrastructure setup and basic file handling
- Azure resource provisioning
- Database schema implementation (including approval columns)
- Basic web application structure
- File upload functionality
- Blob storage integration

### Sprint 2: Core Processing (Days 6-10)
**Objectives**: OCR integration and data extraction
- Azure Form Recognizer integration for multi-vendor support
- Data extraction pipeline with format detection
- Vendor-specific extraction logic
- Database data access layer with approval status
- Error handling and logging
- Basic API endpoints

### Sprint 3: API Development (Days 11-15)
**Objectives**: Complete API implementation including approval workflow
- All REST endpoints implementation (including approve/reject)
- Approval/rejection business logic with cascading deletions
- API documentation (Swagger)
- Unit testing framework
- Integration testing
- Performance optimization

### Sprint 4: UI Enhancement (Days 16-18)
**Objectives**: Approval workflow user interface
- Invoice details page updates with approval status
- Approve/Reject buttons with confirmation dialogs
- Success/error message handling
- Responsive design updates
- Frontend validation and state management

### Sprint 5: Testing & Deployment (Days 19-20)
**Objectives**: Production readiness
- End-to-end testing including approval workflow
- Load testing
- Security testing
- Production deployment
- Documentation completion

---

## 11. Risk Assessment

### High Priority Risks
1. **Vendor Format Variability**: Different vendors may have significantly different invoice layouts
   - *Mitigation*: Implement Azure Form Recognizer custom models per vendor and confidence-based manual review

2. **OCR Accuracy**: Variable invoice formats may impact extraction accuracy
   - *Mitigation*: Use Azure Form Recognizer's adaptive learning and implement manual review workflow

3. **Azure Service Limits**: Form Recognizer API rate limits and costs
   - *Mitigation*: Implement queuing system, rate limiting, and cost monitoring

3. **Data Privacy**: Sensitive financial information handling
   - *Mitigation*: Implement encryption at rest and in transit

### Medium Priority Risks
1. **Performance**: Large file processing times
   - *Mitigation*: Implement async processing and progress tracking

2. **Integration**: External system compatibility
   - *Mitigation*: Comprehensive API documentation and testing

---

## 11. Success Criteria

### Phase 1 (MVP)
- [ ] Process PDF and PNG invoices successfully
- [ ] Extract header and line item data with 90%+ accuracy
- [ ] Handle at least 3 different vendor invoice formats
- [ ] Classify line items as Parts, Labor, or Mixed
- [ ] Store data in Azure SQL Database
- [ ] Provide basic API endpoints
- [ ] Deploy to Azure App Service
- [ ] Implement secure original file access (view/download functionality)
- [ ] Display "View Original File" buttons on invoice details pages
- [ ] **NEW: Implement invoice approval/rejection workflow**
- [ ] **NEW: Add approval status tracking with database column**
- [ ] **NEW: Provide confirmation dialogs for all approval/rejection actions**
- [ ] **NEW: Implement complete invoice deletion on rejection (blob + database)**

### Phase 2 (Enhancement)
- [ ] Implement vendor-specific custom models
- [ ] Add batch processing capabilities
- [ ] Enhance format detection and classification
- [ ] Add comprehensive monitoring and alerts
- [ ] Implement audit logging for approval/rejection actions
- [ ] Support additional vendor formats
- [ ] **NEW: Add bulk approval/rejection capabilities**
- [ ] **NEW: Implement approval workflow with multiple approval levels**
- [ ] **NEW: Add approval history and audit trail**

---

## 12. Appendices

### A. Glossary
- **OCR**: Optical Character Recognition
- **API**: Application Programming Interface
- **MVP**: Minimum Viable Product
- **SLA**: Service Level Agreement

### B. References
- Azure Form Recognizer Documentation
- Azure SQL Database Best Practices
- ASP.NET Core API Guidelines
- Azure App Service Deployment Guide
