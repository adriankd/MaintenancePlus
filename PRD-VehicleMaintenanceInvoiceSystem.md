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
- **Standardization**: Generate consistent, professional maintenance summary descriptions
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
5. **Summarize**: Generate standardized maintenance description summary of all line items using AI
6. **Store**: Structured data saved to Azure SQL Database with normalized labels, classifications, and description summary
7. **Archive**: Original files stored in Azure Blob Storage with secure access
8. **Review**: Users can view invoice details and approve or reject the processed invoice
9. **Feedback**: Users can correct misclassifications to improve future accuracy
10. **Approve/Reject**: Users can approve invoices for final acceptance or reject and delete them
11. **Access**: RESTful API provides data access for external systems
12. **View**: Users can view/download original invoice files through secure links

### Target Users
- **Primary**: Fleet managers and maintenance administrators
- **Secondary**: External systems requiring invoice data integration
- **Tertiary**: Auditors requiring document access and original file review

---

## 3. User Stories

### Fleet Manager User Stories
- **US-001**: As a fleet manager, I want to upload vehicle maintenance invoices so that I can digitize our paper-based records
- **US-002**: As a fleet manager, I want the system to automatically extract key information (vehicle ID, odometer, invoice number, date, total cost) so that I don't have to manually enter this data
- **US-003**: As a fleet manager, I want to see a standardized maintenance summary description so that I can quickly understand what services were performed without reading all the detailed line items
- **US-004**: As a fleet manager, I want line items automatically classified as Parts, Labor, or Fees so that I can track maintenance costs by category
- **US-005**: As a fleet manager, I want to approve or reject processed invoices so that I can ensure data accuracy before final storage

### Maintenance Technician User Stories  
- **US-006**: As a maintenance technician, I want to access invoice data through an API so that I can integrate it with our existing maintenance management system
- **US-007**: As a maintenance technician, I want to view the original invoice files so that I can verify details when needed
- **US-008**: As a maintenance technician, I want to see confidence scores for extracted data so that I know which information might need manual verification

### System Administrator User Stories
- **US-009**: As a system administrator, I want to manage field label mappings so that I can help the system better recognize varying invoice formats from different vendors
- **US-010**: As a system administrator, I want to monitor AI processing costs and usage so that I can stay within budget limits
- **US-011**: As a system administrator, I want the system to gracefully handle AI service outages so that invoice processing can continue with fallback methods

### Accountant User Stories
- **US-012**: As an accountant, I want to see detailed breakdowns of parts vs labor costs so that I can properly categorize expenses for financial reporting
- **US-013**: As an accountant, I want access to original invoice files with audit trails so that I can verify transactions during financial audits
- **US-014**: As an accountant, I want consistent, standardized description summaries so that I can easily identify similar types of maintenance work across different invoices

---

## 4. Functional Requirements

### 4.1 File Processing Module

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
- **Description**: Extract part numbers from parts-only invoice line items using table-first approach with intelligent fallback methods for comprehensive part number capture
- **Acceptance Criteria**:
  - **Line Item Type Filtering**:
    - Only attempt part number extraction for line items classified as "Parts" 
    - Do not extract part numbers from Labor, Tax, Fee, or Service line items
    - Apply part number extraction after line item classification is complete
  - **Extraction Methodology (Table-First with Fallbacks)**:
    - **Primary Method**: Extract from dedicated part number columns in invoice table structures
      - Smart column detection for various naming conventions ("Part Number", "Part No", "PN", "P/N", "Part#", "Item Number")
      - Use case-insensitive header matching with keyword detection
      - Match part numbers to line items using description correlation
    - **Fallback Methods** (when table columns unavailable or insufficient):
      - Description-based matching using fuzzy string comparison algorithms
      - Regex pattern matching for common part number formats (alphanumeric with hyphens, manufacturer codes)
      - Format validation using `IsLikelyPartNumber` heuristics
    - **Intelligent Processing**:
      - Prioritize table-extracted part numbers over description-parsed ones
      - Cross-validate part numbers against line item descriptions for accuracy
      - Handle multiple table structures and vendor-specific invoice formats
  - **Data Processing Requirements**:
    - Store extracted part numbers in the `PartNumber` column of `InvoiceLines` table
    - Preserve original format and casing of part numbers exactly as found
    - Handle missing part number data gracefully (store as NULL)
    - Store empty/blank part number fields as NULL (not empty string)
    - Log extraction method used (table column, description parsing, regex) for audit purposes
  - **Quality and Validation**:
    - Achieve 95% accuracy in part number extraction from dedicated columns when present
    - Achieve 85% accuracy in part number extraction using fallback methods
    - **Format Validation**: Use `IsLikelyPartNumber` heuristics to validate extracted part numbers
      - Verify alphanumeric patterns with manufacturer-specific formatting (e.g., Honda: 17220-5AA-A00, Ford: BC3Z-7G391-A)
      - Check for common part number characteristics (length, hyphen placement, character patterns)
      - Reject obvious false positives (generic text, pure numbers without formatting)
    - **Extraction Path Logging**: Log the specific extraction method used for audit and debugging
      - Record source: "table-column", "description-parsing", or "regex-fallback"
      - Log source column name when extracted from table structures
      - Track confidence scores for each extraction method
      - Enable traceability for quality improvement and troubleshooting
    - Flag invoices with low extraction confidence for manual review
    - Maintain comprehensive audit trail showing extraction method and source column/field used

