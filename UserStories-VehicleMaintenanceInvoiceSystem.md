# User Stories
## Vehicle Maintenance Invoice Processing System

### Document Information
- **Product**: Vehicle Maintenance Invoice Processing System
- **Version**: 2.0
- **Date**: August 19, 2025

---

## Epic 1: File Upload & Storage Management

### US-001: Invoice File Upload
**As a** fleet manager  
**I want to** upload PDF or PNG vehicle maintenance invoices through a web interface  
**So that** I can process multiple invoice formats without manual data entry

**Acceptance Criteria:**
- Given I am on the upload page
- When I select a PDF or PNG file up to 10MB
- Then the system accepts the file and shows upload progress
- And displays a success message when upload completes
- And rejects files that are not PDF/PNG with clear error messages
- And rejects files larger than 10MB with appropriate messaging

**Definition of Done:**
- [ ] Upload interface implemented
- [ ] File type validation working
- [ ] File size validation working
- [ ] Progress indicator functional
- [ ] Error handling implemented
- [ ] Unit tests written
- [ ] Integration tests passing

**Story Points:** 5  
**Priority:** High  
**Sprint:** 1

---

### US-002: Original File Storage
**As a** system administrator  
**I want** uploaded invoice files automatically stored in Azure Blob Storage  
**So that** we maintain audit trails and can reference original documents

**Acceptance Criteria:**
- Given a file has been successfully uploaded
- When the system processes the file
- Then the original file is stored in Azure Blob Storage
- And a unique URL is generated for future access
- And file metadata is recorded (filename, upload date, size)
- And all files are stored in a single container

**Definition of Done:**
- [ ] Azure Blob Storage integration complete
- [ ] Single container storage implemented
- [ ] Metadata tracking functional
- [ ] Unique URL generation working
- [ ] Error handling for storage failures
- [ ] Unit tests written

**Story Points:** 3  
**Priority:** High  
**Sprint:** 1

---

## Epic 2: Data Extraction & Processing

### US-003: Invoice Header Data Extraction
**As a** system  
**I need to** extract invoice header information from uploaded documents  
**So that** key invoice details are available in structured format

**Acceptance Criteria:**
- Given a PDF or PNG invoice file has been uploaded
- When the OCR processing begins
- Then the system extracts the following header data:
  - Vehicle ID
  - Odometer reading
  - Invoice Number
  - Invoice Date
  - Total Cost
  - Total Parts Cost
  - Total Labor Cost
- And provides confidence scores for each extracted field
- And flags low-confidence extractions for manual review

**Definition of Done:**
- [ ] Azure Form Recognizer integration complete
- [ ] Header data extraction logic implemented
- [ ] Confidence scoring working
- [ ] Manual review flagging functional
- [ ] Error handling for extraction failures
- [ ] Unit tests written
- [ ] Accuracy testing completed

**Story Points:** 8  
**Priority:** High  
**Sprint:** 2

---

### US-004: Invoice Line Items Extraction
**As a** system  
**I need to** extract individual line item details from invoices  
**So that** detailed parts and labor information is available

**Acceptance Criteria:**
- Given an invoice with multiple line items
- When OCR processing occurs
- Then the system extracts for each line item:
  - Description
  - Unit Cost
  - Quantity
  - Total Line Cost
- And maintains the relationship to the invoice header
- And handles variable numbers of line items per invoice
- And provides confidence scores for extracted line data

**Definition of Done:**
- [ ] Line item extraction logic implemented
- [ ] Variable line item handling working
- [ ] Header-line relationship maintained
- [ ] Confidence scoring functional
- [ ] Edge case handling (no line items, many line items)
- [ ] Unit tests written
- [ ] Integration tests passing

**Story Points:** 8  
**Priority:** High  
**Sprint:** 2

---

### US-005: Database Storage of Extracted Data
**As a** system  
**I need to** store extracted invoice data in Azure SQL Database  
**So that** the information is persistent and queryable

**Acceptance Criteria:**
- Given invoice data has been successfully extracted
- When the extraction process completes
- Then header data is stored in the InvoiceHeader table
- And line item data is stored in the InvoiceLines table
- And foreign key relationships are maintained
- And all database transactions are atomic
- And timestamps are recorded for audit purposes

**Definition of Done:**
- [ ] Database schema implemented
- [ ] Data access layer complete
- [ ] Transaction handling implemented
- [ ] Foreign key constraints working
- [ ] Audit timestamp functionality
- [ ] Error handling for database failures
- [ ] Unit tests written
- [ ] Performance testing completed

