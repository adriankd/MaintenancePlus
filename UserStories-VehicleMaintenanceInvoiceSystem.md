# User Stories
## Vehicle Maintenance Invoice Processing System

### Document Information
- **Product**: Vehicle Maintenance Invoice Processing System
- **Version**: 1.1
- **Date**: August 18, 2025

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

## Updated Summary

### Story Point Distribution by Sprint
- **Sprint 1**: 8 points (Infrastructure & Upload)
- **Sprint 2**: 21 points (Core Processing)
- **Sprint 3**: 14 points (API Development)
- **Sprint 4**: 24 points (Quality & Core Approval Features)
- **Sprint 5**: 13 points (Advanced Approval Features)
- **Sprint 6**: 8 points (Audit & Compliance)

**Total**: 88 story points

### Priority Distribution
- **High Priority**: 15 stories (Core functionality + Approval workflow)
- **Medium Priority**: 6 stories (Enhancement features)
- **Low Priority**: 1 story (Audit trail)

### Priority Distribution
- **High Priority**: 9 stories (Core functionality)
- **Medium Priority**: 4 stories (Enhancement features)

### Epic Summary
1. **File Upload & Storage**: 2 stories, 8 points
2. **Data Extraction**: 3 stories, 21 points
3. **Data Validation**: 1 story, 5 points
4. **API Development**: 4 stories, 14 points
5. **System Administration**: 2 stories, 10 points
6. **Performance**: 1 story, 8 points
7. **Invoice Approval Workflow**: 8 stories, 39 points

### New Epic 7: Invoice Approval Workflow Details
- **US-014**: View Invoice Approval Status (3 points) - UI status display
- **US-015**: Approve Invoice (5 points) - Core approval functionality  
- **US-016**: Reject Invoice with Complete Deletion (8 points) - Core rejection functionality
- **US-017**: Approval Workflow API Endpoints (5 points) - API integration
- **US-018**: Approval Status Filtering (5 points) - Search and filter capabilities
- **US-019**: Bulk Approval Operations (8 points) - Batch processing
- **US-020**: Approval History and Audit Trail (8 points) - Compliance and reporting
- **US-021**: Database Schema Migration (3 points) - Infrastructure support

### Approval Feature Implementation Priority
1. **Phase 1 (Sprint 4)**: Core approval/rejection functionality (US-014, US-015, US-016, US-017, US-021)
2. **Phase 2 (Sprint 5)**: Enhanced user experience (US-018, US-019)  
3. **Phase 3 (Sprint 6)**: Audit and compliance features (US-020)