#### FR-006: Data Mapping and Transformation
- **Description**: Transform extracted invoice data into normalized database structure
- **Acceptance Criteria**:
  - Map header fields to InvoiceHeader table columns
  - Map all line items to InvoiceLines table with proper categorization
  - **Part Number Extraction Policy (Table-First with Fallbacks)**:
    - **Primary**: Extract part numbers from clearly labeled part number table columns when available
    - **Fallback**: Allow description-based extraction when no dedicated column exists, provided:
      - Strict validation using `IsLikelyPartNumber` confidence thresholds (minimum 85% confidence)
      - Complete provenance recording (source method, confidence score, extraction path)
      - Automatic flagging for manual review when confidence falls below threshold
      - Logging and metrics collection for all fallback extractions
    - **Restrictions**: Populate `PartNumber` field only for line items classified as "Parts"
    - **Null Handling**: Set `PartNumber` to NULL when no dedicated column exists for non-Parts items or when extraction confidence is insufficient
  - Calculate and validate totals (parts vs labor vs total cost)
  - Assign sequential line numbers to detail items
  - Classify line items by type (Parts, Labor, Tax, Fees, Services)
  - Maintain referential integrity between header and line records
  - Handle missing or incomplete line item data gracefully
  - **Store part numbers in the PartNumber column only for Parts line items**
  - **Store NULL for PartNumber when no dedicated part number column exists or item is not a Part**

#### FR-007: Integrated Line Item Classification via GPT-4o with Intelligent Fallback
- **Description**: Perform line item classification as part of the comprehensive GPT-4o processing, with database-driven fallback when rate limits are encountered
- **Acceptance Criteria**:
  - **GPT-4o Integrated Classification**: Line item classification happens within the single comprehensive GPT-4o call:
    - Classify each line item as Part, Labor, Fee, Tax, or Other
    - Include classification confidence scores in JSON output
    - Consider entire invoice context when making classification decisions
    - Extract part numbers for items classified as "Part"
  - **Fallback Rule-Based Classification**: When GPT-4o is unavailable, use database-driven keywords:
    - Apply classification using keywords stored in `PartLaborKeywords` table
    - Support case-insensitive matching with configurable match types (exact, partial, contains)
    - Maintain keyword effectiveness tracking with usage statistics
    - Enable administrative management of keyword database
  - **Processing Workflow**:
    - **Primary**: GPT-4o processes all line items in single call with full context
    - **Fallback**: Apply rule-based classification using database keywords when rate limited
    - **Consistency**: Ensure same classification categories used in both approaches
    - **Audit Trail**: Log which processing method was used (GPT-4o vs fallback)
  - **Quality and Performance Targets**:
    - Achieve 95% accuracy with GPT-4o comprehensive processing
    - Maintain 80% accuracy with fallback rule-based classification
    - Process all line items consistently within single approach per invoice
    - Store classification method and confidence scores for quality monitoring

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