**Story Points:** 5  
**Priority:** High  
**Sprint:** 2

---

## Epic 3: Data Validation & Quality

### US-006: Data Validation and Quality Assurance
**As a** system administrator  
**I want** extracted data to be validated before storage  
**So that** data quality is maintained and errors are caught early

**Acceptance Criteria:**
- Given data has been extracted from an invoice
- When validation occurs
- Then required fields are checked for presence
- And data types are validated (dates, numbers, text)
- And business rules are applied (positive costs, valid dates)
- And validation errors are logged with details
- And failed validations prevent database storage

**Definition of Done:**
- [ ] Validation rules implemented
- [ ] Business logic validation working
- [ ] Error logging functional
- [ ] Database constraint enforcement
- [ ] Validation reporting implemented
- [ ] Unit tests written

**Story Points:** 5  
**Priority:** Medium  
**Sprint:** 2

---

## Epic 4: API Development & Data Access

### US-007: Retrieve All Invoices API
**As an** external system integrator  
**I want to** retrieve a list of all invoices via REST API  
**So that** I can integrate invoice data into my application

**Acceptance Criteria:**
- Given I make a GET request to /api/invoices
- When the API processes the request
- Then I receive a JSON response with invoice header data
- And the response includes pagination parameters
- And I can specify page size (max 100 items)
- And metadata includes total count and page information
- And response time is under 2 seconds

**Definition of Done:**
- [ ] API endpoint implemented
- [ ] Pagination functionality working
- [ ] JSON serialization correct
- [ ] Performance requirements met
- [ ] Error handling implemented
- [ ] API documentation updated
- [ ] Integration tests passing

**Story Points:** 5  
**Priority:** High  
**Sprint:** 3

---

### US-008: Retrieve Single Invoice API
**As an** external system integrator  
**I want to** retrieve a specific invoice with all line items  
**So that** I can access complete invoice details

**Acceptance Criteria:**
- Given I make a GET request to /api/invoices/{id}
- When the API processes the request with a valid ID
- Then I receive complete invoice data including all line items
- And the response includes header information
- And line items are nested within the invoice object
- And invalid IDs return appropriate 404 errors
- And response includes the original file URL

**Definition of Done:**
- [ ] API endpoint implemented
- [ ] Complete data serialization working
- [ ] Nested object structure correct
- [ ] Error handling for invalid IDs
- [ ] Performance optimization implemented
- [ ] Unit tests written
- [ ] Integration tests passing

**Story Points:** 3  
**Priority:** High  
**Sprint:** 3

---

### US-009: Search Invoices by Vehicle API
**As an** external system integrator  
**I want to** search for invoices by vehicle ID  
**So that** I can retrieve maintenance history for specific vehicles

**Acceptance Criteria:**
- Given I make a GET request to /api/invoices/vehicle/{vehicleId}
- When the API processes the request
- Then I receive all invoices for the specified vehicle
- And results are paginated for large result sets
- And invoices are ordered by date (newest first)
- And empty results return appropriate response
- And invalid vehicle IDs return empty result set

**Definition of Done:**
- [ ] Search endpoint implemented
- [ ] Vehicle ID filtering working
- [ ] Pagination implemented
- [ ] Sorting functionality correct
- [ ] Empty result handling
- [ ] Performance optimization
- [ ] Unit tests written

**Story Points:** 3  
**Priority:** High  
**Sprint:** 3

---

### US-010: Retrieve Invoices by Date API
**As an** external system integrator  
**I want to** retrieve all invoices processed on a specific date  
**So that** I can perform daily reconciliation and reporting

**Acceptance Criteria:**
- Given I make a GET request to /api/invoices/date/{date}
- When the API processes the request with a valid date (YYYY-MM-DD)
- Then I receive all invoices processed on that date
- And results include both header and summary information
- And pagination is available for large result sets
- And invalid date formats return 400 Bad Request
- And dates with no invoices return empty result set

**Definition of Done:**
- [ ] Date-based endpoint implemented
- [ ] Date parsing and validation working
- [ ] Filtering logic correct
- [ ] Pagination implemented
- [ ] Error handling for invalid dates
- [ ] Unit tests written
- [ ] Integration tests passing

**Story Points:** 3  
**Priority:** High  
**Sprint:** 3

---

## Epic 5: System Administration & Monitoring

### US-011: Processing Status Tracking
**As a** system administrator  
**I want to** track the processing status of uploaded invoices  
**So that** I can monitor system performance and troubleshoot issues

**Acceptance Criteria:**
- Given an invoice has been uploaded
- When processing occurs
- Then the system tracks status (Uploaded, Processing, Completed, Failed)
- And processing timestamps are recorded
- And error details are logged for failed processing
- And administrators can view processing status via interface
- And alerts are generated for processing failures

**Definition of Done:**
- [ ] Status tracking implemented
- [ ] Timestamp recording working
- [ ] Error logging functional
- [ ] Admin interface created
- [ ] Alert system implemented
- [ ] Monitoring dashboard available

**Story Points:** 5  
**Priority:** Medium  
**Sprint:** 4

---

### US-012: Error Handling and Recovery
**As a** system administrator  
**I want** comprehensive error handling throughout the system  
**So that** failures are gracefully managed and operations can be retried

**Acceptance Criteria:**
- Given any system operation fails
- When the error occurs
- Then appropriate error messages are displayed to users
- And detailed error information is logged for administrators
- And the system remains stable and responsive
- And failed operations can be retried when appropriate
- And users receive helpful guidance for resolution

**Definition of Done:**
- [ ] Global error handling implemented
- [ ] User-friendly error messages
- [ ] Comprehensive logging system
- [ ] Retry mechanisms where appropriate
- [ ] System stability maintained
- [ ] Error recovery procedures documented

**Story Points:** 5  
**Priority:** Medium  
**Sprint:** 4

---

## Epic 6: Performance & Scalability

### US-013: Asynchronous Processing
**As a** user  
**I want** invoice processing to occur in the background  
**So that** I can continue working while files are being processed

**Acceptance Criteria:**
- Given I upload an invoice file
- When the upload completes
- Then processing begins asynchronously
- And I receive immediate confirmation of upload
- And I can track processing progress
- And I receive notification when processing completes
- And the system can handle multiple concurrent uploads

**Definition of Done:**
- [ ] Asynchronous processing implemented
- [ ] Progress tracking functional
- [ ] Notification system working
- [ ] Concurrent processing supported
- [ ] Queue management implemented
- [ ] Performance testing completed

**Story Points:** 8  
**Priority:** Medium  
**Sprint:** 4

---

## Summary

### Story Point Distribution by Sprint
- **Sprint 1**: 8 points (Infrastructure & Upload)
- **Sprint 2**: 21 points (Core Processing)
- **Sprint 3**: 14 points (API Development)
- **Sprint 4**: 18 points (Quality & Performance)

**Total**: 61 story points

---

## Epic 7: Invoice Approval Workflow

### US-014: View Invoice Approval Status
**As a** fleet manager  
**I want to** see the approval status of processed invoices  
**So that** I can identify which invoices need my review and approval

**Acceptance Criteria:**
- Given I am viewing an invoice details page
- When the page loads
- Then I can see the current approval status clearly displayed
- And pending invoices show a "Pending Approval" badge in yellow/orange
- And approved invoices show an "Approved" badge in green
- And approved invoices display the approval date and approving user
- And the approval status is visible at the top of the invoice details
- And the status uses clear, consistent visual indicators

**Definition of Done:**
- [ ] Approval status display implemented
- [ ] Visual status badges created
- [ ] Approval metadata display working
- [ ] Consistent styling applied
- [ ] Responsive design implemented
- [ ] Accessibility standards met
- [ ] Cross-browser testing completed

**Story Points:** 3  
**Priority:** High  
**Sprint:** 4

---

### US-015: Approve Invoice
**As a** fleet manager  
**I want to** approve processed invoices after reviewing the extracted data  
**So that** I can confirm the invoice data is accurate and authorize it for further processing

**Acceptance Criteria:**
- Given I am viewing an unapproved invoice details page
- When I click the "Approve" button
- Then I see a confirmation dialog asking "Are you sure you want to approve this invoice? Once approved, it cannot be rejected or modified."
- And the dialog has "Yes, Approve" (green) and "Cancel" (gray) buttons
- And when I confirm approval, the system updates the database
- And the invoice status changes to "Approved" immediately
- And I see a success message "✓ Invoice has been approved successfully"
- And the Approve and Reject buttons are hidden after approval
- And the approval is recorded with timestamp and my user information
- And the page updates to show the new approval status

**Definition of Done:**
- [ ] Approve button implemented on invoice details page
- [ ] Confirmation dialog working with proper messaging
- [ ] Database update functionality implemented
- [ ] Success message display working
- [ ] UI state updates correctly after approval
- [ ] Approval metadata recording functional
- [ ] Button visibility logic implemented
- [ ] Unit tests written
- [ ] Integration tests passing