#### FR-009: Comprehensive GPT-4o Invoice Processing with Intelligent Fallback
- **Description**: Use GPT-4o for comprehensive invoice processing in a single API call, with database-driven fallback when rate limits are reached
- **Acceptance Criteria**:
  - **Single-Call GPT-4o Processing**: One comprehensive GPT-4o call per invoice upload:
    - Input: Raw Form Recognizer JSON output
    - Output: Structured JSON matching database schema with normalized fields and classified line items
    - Process: Field mapping, normalization, and line item classification in single request
    - Timeout: 30 seconds maximum with proper error handling
  - **GPT-4o Output Schema**: Structured JSON response containing:
    - **Header Fields**: VehicleID, InvoiceNumber, InvoiceDate, Odometer, TotalCost, etc.
    - **Description Summary**: Comprehensive summary of all line item descriptions and services performed
    - **Line Items Array**: Each with Description, UnitCost, Quantity, TotalLineCost, PartNumber, Classification (Part/Labor/Fee)
    - **Confidence Scores**: Overall confidence and per-field confidence ratings
    - **Processing Notes**: Any ambiguities or assumptions made during processing
  - **Rate Limit Handling**: Graceful fallback when GPT-4o is unavailable:
    - Detect rate limit responses (429 status codes)
    - Automatically switch to intelligent fallback processing
    - Log rate limit encounters for monitoring
    - Continue processing without user-visible errors
  - **Database-Driven Fallback System**:
    - **Field Mapping**: Use `InvoiceFields` table for field label normalization
    - **Classification**: Use `PartLaborKeywords` table for rule-based line item classification
    - **Expandable Keywords**: Support administrative addition of new field mappings and keywords
    - **Learning Capability**: Optionally store successful GPT-4o patterns for future fallback improvement
  - **Description Summary Generation**: Generate standardized summary using maintenance-friendly terminology:
    - **Input**: All line item descriptions from the invoice
    - **Output**: Standardized, professional summary using common maintenance terminology
    - **Style**: Invoice-friendly labels rather than verbatim line item descriptions
    - **Examples**: "Oil Change Service and Inspection", "Brake System Repair", "Routine Maintenance", "Engine Diagnostics and Parts Replacement"
    - **Length**: Concise phrase or single sentence using industry-standard terminology
    - **Standardization**: Transform detailed line items into common maintenance categories:
      - Oil/fluid services → "Oil Change Service" or "Fluid Service"
      - Multiple brake items → "Brake System Service" or "Brake Repair"
      - Diagnostic work → "Engine Diagnostics" or "System Diagnostics"
      - Mixed services → "Routine Maintenance" or "General Service"
    - **Integration**: Include summary generation in the single comprehensive GPT-4o call

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

#### FR-011: Single-Call Comprehensive Processing Pipeline
- **Description**: Implement streamlined processing pipeline with single GPT-4o call for complete invoice enhancement
- **Acceptance Criteria**:
  - **Unified Processing Workflow**: Execute processing in optimal sequence:
    - Step 1: Form Recognizer extracts raw OCR data
    - Step 2: Single comprehensive GPT-4o call transforms raw data into structured database-ready JSON
    - Step 3: Direct database storage of structured output
    - Step 4: Fallback processing only when GPT-4o fails or rate limited
  - **GPT-4o Integration**: Single API call handles all intelligence processing:
    - Field mapping and normalization
    - Line item classification and part number extraction
    - Data validation and error correction
    - Confidence scoring and quality assessment
  - **Fallback Architecture**: Seamless degradation when GPT-4o unavailable:
    - Database-driven field mapping using `InvoiceFields` table
    - Database-driven classification using `PartLaborKeywords` table
    - Maintain processing pipeline consistency regardless of method used
  - **Processing Metadata**: Track and store processing information:
    - Method used (GPT-4o-enhanced vs fallback)
    - Processing timestamps and performance metrics
    - Confidence scores and quality indicators
    - Error handling and retry logic results

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

### 3.6 AI-Powered Enhancement Module (GPT-4o Integration)