**Story Points:** 5  
**Priority:** High  
**Sprint:** 4

---

### US-016: Reject Invoice with Complete Deletion
**As a** fleet manager  
**I want to** reject invoices that contain incorrect or invalid data  
**So that** bad data doesn't pollute the system and I can re-process the invoice correctly

**Acceptance Criteria:**
- Given I am viewing an unapproved invoice details page
- When I click the "Reject" button
- Then I see a warning confirmation dialog with the message "⚠️ Are you sure you want to reject this invoice? This will permanently delete the invoice data and original file. This action cannot be undone."
- And the dialog has "Yes, Delete Forever" (red) and "Cancel" (gray) buttons
- And when I confirm rejection, the system performs the following atomically:
  - Deletes the original file from Azure Blob Storage
  - Deletes all related invoice line items from the database
  - Deletes the invoice header record from the database
- And I am redirected to the upload page after successful deletion
- And I see a success message "✓ Invoice has been rejected and removed from the system"
- And if deletion fails partially, I receive an appropriate error message
- And the rejection action is logged for audit purposes

**Definition of Done:**
- [ ] Reject button implemented on invoice details page
- [ ] Warning confirmation dialog with strong messaging
- [ ] Atomic deletion process implemented (blob + database)
- [ ] Proper transaction handling for data integrity
- [ ] Redirect to upload page after successful rejection
- [ ] Success and error message handling
- [ ] Audit logging for rejection actions
- [ ] Error handling for partial deletion failures
- [ ] Unit tests written
- [ ] Integration tests passing

**Story Points:** 8  
**Priority:** High  
**Sprint:** 4

---

### US-017: Approval Workflow API Endpoints
**As an** external system integrator  
**I want to** programmatically approve or reject invoices via API  
**So that** I can integrate approval workflows into automated systems

**Acceptance Criteria:**
- Given I have a valid invoice ID for an unapproved invoice
- When I make a PUT request to `/api/invoices/{id}/approve`
- Then the system updates the invoice approval status to true
- And returns the updated invoice data with approval metadata
- And returns 404 if the invoice doesn't exist
- And returns 400 if the invoice is already approved
- Given I have a valid invoice ID for an unapproved invoice
- When I make a DELETE request to `/api/invoices/{id}/reject`
- Then the system deletes the blob file, line items, and header record
- And returns 200 with success confirmation
- And returns 404 if the invoice doesn't exist
- And returns 400 if the invoice is already approved
- And both endpoints include proper error handling and logging

**Definition of Done:**
- [ ] PUT /api/invoices/{id}/approve endpoint implemented
- [ ] DELETE /api/invoices/{id}/reject endpoint implemented
- [ ] Proper HTTP status code handling
- [ ] Input validation and error responses
- [ ] API documentation updated
- [ ] Unit tests written
- [ ] Integration tests passing
- [ ] Swagger documentation updated

**Story Points:** 5  
**Priority:** High  
**Sprint:** 4

---

### US-018: Approval Status Filtering and Search
**As a** fleet manager  
**I want to** filter and search invoices by approval status  
**So that** I can efficiently find invoices that need my attention

**Acceptance Criteria:**
- Given I am on the invoice list page
- When I apply an approval status filter
- Then I can filter by "All", "Pending Approval", or "Approved"
- And the list updates to show only invoices matching the selected status
- And the filter state is maintained when navigating pages
- Given I use the API to retrieve invoices
- When I include approval status parameters
- Then I can filter API results by approval status
- And paginated results respect the approval status filter
- And the total count reflects the filtered results

**Definition of Done:**
- [ ] Approval status filter UI implemented
- [ ] Filter state management working
- [ ] Database queries updated to support filtering
- [ ] API endpoints updated with approval status parameters
- [ ] Pagination works correctly with filters
- [ ] Filter persistence across navigation
- [ ] Unit tests written
- [ ] Integration tests passing

**Story Points:** 5  
**Priority:** Medium  
**Sprint:** 5

---

### US-019: Bulk Approval Operations
**As a** fleet manager  
**I want to** approve multiple invoices at once  
**So that** I can efficiently process large batches of routine invoices

**Acceptance Criteria:**
- Given I am on the invoice list page
- When I select multiple unapproved invoices using checkboxes
- Then I can click a "Approve Selected" button
- And I see a confirmation dialog showing the count of selected invoices
- And when I confirm, all selected invoices are approved simultaneously
- And I see a progress indicator during bulk processing
- And I receive a summary of successful and failed approvals
- And the list updates to reflect the new approval statuses
- And only unapproved invoices can be selected for bulk approval

**Definition of Done:**
- [ ] Multi-select checkbox functionality implemented
- [ ] Bulk approve button and logic working
- [ ] Confirmation dialog with invoice count
- [ ] Progress indicator during bulk operations
- [ ] Result summary display
- [ ] Error handling for partial failures
- [ ] UI updates after bulk operations
- [ ] Performance optimization for large batches
- [ ] Unit tests written

**Story Points:** 8  
**Priority:** Medium  
**Sprint:** 5

---

### US-020: Approval History and Audit Trail
**As a** system administrator  
**I want to** view a complete audit trail of approval actions  
**So that** I can track who approved what invoices and when for compliance purposes

**Acceptance Criteria:**
- Given approval actions have occurred in the system
- When I access the approval history page
- Then I can see a chronological list of all approval/rejection actions
- And each entry shows: invoice ID, action (approved/rejected), user, timestamp, and invoice details
- And I can filter the history by date range, user, or action type
- And I can search for specific invoice numbers or vehicle IDs
- And the history includes both web UI and API-driven actions
- And rejected invoices show as "Deleted" with reference to original data
- And the history is paginated for performance

**Definition of Done:**
- [ ] Approval history data model implemented
- [ ] History logging for all approval actions
- [ ] Approval history page created
- [ ] Filtering and search functionality working
- [ ] Pagination implemented
- [ ] Historical data display includes all relevant details
- [ ] Performance optimization for large datasets
- [ ] Export functionality for compliance reporting
- [ ] Unit tests written

**Story Points:** 8  
**Priority:** Low  
**Sprint:** 6

---

### US-021: Database Schema Migration for Approval
**As a** system administrator  
**I need** the database schema updated to support approval functionality  
**So that** approval status and metadata can be properly stored and tracked

**Acceptance Criteria:**
- Given the current database schema exists
- When the migration is applied
- Then the InvoiceHeader table includes new columns:
  - Approved (BIT, NOT NULL, DEFAULT 0)
  - ApprovedAt (DATETIME2, NULL)  
  - ApprovedBy (NVARCHAR(100), NULL)
- And existing invoice records default to Approved = FALSE
- And database indexes are created for approval status queries
- And the migration can be applied to existing production data
- And rollback procedures are documented and tested

**Definition of Done:**
- [ ] Database migration script created
- [ ] Migration tested on sample data
- [ ] Index creation included in migration
- [ ] Default value handling for existing records
- [ ] Rollback script created and tested
- [ ] Migration documentation written
- [ ] Production deployment plan created
- [ ] Database constraints validated

**Story Points:** 3  
**Priority:** High  
**Sprint:** 4

---

## Epic 8: Line Item Classification

### US-022: Rule-Based Line Item Classification
**As a** system administrator  
**I want** the system to automatically classify invoice line items as "Part" or "Labor" using keyword-based rules  
**So that** invoices are consistently categorized without manual data entry

**Acceptance Criteria:**
- Given an invoice line item has been extracted from OCR
- When the classification process runs
- Then the system analyzes the description text using predefined keywords
- And classifies items containing "part", "filter", "oil", "brake", "tire", "battery" as "Part"
- And classifies items containing "labor", "service", "diagnostic", "repair", "hour" as "Labor"
- And assigns confidence scores based on keyword match strength
- And stores the classification result with the method marked as "Rule-based"
- And handles edge cases where no keywords match (default to manual review)
- And achieves 75% accuracy on typical automotive maintenance invoices

**Definition of Done:**
- [ ] Keyword dictionary implemented with automotive terms
- [ ] Classification algorithm with confidence scoring
- [ ] Integration into invoice processing pipeline
- [ ] Database schema updated to store classification results
- [ ] Classification method and version tracking
- [ ] Edge case handling for ambiguous descriptions
- [ ] Unit tests for classification logic
- [ ] Performance testing on sample invoices
- [ ] Accuracy measurement framework

**Story Points:** 8  
**Priority:** High  
**Sprint:** 5

---

### US-023: Display Line Item Classifications
**As a** fleet manager  
**I want to** see the Part/Labor classification for each line item on invoice details  
**So that** I can verify the system's categorization is accurate