#### FR-024: GPT-4o Integration Infrastructure
- **Description**: Integrate OpenAI's GPT-4o model via GitHub Models API for advanced invoice processing capabilities
- **Acceptance Criteria**:
  - Implement secure API integration with GitHub Models endpoint (https://models.inference.ai.azure.com)
  - Configure GitHub Personal Access Token authentication
  - Support rate limiting and error handling for API calls
  - Implement proper request/response logging for debugging
  - Provide fallback mechanisms when AI service is unavailable
  - Maintain cost monitoring and usage tracking
  - Support timeout configuration (default 30 seconds per request)

#### FR-025: Intelligent Part Number Extraction
- **Description**: Enhance part number extraction using GPT-4o's automotive expertise and pattern recognition
- **Acceptance Criteria**:
  - **Advanced Pattern Recognition**: Utilize GPT-4o to identify part numbers using:
    - OEM-specific formats (Honda: 12345-ABC-123, Toyota: 90210-54321, Ford: F1XZ-1234-AB)
    - Cross-reference numbers and alternative part numbers
    - Context-aware validation using surrounding invoice text
  - **Multi-vendor Support**: Handle manufacturer-specific part number conventions
  - **Confidence Scoring**: Provide AI-generated confidence scores for each extracted part number
  - **Fallback Integration**: Use GPT-4o as enhancement to existing extraction, not replacement
  - **Quality Assurance**: Achieve 95% accuracy on part number extraction when combined with traditional methods
  - **Audit Trail**: Log AI extraction results alongside traditional extraction for comparison

#### FR-026: Comprehensive Invoice Enhancement
- **Description**: Use GPT-4o to enhance and validate entire invoice processing workflow
- **Acceptance Criteria**:
  - **Service Classification**: Automatically categorize line items into detailed service types:
    - Oil Change, Brake Service, Engine Repair, Transmission Work, Electrical Repair, Tire Service, etc.
  - **Data Validation**: Identify and flag potential OCR errors in:
    - Inconsistent totals and missing quantities
    - Unclear or suspicious part descriptions
    - Number formatting issues and calculation discrepancies
  - **Data Standardization**: Normalize and clean extracted data:
    - Standardize part descriptions (remove extra spaces, fix common OCR errors)
    - Normalize units of measure and quantities
    - Standardize service categories using consistent naming
  - **Quality Enhancement**: Improve overall data quality by:
    - Suggesting corrections for likely OCR errors
    - Providing alternative interpretations for ambiguous text
    - Cross-validating extracted data against automotive knowledge base

#### FR-027: AI-Powered Validation and Error Correction
- **Description**: Implement intelligent validation using GPT-4o's reasoning capabilities
- **Acceptance Criteria**:
  - **Anomaly Detection**: Identify unusual patterns that may indicate extraction errors:
    - Parts costs that seem unusually high or low
    - Service descriptions that don't match common automotive terminology
    - Inconsistent vehicle information across invoice sections
  - **Smart Suggestions**: Provide AI-powered suggestions for:
    - Likely intended values for unclear OCR extractions
    - Standard part description corrections
    - Missing information that can be inferred from context
  - **Confidence-Based Processing**: Route invoices for manual review based on AI confidence scores
  - **Contextual Validation**: Use automotive domain knowledge to validate:
    - Part-service relationships (ensure parts match typical service procedures)
    - Reasonable cost ranges for different types of services
    - Logical consistency across the entire invoice

#### FR-028: LLM Testing and Monitoring Endpoints
- **Description**: Provide dedicated endpoints for testing and monitoring GPT-4o integration
- **Acceptance Criteria**:
  - **Connection Testing**: `/api/llm/test-connection` endpoint to verify GitHub Models API connectivity
  - **Part Extraction Testing**: `/api/llm/extract-parts` endpoint for testing part number extraction on text samples
  - **Invoice Enhancement Testing**: `/api/llm/enhance-invoice` endpoint for testing complete invoice enhancement
  - **Performance Monitoring**: Track response times, success rates, and error patterns
  - **Cost Monitoring**: Monitor API usage and associated costs
  - **Quality Metrics**: Compare AI-enhanced results with traditional extraction methods

#### FR-029: Hybrid Processing Pipeline
- **Description**: Integrate GPT-4o enhancement into existing invoice processing workflow
- **Acceptance Criteria**:
  - **Seamless Integration**: AI enhancement occurs transparently within existing pipeline
  - **Configurable Enhancement**: Allow enabling/disabling of AI features via configuration
  - **Performance Optimization**: Ensure AI enhancement doesn't significantly impact processing time
  - **Fallback Support**: Graceful degradation when AI services are unavailable
  - **Result Comparison**: Store both traditional and AI-enhanced results for quality analysis
  - **Progressive Enhancement**: Use AI to improve results rather than replace existing functionality

#### FR-030: AI Enhancement Configuration and Management
- **Description**: Provide administrative controls for managing AI-powered features
- **Acceptance Criteria**:
  - **Feature Toggles**: Enable/disable specific AI features independently
  - **Threshold Configuration**: Set confidence thresholds for different AI operations
  - **Cost Controls**: Set usage limits and alerts for API consumption
  - **Model Version Management**: Support updating to newer GPT-4o model versions
  - **Performance Tuning**: Adjust timeout, retry, and rate limiting parameters
  - **Quality Monitoring**: Track and report AI enhancement effectiveness

---

## 5. API Specification

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

### Comprehensive GPT-4o Processing Endpoints

#### POST /llm/process-invoice
- **Purpose**: Single comprehensive call to process invoice using GPT-4o with structured output
- **Body**: JSON with Form Recognizer raw output and processing options
- **Request Example**:
  ```json
  {
    "formRecognizerData": "{\"vendor\": \"Honda Service Center\", \"lines\": [...]}",
    "options": {
      "includeConfidenceScores": true,
      "validateTotals": true,
      "extractPartNumbers": true,
      "classifyLineItems": true
    }
  }
  ```
- **Response**: Complete structured invoice data ready for database storage
- **Example Response**:
  ```json
  {
    "success": true,
    "processingMethod": "GPT4o-Enhanced",
    "invoice": {
      "header": {
        "vehicleId": "VEH001",
        "invoiceNumber": "HSC-2025-142",
        "invoiceDate": "2025-08-22",
        "odometer": 67890,
        "totalCost": 284.50,
        "description": "Oil Change Service and General Inspection"
      },
      "lineItems": [
        {
          "lineNumber": 1,
          "description": "Oil Change Service",
          "classification": "Labor",
          "unitCost": 45.00,
          "quantity": 1,
          "totalCost": 45.00,
          "confidence": 0.95
        }
      ],
      "overallConfidence": 0.92,
      "processingNotes": ["All fields mapped successfully", "2 part numbers extracted"]
    }
  }
  ```

#### POST /llm/test-connection
- **Purpose**: Test GPT-4o connectivity and rate limit status
- **Response**: Connection status and current rate limit information

#### POST /llm/test-connection
- **Purpose**: Test GPT-4o connectivity and API functionality
- **Authentication**: GitHub Personal Access Token via configuration
- **Response**: Connection status, model availability, and capability confirmation
- **Example Response**:
  ```json
  {
    "status": "success",
    "response": {
      "status": "connected",
      "model": "gpt-4o",
      "timestamp": "2025-08-21T10:30:00Z",
      "capabilities": ["text_processing", "json_output", "automotive_expertise"]
    },
    "message": "GPT-4o connection successful via GitHub Models"
  }
  ```

#### POST /llm/extract-parts
- **Purpose**: Extract automotive part numbers from text using GPT-4o advanced pattern recognition
- **Body**: JSON with invoice text and optional brand preference
- **Request Example**:
  ```json
  {
    "invoiceText": "1 EA ENGINE OIL FILTER 15400-PLM-A02 Honda OEM\n2 QT MOTOR OIL 5W-30 Mobile 1",
    "preferredBrand": "Honda"
  }
  ```
- **Response**: Structured part number extraction with confidence scores
- **Example Response**:
  ```json
  {
    "status": "success",
    "result": {
      "part_numbers": [
        {
          "number": "15400-PLM-A02",
          "line_text": "1 EA ENGINE OIL FILTER 15400-PLM-A02 Honda OEM",
          "confidence": 0.98,
          "type": "OEM",
          "brand": "Honda"
        }
      ]
    },
    "processed_with": "GPT-4o part number extraction"
  }
  ```

#### POST /llm/enhance-invoice
- **Purpose**: Comprehensive invoice enhancement using GPT-4o intelligence
- **Body**: JSON with raw invoice data from Form Recognizer
- **Request Example**:
  ```json
  {
    "invoiceData": "{\"vendor\": \"Honda Service Center\", \"lines\": [...]}",
    "includePartNumbers": true,
    "includeClassification": true,
    "validateData": true
  }
  ```
- **Response**: Enhanced invoice data with AI improvements
- **Example Response**:
  ```json
  {
    "status": "success",
    "enhanced_data": {
      "validated_totals": {...},
      "classified_services": {...},
      "extracted_parts": {...},
      "corrected_ocr_errors": {...}
    },
    "confidence_score": 0.92,
    "improvements": [
      "Part numbers extracted and classified",
      "Service categories standardized",
      "3 OCR errors corrected",
      "Total calculations validated"
    ],
    "processed_with": "GPT-4o via GitHub Models"
  }
  ```

---

## 6. Non-Functional Requirements

### Performance
- **Response Time**: API responses < 2 seconds
- **Processing Time**: Invoice processing < 30 seconds (including classification and normalization)
- **Classification Time**: Line item classification < 5 seconds per invoice
- **LLM Enhancement Time**: GPT-4o processing < 15 seconds per invoice
- **LLM Response Time**: Individual GPT-4o API calls < 30 seconds with timeout handling
- **Throughput**: Support 100 concurrent users
- **File Processing**: Handle up to 1000 invoices/day
- **AI Service Reliability**: Fallback to traditional processing when LLM services unavailable

### Security
- **Authentication**: Future implementation (Phase 2)
- **Data Encryption**: HTTPS for all communications
- **Access Control**: Azure AD integration capability
- **Audit Logging**: Track all data access and modifications
- **File Access Logging**: Log original file view/download activities
- **LLM Security**: 
  - Secure GitHub Personal Access Token storage and rotation
  - Invoice data sanitization before sending to external AI services
  - No storage of invoice data on external AI service providers
  - Rate limiting and usage monitoring for AI API calls
  - Secure transmission of data to GitHub Models API endpoint

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

## 7. Technical Architecture

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

## 8. Database Schema

**Note**: All timestamp columns use DATETIME2 with GETUTCDATE() defaults to ensure consistent UTC time storage across the system.

### InvoiceHeader Table
Stores one record per invoice with summary information extracted from invoice header.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| InvoiceID | INT IDENTITY | PRIMARY KEY | Auto-generated |
| VehicleID | NVARCHAR(50) | NOT NULL | Vehicle identifier on invoice (normalized) |
| OriginalVehicleLabel | NVARCHAR(100) | NULL | Original extracted vehicle field label |
| Odometer | INT | NULL | Mileage reading from invoice (supports comma-separated format) |
| OriginalOdometerLabel | NVARCHAR(100) | NULL | Original extracted odometer field label |
| InvoiceNumber | NVARCHAR(50) | NOT NULL, UNIQUE | Invoice number from header (normalized) |
| OriginalInvoiceLabel | NVARCHAR(100) | NULL | Original extracted invoice field label |
| InvoiceDate | DATE | NOT NULL | Service/invoice date |
| TotalCost | DECIMAL(18,2) | NOT NULL | Grand total from invoice |
| TotalPartsCost | DECIMAL(18,2) | NOT NULL | Calculated sum of parts lines |
| TotalLaborCost | DECIMAL(18,2) | NOT NULL | Calculated sum of labor lines |
| Description | NVARCHAR(MAX) | NULL | GPT-4o generated summary of all line item descriptions |
| BlobFileUrl | NVARCHAR(255) | NOT NULL | Azure Blob Storage URL |
| Approved | BIT | NOT NULL, DEFAULT 0 | Approval status |
| ApprovedAt | DATETIME2 | NULL | Timestamp when invoice was approved (UTC) |
| ApprovedBy | NVARCHAR(100) | NULL | User who approved the invoice |
| ExtractedData | NVARCHAR(MAX) | NULL | Raw JSON of extracted data |
| ConfidenceScore | DECIMAL(5,2) | NULL | Overall extraction confidence |
| NormalizationVersion | NVARCHAR(20) | NULL | Version of normalization rules applied |
| CreatedAt | DATETIME2 | DEFAULT GETUTCDATE() | Record creation time (UTC) |

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
| ClassificationMethod | NVARCHAR(50) | NOT NULL | GPT4o-Enhanced, Fallback-Rules, Manual |
| ClassificationVersion | NVARCHAR(20) | NULL | Version of classification model used |
| OriginalCategory | NVARCHAR(100) | NULL | Original extracted category before classification |
| ExtractionConfidence | DECIMAL(5,2) | NULL | Line extraction confidence from OCR |
| CreatedAt | DATETIME2 | DEFAULT GETUTCDATE() | Record creation time (UTC) |

### InvoiceFields Table (Fallback Support)
Stores field label mappings for fallback processing when GPT-4o is unavailable.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| FieldMappingID | INT IDENTITY | PRIMARY KEY | Auto-generated |
| TargetFieldName | NVARCHAR(50) | NOT NULL | Standard field name (VehicleID, InvoiceNumber, Odometer) |
| ExpectedValue | NVARCHAR(100) | NOT NULL | Possible field label variation |
| MatchType | NVARCHAR(20) | NOT NULL | EXACT, PARTIAL, CONTAINS |
| IsActive | BIT | NOT NULL, DEFAULT 1 | Enable/disable mapping |
| UsageCount | INT | NOT NULL, DEFAULT 0 | How often this mapping was used |
| SuccessRate | DECIMAL(5,2) | NULL | Success rate percentage for this mapping |
| CreatedBy | NVARCHAR(50) | NOT NULL | Admin, System, Import |
| CreatedAt | DATETIME2 | DEFAULT GETUTCDATE() | Record creation time (UTC) |
| LastUsedAt | DATETIME2 | NULL | Last time this mapping was applied |

### PartLaborKeywords Table (Fallback Support)
Stores keywords for fallback classification when GPT-4o is unavailable.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| KeywordID | INT IDENTITY | PRIMARY KEY | Auto-generated |
| Keyword | NVARCHAR(100) | NOT NULL | Classification keyword |
| Classification | NVARCHAR(20) | NOT NULL | Part, Labor, Fee, Tax, Other |
| MatchType | NVARCHAR(20) | NOT NULL | EXACT, PARTIAL, CONTAINS |
| Weight | DECIMAL(3,2) | NOT NULL, DEFAULT 1.0 | Keyword importance weight |
| IsActive | BIT | NOT NULL, DEFAULT 1 | Enable/disable keyword |
| UsageCount | INT | NOT NULL, DEFAULT 0 | How often this keyword was matched |
| SuccessRate | DECIMAL(5,2) | NULL | Success rate percentage for this keyword |
| CreatedBy | NVARCHAR(50) | NOT NULL | Admin, System, Import |
| CreatedAt | DATETIME2 | DEFAULT GETUTCDATE() | Record creation time (UTC) |
| LastUsedAt | DATETIME2 | NULL | Last time this keyword was matched |

---

## 9. Development Timeline (65 Working Days)

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
- GPT-4o integration for comprehensive processing
- Description summary generation
- Semantic similarity for field normalization
- Rate limit handling and fallback systems
- Comprehensive monitoring dashboard
- Performance analytics
- System optimization

---

## 10. Success Criteria

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

### Phase 4 (AI-Powered Enhancement - GPT-4o Integration)
- [ ] Implement GitHub Models API integration with GPT-4o
- [ ] Configure secure GitHub Personal Access Token authentication
- [ ] Create LLM testing and monitoring endpoints
- [ ] **Implement single comprehensive GPT-4o call processing pipeline**
- [ ] **Create structured JSON schema for complete invoice processing output**
- [ ] **Implement rate limit detection and graceful degradation to database fallback**
- [ ] **Create fallback processing using InvoiceFields and PartLaborKeywords tables**
- [ ] **Achieve 95% success rate for single-call comprehensive GPT-4o processing**
- [ ] **Maintain sub-15 second response times for complete invoice processing**
- [ ] Implement intelligent part number extraction using GPT-4o
- [ ] **Generate comprehensive description summaries of all line items using GPT-4o**
- [ ] Achieve 90% accuracy on field mapping through comprehensive AI processing
- [ ] Achieve 85% accuracy on part/labor classification through integrated AI analysis
- [ ] Implement data validation and confidence scoring for all AI outputs
- [ ] Create administrative controls for processing method selection and overrides
- [ ] Add cost monitoring and usage analytics for GPT-4o API consumption
- [ ] Implement error handling and recovery mechanisms for AI processing failures
- [ ] Achieve 92% overall accuracy on AI-enhanced invoice processing
- [ ] **Implement administrative interfaces for keyword and field mapping management**

### Phase 5 (Continuous Learning)
- [ ] Implement automated model retraining
- [ ] Achieve 90% accuracy on line item classification
- [ ] Create accuracy tracking dashboard
- [ ] Implement confidence-based manual review workflows
- [ ] Integrate AI feedback loops for continuous improvement

---

## 11. Risk Assessment

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
6. **GPT-4o Rate Limiting**: GitHub Models API rate limits may block comprehensive processing
   - *Mitigation*: Implement intelligent rate limit detection, graceful degradation to database fallback, retry logic with exponential backoff
7. **Single Point of AI Failure**: Dependency on single comprehensive GPT-4o call for processing
   - *Mitigation*: Robust fallback system using InvoiceFields and PartLaborKeywords tables, clear failure mode handling
8. **AI Processing Costs**: Single comprehensive calls may be more expensive per invoice
   - *Mitigation*: Cost monitoring, usage analytics, intelligent processing method selection based on complexity
9. **AI Data Security**: Comprehensive invoice data transmission to external AI services
   - *Mitigation*: Data sanitization, secure API endpoints, no persistent storage on AI services, audit logging
10. **AI Response Consistency**: Large comprehensive responses may have variable quality across different sections
    - *Mitigation*: Section-by-section confidence scoring, validation against business rules, partial fallback capabilities
11. **Fallback System Maintenance**: Database-driven fallback may become stale without continuous GPT-4o learning
    - *Mitigation*: Periodic batch updates to fallback tables, administrative tools for manual maintenance, quality metrics tracking

---

## 12. Quality Assurance and Testing

### Test Coverage Requirements
- **Unit Tests**: 90% code coverage for data extraction and parsing logic
- **Integration Tests**: End-to-end invoice processing with various formats
- **Regression Tests**: Automated tests for numeric parsing edge cases
- **Performance Tests**: Load testing with 1000+ invoices
- **Security Tests**: Vulnerability scanning and penetration testing
- **AI Processing Tests**: GPT-4o integration and description generation testing

### Numeric Extraction Test Cases
- Standard integers (e.g., "67890", "123456")
- Comma-separated numbers (e.g., "67,890", "123,456", "1,234,567")
- Numbers with leading/trailing spaces
- Invalid formats and edge cases
- Mixed format invoices
- Currency values with various formatting

### Description Summary Test Cases
- **Single service type**: Oil change items → "Oil Change Service"
- **Multiple brake items**: Brake pads, rotors, labor → "Brake System Repair"
- **Mixed maintenance**: Oil, filters, inspection → "Routine Maintenance"
- **Diagnostic work**: Computer diagnostic, troubleshooting → "Engine Diagnostics"
- **Complex invoices**: Multiple service categories → appropriate combined summary
- **Edge cases**: Empty descriptions, very long descriptions, special characters

---

## 13. Appendices

### A. Glossary
- **OCR**: Optical Character Recognition
- **API**: Application Programming Interface
- **MVP**: Minimum Viable Product
- **ML**: Machine Learning
- **Field Normalization**: Process of standardizing inconsistent field labels
- **Line Item Classification**: Process of categorizing invoice line items as Parts or Labor
- **Confidence Score**: Numerical score indicating system's certainty in classification
- **Comma-Separated Numbers**: Numeric format using commas as thousand separators (e.g., "1,234,567")
- **GPT-4o**: OpenAI's advanced language model integrated via GitHub Models API for intelligent text processing
- **LLM**: Large Language Model - AI system capable of understanding and generating human-like text
- **GitHub Models API**: Microsoft's API service providing access to various AI models including GPT-4o
- **AI Enhancement**: Process of using artificial intelligence to improve and validate extracted invoice data
- **Hybrid Processing**: Combination of traditional rule-based processing with AI-powered enhancement
- **Fallback Processing**: Automatic switch to traditional processing methods when AI services are unavailable
- **Database-Driven Classification**: Method of using stored keywords and field mappings in database tables for classification and normalization, reducing dependency on external AI services
- **Intelligent Fallback**: System that uses GPT-4o as a secondary method when primary database-driven methods don't provide results
- **Learning Loop**: Automatic process of storing successful GPT-4o results back to database for future direct matching
- **Cost Reduction Strategy**: Approach to minimize expensive AI API calls by building up database knowledge over time

### B. References
- Azure Form Recognizer Documentation
- Azure SQL Database Best Practices
- ASP.NET Core API Guidelines
- Azure App Service Deployment Guide
- ML.NET Text Classification Documentation
- Regular Expression Patterns for Numeric Data
- GitHub Models API Documentation
- OpenAI GPT-4o Model Documentation
- GitHub Personal Access Token Security Guidelines

### C. Technical Implementation Notes
- **Regex Pattern for Comma-Separated Numbers**: `\d{1,3}(?:,\d{3})+`
- **Fallback Logic**: Always attempt standard integer parsing if comma-separated parsing fails
- **Audit Trail**: Maintain original extracted text alongside parsed values
- **Performance**: Numeric parsing should not add more than 100ms to total processing time
- **GPT-4o API Endpoint**: `https://models.inference.ai.azure.com/chat/completions`
- **AI Request Timeout**: 30 seconds with exponential backoff retry logic
- **AI Enhancement Integration**: Execute AI processing in parallel with traditional methods for comparison
- **Cost Optimization**: Implement request batching and intelligent caching to minimize API calls
- **Security**: Sanitize invoice data before transmission to external AI services
- **Database-Driven Intelligence**: 
  - Primary processing uses database lookups for field mappings and classification keywords
  - Secondary processing uses GPT-4o only for items not found in database
  - Automatic learning saves successful GPT-4o results to database for future direct matching
  - Target: 80% reduction in GPT-4o calls within 6 months through accumulated learning
- **Keyword Management**: Support manual keyword addition, effectiveness tracking, and automatic pruning
- **Field Mapping Strategy**: Case-insensitive matching with support for exact, partial, and contains match types