**Acceptance Criteria:**
- Given I am viewing an invoice details page
- When the page displays line items
- Then each line item shows its classification (Part/Labor) with a visual indicator
- And displays the confidence score as a percentage
- And shows the classification method used (Rule-based, ML, Manual)
- And uses distinct colors or icons for Part vs Labor categories
- And includes tooltips explaining the classification criteria
- And maintains responsive design on mobile and desktop

**Definition of Done:**
- [ ] UI components for classification display
- [ ] Visual indicators (colors, icons, badges)
- [ ] Confidence score formatting and display
- [ ] Responsive design implementation
- [ ] Tooltip functionality with explanations
- [ ] Cross-browser compatibility testing
- [ ] Accessibility compliance (screen readers)
- [ ] Integration with existing invoice details page

**Story Points:** 5  
**Priority:** High  
**Sprint:** 5

---

### US-024: Classification Feedback Collection
**As a** fleet manager  
**I want to** correct misclassified line items and provide feedback  
**So that** the system can learn from my corrections and improve accuracy over time

**Acceptance Criteria:**
- Given I am viewing an invoice with classified line items
- When I see an incorrectly classified item
- Then I can click a "Correct Classification" button next to the item
- And select the correct classification (Part or Labor) from a dropdown
- And optionally add a comment explaining the correction
- And submit the feedback to be stored in the system
- And see immediate visual confirmation that my feedback was recorded
- And the corrected classification updates the display immediately
- And the feedback is logged for future model training

**Definition of Done:**
- [ ] "Correct Classification" UI controls on line items
- [ ] Feedback modal dialog with classification options
- [ ] Comment field for user explanations
- [ ] Feedback submission API endpoint
- [ ] Database storage for classification feedback
- [ ] Immediate UI updates after feedback submission
- [ ] User identification and timestamp logging
- [ ] Success/error message handling
- [ ] Integration tests for feedback workflow

**Story Points:** 8  
**Priority:** High  
**Sprint:** 5

---

### US-025: Machine Learning Classification Model
**As a** system administrator  
**I want** to implement ML-based line item classification to improve accuracy beyond keyword rules  
**So that** the system can handle complex descriptions and learn from accumulated feedback

**Acceptance Criteria:**
- Given sufficient training data from rule-based classification and user feedback
- When the ML model training process runs
- Then a text classification model is trained using ML.NET or Azure AI Language
- And the model achieves 85% accuracy on validation data
- And can process line item descriptions to predict Part vs Labor category
- And provides confidence scores for each prediction
- And integrates with the existing processing pipeline
- And supports A/B testing against rule-based classification
- And can be updated with new training data periodically

**Definition of Done:**
- [ ] ML.NET text classification model implementation
- [ ] Training pipeline using existing classified data
- [ ] Model evaluation and accuracy measurement
- [ ] Integration with invoice processing workflow
- [ ] A/B testing framework for model comparison
- [ ] Model versioning and deployment system
- [ ] Confidence threshold configuration
- [ ] Performance benchmarking
- [ ] Documentation for model training process

**Story Points:** 13  
**Priority:** Medium  
**Sprint:** 6

---

### US-026: Batch Reclassification
**As a** system administrator  
**I want to** reclassify historical invoices when the classification model improves  
**So that** all invoice data maintains consistent and accurate categorization

**Acceptance Criteria:**
- Given a new classification model has been deployed
- When I trigger batch reclassification
- Then the system identifies invoices processed with older classification versions
- And processes all line items through the new classification model
- And updates the database with new classifications and confidence scores
- And preserves the original classification data for comparison
- And provides progress tracking for large batch operations
- And handles failures gracefully with retry capabilities
- And generates a report of classification changes

**Definition of Done:**
- [ ] Batch reclassification API endpoint
- [ ] Background job processing for large batches
- [ ] Progress tracking and status reporting
- [ ] Data migration logic preserving history
- [ ] Error handling and retry mechanisms
- [ ] Performance optimization for large datasets
- [ ] Classification change reporting
- [ ] Administrative UI for triggering batch operations
- [ ] Unit and integration tests

**Story Points:** 8  
**Priority:** Medium  
**Sprint:** 6

---

## Epic 9: Field Label Normalization

### US-027: Dictionary-Based Field Normalization
**As a** system administrator  
**I want** the system to normalize inconsistent field labels from different invoice formats  
**So that** data is consistently stored using standardized field names

**Acceptance Criteria:**
- Given an invoice has been processed by OCR extraction
- When field normalization runs
- Then the system maps common field variations to standardized names:
  - "Invoice", "Invoice No", "RO#" → "InvoiceNumber"
  - "Mileage", "Odometer" → "Odometer"  
  - "Vehicle ID", "Vehicle Registration" → "VehicleRegistration"
- And stores both original and normalized field labels
- And handles case-insensitive matching
- And provides confidence scores for normalization decisions
- And logs all normalization activities for analysis

**Definition of Done:**
- [ ] Field normalization dictionary with common variations
- [ ] Normalization algorithm with confidence scoring
- [ ] Database schema updated to store original labels
- [ ] Integration with OCR processing pipeline
- [ ] Case-insensitive matching implementation
- [ ] Normalization logging and audit trail
- [ ] Unit tests for normalization logic
- [ ] Configuration system for easy dictionary updates

**Story Points:** 5  
**Priority:** High  
**Sprint:** 5

---

### US-028: Display Normalized Field Information
**As a** fleet manager  
**I want to** see both original and normalized field labels on invoice details  
**So that** I can verify the normalization is correct and understand how fields were mapped

**Acceptance Criteria:**
- Given I am viewing an invoice details page
- When field normalization has been applied
- Then I can see the standardized field labels in the main display
- And can view the original extracted labels via hover tooltips or expandable sections
- And see visual indicators when fields have been normalized
- And view the confidence score for each normalization
- And access a "Field Mapping" section showing all normalization decisions

**Definition of Done:**
- [ ] UI display of normalized field labels
- [ ] Tooltip or expandable view for original labels
- [ ] Visual indicators for normalized fields
- [ ] Confidence score display formatting
- [ ] Field mapping summary section
- [ ] Responsive design for mobile/desktop
- [ ] User experience testing
- [ ] Integration with existing invoice details layout

**Story Points:** 3  
**Priority:** High  
**Sprint:** 5

---

### US-029: Field Normalization Feedback
**As a** fleet manager  
**I want to** correct incorrect field normalizations and provide feedback  
**So that** the system can improve its field mapping accuracy over time

**Acceptance Criteria:**
- Given I am viewing an invoice with normalized fields
- When I notice an incorrect field mapping
- Then I can click "Report Incorrect Mapping" next to the field
- And specify what the correct normalized field should be
- And provide an optional comment explaining the correction
- And submit the feedback to improve future normalizations
- And see confirmation that my feedback was recorded
- And the feedback is stored for system improvement

**Definition of Done:**
- [ ] "Report Incorrect Mapping" UI controls
- [ ] Feedback modal with normalization correction options
- [ ] Comment field for user explanations
- [ ] Feedback submission and storage system
- [ ] Database schema for normalization feedback
- [ ] User identification and timestamp logging
- [ ] Success confirmation and error handling
- [ ] Integration tests for feedback collection

**Story Points:** 5  
**Priority:** High  
**Sprint:** 5

---

### US-030: Semantic Field Normalization
**As a** system administrator  
**I want** to implement semantic similarity matching for field normalization  
**So that** the system can handle new field label variations not in the dictionary

**Acceptance Criteria:**
- Given a field label is not found in the normalization dictionary
- When semantic normalization runs
- Then the system uses embedding-based similarity to find the closest match
- And calculates similarity scores against known normalized field names
- And applies normalization when similarity exceeds confidence threshold
- And routes low-confidence matches to manual review
- And learns from user corrections to improve similarity matching
- And handles new field variations discovered in invoices

**Definition of Done:**
- [ ] Embedding model integration (Azure OpenAI or sentence transformers)
- [ ] Similarity calculation and threshold configuration
- [ ] Integration with existing normalization pipeline
- [ ] Manual review workflow for low-confidence matches
- [ ] Learning pipeline from user feedback
- [ ] Performance optimization for real-time processing
- [ ] Accuracy measurement and monitoring
- [ ] Documentation for similarity threshold tuning

**Story Points:** 13  
**Priority:** Medium  
**Sprint:** 7

---

### US-031: Normalization Accuracy Monitoring
**As a** system administrator  
**I want to** monitor field normalization accuracy and performance over time  
**So that** I can identify areas for improvement and track system effectiveness

**Acceptance Criteria:**
- Given field normalization has been running for a period of time
- When I access the normalization monitoring dashboard
- Then I can see accuracy metrics by field type and time period
- And view trends in normalization confidence scores
- And see the most common user corrections
- And identify field variations that frequently cause problems
- And track the impact of system improvements over time
- And export reports for analysis and documentation

**Definition of Done:**
- [ ] Normalization accuracy tracking system
- [ ] Dashboard displaying metrics and trends
- [ ] User correction analysis and reporting
- [ ] Problem field identification algorithms
- [ ] Historical trend visualization
- [ ] Report export functionality
- [ ] Administrative access controls
- [ ] Performance monitoring for dashboard queries

**Story Points:** 8  
**Priority:** Medium  
**Sprint:** 7

---

## Epic 10: Unified Processing Pipeline Enhancement

### US-032: Enhanced Processing Pipeline Integration
**As a** system architect  
**I want** to integrate classification and normalization into the core invoice processing workflow  
**So that** all invoices benefit from intelligent processing without disrupting existing functionality

**Acceptance Criteria:**
- Given an invoice file is uploaded for processing
- When the processing pipeline executes
- Then the workflow follows: OCR Extract → Normalize Fields → Classify Line Items → Store Data
- And each step preserves metadata and confidence scores
- And processing continues gracefully if individual steps fail partially  
- And the system maintains backward compatibility with existing data
- And processing time remains under 30 seconds per invoice
- And all steps are logged for debugging and audit purposes

**Definition of Done:**
- [ ] Unified processing pipeline architecture
- [ ] Integration points for normalization and classification
- [ ] Error handling and graceful degradation
- [ ] Backward compatibility with existing invoices
- [ ] Performance optimization and monitoring
- [ ] Comprehensive logging and audit trail
- [ ] Integration tests for full pipeline
- [ ] Documentation for pipeline architecture

**Story Points:** 8  
**Priority:** High  
**Sprint:** 5

---

## Updated Summary

### Story Point Distribution by Sprint
- **Sprint 1**: 8 points (Infrastructure & Upload)
- **Sprint 2**: 21 points (Core Processing)
- **Sprint 3**: 14 points (API Development)
- **Sprint 4**: 24 points (Quality & Core Approval Features)
- **Sprint 5**: 29 points (Intelligence Features - Classification & Normalization)
- **Sprint 6**: 21 points (Advanced ML Features)
- **Sprint 7**: 21 points (Semantic Enhancement & Monitoring)

**Total**: 138 story points

### Priority Distribution
- **High Priority**: 22 stories (Core functionality + Approval workflow + Intelligence features)
- **Medium Priority**: 10 stories (Enhancement features + Advanced ML)
- **Low Priority**: 1 story (Audit trail)

### Epic Summary
1. **File Upload & Storage**: 2 stories, 8 points
2. **Data Extraction**: 3 stories, 21 points  
3. **Data Validation**: 1 story, 5 points
4. **API Development**: 4 stories, 14 points
5. **System Administration**: 2 stories, 10 points
6. **Performance**: 1 story, 8 points
7. **Invoice Approval Workflow**: 8 stories, 39 points
8. **Line Item Classification**: 5 stories, 42 points  
9. **Field Label Normalization**: 5 stories, 34 points
10. **Unified Processing Pipeline**: 1 story, 8 points

### New Intelligence Features Implementation Priority

#### Epic 8: Line Item Classification Details
- **US-022**: Rule-Based Line Item Classification (8 points) - Core classification logic
- **US-023**: Display Line Item Classifications (5 points) - UI display of classifications  
- **US-024**: Classification Feedback Collection (8 points) - User correction system
- **US-025**: Machine Learning Classification Model (13 points) - ML enhancement
- **US-026**: Batch Reclassification (8 points) - Historical data processing

#### Epic 9: Field Label Normalization Details  
- **US-027**: Dictionary-Based Field Normalization (5 points) - Core normalization logic
- **US-028**: Display Normalized Field Information (3 points) - UI display of normalizations
- **US-029**: Field Normalization Feedback (5 points) - User correction system
- **US-030**: Semantic Field Normalization (13 points) - ML-based similarity matching
- **US-031**: Normalization Accuracy Monitoring (8 points) - Analytics and monitoring

#### Epic 10: Processing Pipeline Enhancement
- **US-032**: Enhanced Processing Pipeline Integration (8 points) - Unified workflow

### Implementation Phases
1. **Phase 1 (Sprint 4)**: Core approval/rejection functionality
2. **Phase 2 (Sprint 5)**: Rule-based intelligence features (classification & normalization)
3. **Phase 3 (Sprint 6)**: Machine learning enhancements  
4. **Phase 4 (Sprint 7)**: Advanced semantic features and monitoring
5. **Phase 5 (Future)**: Additional approval workflow enhancements
