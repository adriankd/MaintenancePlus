# End-to-End QA Development and Testing Plan
## Vehicle Maintenance Invoice Processing System

### Document Information
- **Version**: 1.0
- **Date**: August 28, 2025
- **Branch**: E2E-qa
- **Team**: QA Development Team
- **Application**: Maintenance Plus (Vehicle Invoice Processing System)

---

## 1. Executive Summary

### Purpose
This comprehensive E2E QA plan ensures the Vehicle Maintenance Invoice Processing System meets all functional requirements through systematic testing across all layers - from file upload through AI processing to data storage and API access.

### Scope
- **Full Application Testing**: Web UI, API endpoints, database operations
- **AI Integration Testing**: GPT-4o processing, fallback mechanisms, rate limit handling
- **Performance Testing**: Load testing, response time validation
- **Security Testing**: File access controls, data validation, audit logging
- **Data Integrity Testing**: OCR accuracy, numeric parsing, approval workflow
- **Cross-Browser Compatibility**: Desktop and mobile responsive design

### Success Criteria
- 95% test automation coverage for critical paths
- 90% OCR data extraction accuracy validation
- Sub-30 second processing time for complete invoice workflow
- Zero data loss during approval/rejection processes
- 99.9% API endpoint reliability

---

## 2. Testing Strategy Overview

### Testing Pyramid Architecture

```
┌─────────────────────────────────┐
│        E2E UI Tests             │  ← 10% (Critical User Journeys)
│     (Playwright/Selenium)       │
├─────────────────────────────────┤
│      Integration Tests          │  ← 20% (API + Database + Services)
│    (TestHost + InMemory DB)     │
├─────────────────────────────────┤
│       Unit Tests                │  ← 70% (Business Logic + Utilities)
│   (xUnit + FluentAssertions)    │
└─────────────────────────────────┘
```

### Test Environment Strategy
1. **Local Development**: Individual developer testing
2. **CI/CD Pipeline**: Automated testing on every commit
3. **Staging Environment**: Azure-deployed testing environment
4. **Production Monitoring**: Real-time validation and alerting

---

## 3. Test Categories and Coverage

### 3.1 Unit Testing (70% of test suite)

#### Core Business Logic Tests
```csharp
// Numeric parsing with various formats
[Theory]
[InlineData("67890", 67890)]
[InlineData("67,890", 67890)]
[InlineData("1,234,567", 1234567)]
[InlineData(" 123,456 ", 123456)]
public void NumericParser_ParsesFormatsCorrectly(string input, int expected)

// GPT-4o response processing
[Fact]
public async Task ProcessInvoiceResponse_ValidJson_ReturnsStructuredData()

// Fallback classification logic
[Theory]
[InlineData("Oil filter replacement", "Part")]
[InlineData("Brake pad installation", "Labor")]
public void FallbackClassifier_ClassifiesCorrectly(string description, string expectedCategory)
```

#### Service Layer Tests
- **InvoiceProcessingService**: End-to-end processing workflow
- **GitHubModelsService**: AI integration and error handling
- **ComprehensiveProcessingService**: Hybrid processing logic
- **InvoiceFallbackService**: Database-driven fallback mechanisms

#### Utility and Helper Tests
- JSON extraction from markdown responses
- Part number validation and extraction
- Field label normalization
- Currency and decimal formatting

### 3.2 Integration Testing (20% of test suite)

#### API Integration Tests
```csharp
[Fact]
public async Task UploadInvoice_ValidPdf_ReturnsProcessedData()

[Fact]
public async Task GetInvoices_ApprovedOnly_ReturnsFilteredResults()

[Fact]
public async Task ApproveInvoice_ValidRequest_UpdatesDatabase()

[Fact]
public async Task RejectInvoice_ValidRequest_DeletesAllData()
```

#### Database Integration Tests
- Entity Framework operations
- Transactional integrity
- Foreign key constraints
- Index performance

#### External Service Integration
- Azure Form Recognizer connectivity
- Azure Blob Storage operations
- GitHub Models API integration
- Rate limiting and retry logic

### 3.3 End-to-End Testing (10% of test suite)

#### Critical User Journey Tests
1. **Complete Invoice Processing Workflow**
2. **Approval and Rejection Workflows**  
3. **File Access and Download Operations**
4. **API Data Retrieval and Filtering**

---

## 9. Detailed Playwright E2E Test Scenarios

Based on the application's pages and functionality, here are comprehensive E2E test scenarios for Playwright implementation:

### 9.1 Core Page Test Scenarios

#### Test Suite: Homepage and Upload Functionality (`/` or `/Home/Index`)

```typescript
// tests/e2e/homepage.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Homepage and Upload Functionality', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display KPI dashboard with statistics', async ({ page }) => {
    // Verify KPI cards are visible
    await expect(page.locator('[data-testid="total-invoices-kpi"]').first()).toBeVisible();
    await expect(page.locator('[data-testid="approved-invoices-kpi"]').first()).toBeVisible();
    await expect(page.locator('[data-testid="pending-invoices-kpi"]').first()).toBeVisible();
    await expect(page.locator('[data-testid="total-value-kpi"]').first()).toBeVisible();
    
    // Verify KPI values are displayed
    await expect(page.locator('.kpi__value').first()).toContainText(/\d+/);
  });

  test('should display upload form with proper elements', async ({ page }) => {
    // Verify upload form elements
    await expect(page.locator('#file')).toBeVisible();
    await expect(page.locator('input[type="file"]')).toHaveAttribute('accept', '.pdf,.png');
    await expect(page.locator('button[type="submit"]')).toContainText('Upload and Process Invoice');
    
    // Verify help text
    await expect(page.locator('.form-text')).toContainText('Supported formats: PDF, PNG');
    await expect(page.locator('.form-text')).toContainText('Maximum size: 10MB');
  });

  test('should validate file selection before upload', async ({ page }) => {
    const uploadBtn = page.locator('button[type="submit"]');
    
    // Click upload without selecting file
    await uploadBtn.click();
    
    // Should prevent form submission
    page.on('dialog', async dialog => {
      expect(dialog.message()).toContain('Please select a file to upload');
      await dialog.accept();
    });
  });

  test('should validate file type and size', async ({ page }) => {
    // Test invalid file type
    const fileInput = page.locator('#file');
    await fileInput.setInputFiles({
      name: 'test.docx',
      mimeType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      buffer: Buffer.from('fake docx content')
    });
    
    const uploadBtn = page.locator('button[type="submit"]');
    await uploadBtn.click();
    
    page.on('dialog', async dialog => {
      expect(dialog.message()).toContain('Only PDF and PNG files are allowed');
      await dialog.accept();
    });
  });

  test('should show processing state during upload', async ({ page }) => {
    // Mock a valid PDF file
    const fileInput = page.locator('#file');
    await fileInput.setInputFiles({
      name: 'test-invoice.pdf',
      mimeType: 'application/pdf',
      buffer: Buffer.from('fake pdf content')
    });
    
    const uploadBtn = page.locator('button[type="submit"]');
    await uploadBtn.click();
    
    // Verify processing state
    await expect(uploadBtn).toBeDisabled();
    await expect(uploadBtn).toContainText('Processing...');
    await expect(page.locator('#uploadProgress')).toBeVisible();
  });

  test('should display how-it-works section', async ({ page }) => {
    // Verify process steps
    await expect(page.locator('h6:has-text("1. Upload")')).toBeVisible();
    await expect(page.locator('h6:has-text("2. Extract")')).toBeVisible();
    await expect(page.locator('h6:has-text("3. Store")')).toBeVisible();
    await expect(page.locator('h6:has-text("4. Access")')).toBeVisible();
    
    // Verify icons are present
    await expect(page.locator('.fa-upload')).toBeVisible();
    await expect(page.locator('.fa-eye')).toBeVisible();
    await expect(page.locator('.fa-database')).toBeVisible();
    await expect(page.locator('.fa-api')).toBeVisible();
  });

  test('should handle successful upload with redirect', async ({ page }) => {
    // Mock successful upload response
    await page.route('**/Home/Upload', async route => {
      await route.fulfill({
        status: 302,
        headers: { 'Location': '/Home/Index?success=true&invoiceId=123' }
      });
    });
    
    const fileInput = page.locator('#file');
    await fileInput.setInputFiles({
      name: 'test-invoice.pdf',
      mimeType: 'application/pdf',
      buffer: Buffer.from('fake pdf content')
    });
    
    await page.locator('button[type="submit"]').click();
    
    // Should show success message
    await expect(page.locator('.alert-success')).toBeVisible();
    await expect(page.locator('.alert-success')).toContainText('Success!');
  });

  test('should handle upload errors gracefully', async ({ page }) => {
    // Mock error response
    await page.route('**/Home/Upload', async route => {
      await route.fulfill({
        status: 400,
        body: JSON.stringify({ error: 'Invalid file format' })
      });
    });
    
    const fileInput = page.locator('#file');
    await fileInput.setInputFiles({
      name: 'invalid.txt',
      mimeType: 'text/plain',
      buffer: Buffer.from('invalid content')
    });
    
    await page.locator('button[type="submit"]').click();
    
    // Should show error message
    await expect(page.locator('.alert-danger')).toBeVisible();
    await expect(page.locator('.alert-danger')).toContainText('Upload Failed');
  });
});
```

#### Test Suite: Invoice List Page (`/Home/Invoices`)

```typescript
// tests/e2e/invoice-list.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Invoice List Functionality', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/Home/Invoices');
  });

  test('should display invoice list with proper headers', async ({ page }) => {
    // Verify table headers
    await expect(page.locator('th:has-text("Invoice #")')).toBeVisible();
    await expect(page.locator('th:has-text("Vehicle ID")')).toBeVisible();
    await expect(page.locator('th:has-text("Date")')).toBeVisible();
    await expect(page.locator('th:has-text("Total Cost")')).toBeVisible();
    await expect(page.locator('th:has-text("Confidence")')).toBeVisible();
    await expect(page.locator('th:has-text("Line Items")')).toBeVisible();
    await expect(page.locator('th:has-text("Approval Status")')).toBeVisible();
    await expect(page.locator('th:has-text("Actions")')).toBeVisible();
  });

  test('should display invoices with proper formatting', async ({ page }) => {
    // Wait for invoice data to load
    await page.waitForSelector('tbody tr', { timeout: 10000 });
    
    const firstRow = page.locator('tbody tr').first();
    
    // Verify invoice number format
    await expect(firstRow.locator('td:nth-child(1) strong')).toBeVisible();
    
    // Verify vehicle ID badge
    await expect(firstRow.locator('.badge.bg-secondary')).toBeVisible();
    
    // Verify currency formatting
    await expect(firstRow.locator('.text-success')).toContainText(/\$\d+\.\d{2}/);
    
    // Verify confidence badge
    const confidenceBadge = firstRow.locator('.badge:has-text("%")');
    if (await confidenceBadge.count() > 0) {
      await expect(confidenceBadge).toContainText(/%$/);
    }
  });

  test('should handle approval status display correctly', async ({ page }) => {
    await page.waitForSelector('tbody tr', { timeout: 10000 });
    
    // Check for approved status
    const approvedBadge = page.locator('.badge.bg-success:has-text("Approved")').first();
    if (await approvedBadge.count() > 0) {
      await expect(approvedBadge).toContainText('Approved');
    }
    
    // Check for pending status
    const pendingBadge = page.locator('.badge.bg-warning:has-text("Pending")').first();
    if (await pendingBadge.count() > 0) {
      await expect(pendingBadge).toContainText('Pending');
    }
  });

  test('should provide action buttons for each invoice', async ({ page }) => {
    await page.waitForSelector('tbody tr', { timeout: 10000 });
    
    const firstRow = page.locator('tbody tr').first();
    const actionGroup = firstRow.locator('.btn-group');
    
    // Verify view details button
    await expect(actionGroup.locator('[title="View Details"]')).toBeVisible();
    await expect(actionGroup.locator('.fa-eye')).toBeVisible();
    
    // Verify view original file button
    await expect(actionGroup.locator('[title="View Original File"]')).toBeVisible();
    await expect(actionGroup.locator('.fa-file-alt')).toBeVisible();
  });

  test('should handle pagination when multiple pages exist', async ({ page }) => {
    // Check if pagination exists
    const pagination = page.locator('.pagination');
    
    if (await pagination.count() > 0) {
      await expect(pagination).toBeVisible();
      
      // Verify pagination controls
      await expect(page.locator('.page-link:has-text("Previous")')).toBeVisible();
      await expect(page.locator('.page-link:has-text("Next")')).toBeVisible();
      
      // Test pagination click (if next is enabled)
      const nextButton = page.locator('.page-item:not(.disabled) .page-link:has-text("Next")');
      if (await nextButton.count() > 0) {
        await nextButton.click();
        await expect(page).toHaveURL(/page=2/);
      }
    }
  });

  test('should display pagination summary', async ({ page }) => {
    await page.waitForSelector('.text-muted', { timeout: 5000 });
    
    const paginationSummary = page.locator('.text-muted:has-text("Showing")');
    if (await paginationSummary.count() > 0) {
      await expect(paginationSummary).toContainText(/Showing \d+ to \d+ of \d+ invoices/);
    }
  });

  test('should provide API access links', async ({ page }) => {
    // Verify API endpoint button
    await expect(page.locator('a[href="/api/invoices"]')).toBeVisible();
    await expect(page.locator('a[href="/api/invoices"]')).toContainText('API Endpoint');
    
    // Verify Swagger docs button
    await expect(page.locator('a[href="/swagger"]')).toBeVisible();
    await expect(page.locator('a[href="/swagger"]')).toContainText('API Docs');
  });

  test('should handle empty state gracefully', async ({ page }) => {
    // Mock empty response
    await page.route('**/api/invoices*', async route => {
      await route.fulfill({
        status: 200,
        body: JSON.stringify({
          items: [],
          totalCount: 0,
          pageNumber: 1,
          pageSize: 20,
          totalPages: 0
        })
      });
    });
    
    await page.reload();
    
    // Verify empty state
    await expect(page.locator('h5:has-text("No invoices found")')).toBeVisible();
    await expect(page.locator('.fa-inbox')).toBeVisible();
    await expect(page.locator('a:has-text("Upload Invoice")')).toBeVisible();
  });

  test('should navigate to details page when view button is clicked', async ({ page }) => {
    await page.waitForSelector('tbody tr', { timeout: 10000 });
    
    const firstViewButton = page.locator('[title="View Details"]').first();
    await firstViewButton.click();
    
    // Should navigate to details page
    await expect(page).toHaveURL(/\/Home\/Details\/\d+/);
  });

  test('should handle upload new invoice navigation', async ({ page }) => {
    const uploadButton = page.locator('a:has-text("Upload New Invoice")');
    await expect(uploadButton).toBeVisible();
    
    await uploadButton.click();
    await expect(page).toHaveURL('/');
  });
});
```

#### Test Suite: Invoice Details Page (`/Home/Details/{id}`)

```typescript
// tests/e2e/invoice-details.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Invoice Details Functionality', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to a test invoice details page
    await page.goto('/Home/Details/1'); // Assuming invoice ID 1 exists
  });

  test('should display invoice header information', async ({ page }) => {
    // Verify header card is visible
    await expect(page.locator('.card-header:has-text("Invoice Information")')).toBeVisible();
    
    // Verify invoice details table
    await expect(page.locator('td:has-text("Invoice Number:")')).toBeVisible();
    await expect(page.locator('td:has-text("Vehicle ID:")')).toBeVisible();
    await expect(page.locator('td:has-text("Invoice Date:")')).toBeVisible();
    await expect(page.locator('td:has-text("Total Cost:")')).toBeVisible();
    
    // Verify vehicle ID badge
    await expect(page.locator('.badge.bg-secondary')).toBeVisible();
    
    // Verify total cost formatting
    await expect(page.locator('h5.text-success')).toContainText(/\$\d+\.\d{2}/);
  });

  test('should display approval status correctly', async ({ page }) => {
    // Check for approval status badge
    const approvedBadge = page.locator('.badge.bg-success:has-text("Approved")');
    const pendingBadge = page.locator('.badge.bg-warning:has-text("Pending Approval")');
    
    // Either approved or pending should be visible
    const hasApproved = await approvedBadge.count() > 0;
    const hasPending = await pendingBadge.count() > 0;
    
    expect(hasApproved || hasPending).toBe(true);
  });

  test('should show approval controls for unapproved invoices', async ({ page }) => {
    // If invoice is not approved, should show approval buttons
    const pendingBadge = page.locator('.badge.bg-warning:has-text("Pending")');
    
    if (await pendingBadge.count() > 0) {
      await expect(page.locator('button:has-text("Approve")')).toBeVisible();
      await expect(page.locator('button:has-text("Reject")')).toBeVisible();
    }
  });

  test('should handle invoice approval workflow', async ({ page }) => {
    const approveButton = page.locator('button:has-text("Approve")');
    
    if (await approveButton.count() > 0) {
      // Mock approval API
      await page.route('**/api/invoices/*/approve', async route => {
        await route.fulfill({
          status: 200,
          body: JSON.stringify({ success: true, message: 'Invoice approved successfully' })
        });
      });
      
      // Handle prompts and confirmations
      page.on('dialog', async dialog => {
        if (dialog.type() === 'prompt') {
          await dialog.accept('Test User');
        } else if (dialog.type() === 'confirm') {
          await dialog.accept();
        } else if (dialog.type() === 'alert') {
          expect(dialog.message()).toContain('Success');
          await dialog.accept();
        }
      });
      
      await approveButton.click();
    }
  });

  test('should handle invoice rejection workflow', async ({ page }) => {
    const rejectButton = page.locator('button:has-text("Reject")');
    
    if (await rejectButton.count() > 0) {
      // Mock rejection API
      await page.route('**/api/invoices/*/reject', async route => {
        await route.fulfill({
          status: 200,
          body: JSON.stringify({ success: true, message: 'Invoice rejected and deleted' })
        });
      });
      
      // Handle multiple confirmations
      let confirmCount = 0;
      page.on('dialog', async dialog => {
        if (dialog.type() === 'confirm') {
          confirmCount++;
          await dialog.accept();
        } else if (dialog.type() === 'alert') {
          expect(dialog.message()).toContain('Success');
          await dialog.accept();
        }
      });
      
      await rejectButton.click();
      
      // Should redirect after rejection
      await page.waitForURL('/Home/Invoices');
    }
  });

  test('should display line items table with proper formatting', async ({ page }) => {
    // Verify line items section
    await expect(page.locator('.card-header:has-text("Line Items")')).toBeVisible();
    
    // Verify table headers
    await expect(page.locator('th:has-text("#")')).toBeVisible();
    await expect(page.locator('th:has-text("Description")')).toBeVisible();
    await expect(page.locator('th:has-text("Category")')).toBeVisible();
    await expect(page.locator('th:has-text("Unit Cost")')).toBeVisible();
    await expect(page.locator('th:has-text("Qty")')).toBeVisible();
    await expect(page.locator('th:has-text("Line Total")')).toBeVisible();
    await expect(page.locator('th:has-text("Part Number")')).toBeVisible();
    await expect(page.locator('th:has-text("Confidence")')).toBeVisible();
  });

  test('should display line item categories with proper badges', async ({ page }) => {
    const categoryBadges = page.locator('tbody .badge');
    
    if (await categoryBadges.count() > 0) {
      const firstBadge = categoryBadges.first();
      
      // Should have appropriate category text
      const badgeText = await firstBadge.textContent();
      expect(['Part', 'Parts', 'Labor', 'Tax', 'Fee', 'Service'].some(cat => 
        badgeText?.includes(cat)
      )).toBe(true);
    }
  });

  test('should display part numbers for parts categories', async ({ page }) => {
    // Check for part number codes
    const partNumberCodes = page.locator('tbody code');
    
    if (await partNumberCodes.count() > 0) {
      // Part numbers should be in code format
      await expect(partNumberCodes.first()).toBeVisible();
    }
  });

  test('should show confidence scores with proper badges', async ({ page }) => {
    const confidenceBadges = page.locator('tbody .badge:has-text("%")');
    
    if (await confidenceBadges.count() > 0) {
      const firstConfidenceBadge = confidenceBadges.first();
      await expect(firstConfidenceBadge).toContainText(/%$/);
      
      // Badge should have appropriate color class
      const badgeClasses = await firstConfidenceBadge.getAttribute('class');
      expect(badgeClasses).toMatch(/(bg-success|bg-warning|bg-danger)/);
    }
  });

  test('should display summary cards with totals', async ({ page }) => {
    // Verify summary cards
    await expect(page.locator('h6:has-text("Parts")')).toBeVisible();
    await expect(page.locator('h6:has-text("Labor")')).toBeVisible();
    await expect(page.locator('h6:has-text("Tax & Fees")')).toBeVisible();
    await expect(page.locator('h6:has-text("Total Items")')).toBeVisible();
    
    // Verify monetary values
    const partsTotal = page.locator('h5.text-primary');
    const laborTotal = page.locator('h5.text-warning');
    const taxFeesTotal = page.locator('h5.text-info');
    
    if (await partsTotal.count() > 0) {
      await expect(partsTotal).toContainText(/\$\d+\.\d{2}/);
    }
  });

  test('should provide view original file functionality', async ({ page }) => {
    const viewOriginalButton = page.locator('a:has-text("View Original")');
    await expect(viewOriginalButton).toBeVisible();
    await expect(viewOriginalButton).toHaveAttribute('target', '_blank');
    
    // Should link to file API endpoint
    const href = await viewOriginalButton.getAttribute('href');
    expect(href).toMatch(/\/api\/invoices\/\d+\/file/);
  });

  test('should display API access information', async ({ page }) => {
    // Verify API access section
    await expect(page.locator('h5:has-text("API Access")')).toBeVisible();
    
    // Verify API endpoint links
    await expect(page.locator('code:has-text("GET /api/invoices/")')).toBeVisible();
    await expect(page.locator('code:has-text("GET /api/invoices/") + code:has-text("/file")')).toBeVisible();
    
    // Verify try API buttons
    await expect(page.locator('a:has-text("Try API")')).toBeVisible();
    await expect(page.locator('a:has-text("Access File")')).toBeVisible();
  });

  test('should handle navigation back to list', async ({ page }) => {
    const backButton = page.locator('a:has-text("Back to List")');
    await expect(backButton).toBeVisible();
    
    await backButton.click();
    await expect(page).toHaveURL('/Home/Invoices');
  });

  test('should handle error states gracefully', async ({ page }) => {
    // Navigate to non-existent invoice
    await page.goto('/Home/Details/99999');
    
    // Should show error message
    await expect(page.locator('.alert-danger')).toBeVisible();
    await expect(page.locator('h4:has-text("Error Loading Invoice Details")')).toBeVisible();
    
    // Should provide navigation options
    await expect(page.locator('a:has-text("← Back to Home")')).toBeVisible();
    await expect(page.locator('a:has-text("View All Invoices")')).toBeVisible();
  });

  test('should display odometer reading when available', async ({ page }) => {
    const odometerRow = page.locator('td:has-text("Odometer:")');
    
    if (await odometerRow.count() > 0) {
      const odometerValue = odometerRow.locator('+ td');
      await expect(odometerValue).toContainText(/\d+.*miles/);
    }
  });

  test('should display AI-generated description when available', async ({ page }) => {
    const descriptionRow = page.locator('td:has-text("Description:")');
    
    if (await descriptionRow.count() > 0) {
      const descriptionValue = descriptionRow.locator('+ td .text-muted');
      await expect(descriptionValue).toBeVisible();
      await expect(descriptionValue.locator('.fa-robot')).toBeVisible();
    }
  });
});
```

#### Test Suite: Navigation and Layout (`Shared/_Layout.cshtml`)

```typescript
// tests/e2e/navigation.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Navigation and Layout', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display header with branding and navigation', async ({ page }) => {
    // Verify header navbar
    await expect(page.locator('.navbar.bg-primary')).toBeVisible();
    
    // Verify brand
    await expect(page.locator('.navbar-brand')).toContainText('Maintenance Plus');
    await expect(page.locator('.navbar-brand .fa-wrench')).toBeVisible();
    
    // Verify navigation links
    await expect(page.locator('.nav-link[href*="Upload"]')).toBeVisible();
    await expect(page.locator('.nav-link[href*="Invoices"]')).toBeVisible();
    await expect(page.locator('.nav-link[href="/swagger"]')).toBeVisible();
  });

  test('should navigate between main sections', async ({ page }) => {
    // Test Upload navigation
    await page.locator('.nav-link:has-text("Upload")').click();
    await expect(page).toHaveURL('/');
    
    // Test Invoices navigation
    await page.locator('.nav-link:has-text("Invoices")').click();
    await expect(page).toHaveURL('/Home/Invoices');
    
    // Test API Docs navigation
    const apiDocsLink = page.locator('.nav-link[href="/swagger"]');
    await expect(apiDocsLink).toHaveAttribute('href', '/swagger');
  });

  test('should display footer', async ({ page }) => {
    await expect(page.locator('.footer')).toBeVisible();
    await expect(page.locator('.footer')).toContainText('© 2025 - Maintenance Plus');
  });

  test('should have responsive design', async ({ page }) => {
    // Test mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    
    // Navigation should still be functional
    await expect(page.locator('.navbar')).toBeVisible();
    await expect(page.locator('.navbar-brand')).toBeVisible();
    
    // Test tablet viewport
    await page.setViewportSize({ width: 768, height: 1024 });
    await expect(page.locator('.navbar')).toBeVisible();
    
    // Test desktop viewport
    await page.setViewportSize({ width: 1920, height: 1080 });
    await expect(page.locator('.navbar')).toBeVisible();
  });

  test('should handle brand logo click navigation', async ({ page }) => {
    // Navigate away from home
    await page.goto('/Home/Invoices');
    
    // Click brand to return home
    await page.locator('.navbar-brand').click();
    await expect(page).toHaveURL('/');
  });
});
```

### 9.2 Advanced E2E Test Scenarios

#### Test Suite: Complete User Workflows

```typescript
// tests/e2e/complete-workflows.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Complete User Workflows', () => {
  test('should complete full invoice processing workflow', async ({ page }) => {
    // Step 1: Upload invoice
    await page.goto('/');
    
    const fileInput = page.locator('#file');
    await fileInput.setInputFiles({
      name: 'test-invoice.pdf',
      mimeType: 'application/pdf',
      buffer: Buffer.from('fake pdf content')
    });
    
    // Mock successful upload
    await page.route('**/Home/Upload', async route => {
      await route.fulfill({
        status: 302,
        headers: { 'Location': '/Home/Index?success=true&invoiceId=123' }
      });
    });
    
    await page.locator('button[type="submit"]').click();
    
    // Step 2: Verify success and navigate to details
    await expect(page.locator('.alert-success')).toBeVisible();
    const viewDetailsButton = page.locator('a:has-text("View Invoice Details")');
    await viewDetailsButton.click();
    
    // Step 3: Verify details page
    await expect(page).toHaveURL('/Home/Details/123');
    await expect(page.locator('h2:has-text("Invoice Details")')).toBeVisible();
    
    // Step 4: Approve invoice (if pending)
    const approveButton = page.locator('button:has-text("Approve")');
    if (await approveButton.count() > 0) {
      // Mock approval
      await page.route('**/api/invoices/123/approve', async route => {
        await route.fulfill({
          status: 200,
          body: JSON.stringify({ success: true, message: 'Approved' })
        });
      });
      
      page.on('dialog', async dialog => {
        if (dialog.type() === 'prompt') await dialog.accept('Test User');
        if (dialog.type() === 'confirm') await dialog.accept();
        if (dialog.type() === 'alert') await dialog.accept();
      });
      
      await approveButton.click();
      await page.waitForLoadState();
    }
    
    // Step 5: Navigate to invoice list and verify
    await page.locator('a:has-text("Back to List")').click();
    await expect(page).toHaveURL('/Home/Invoices');
    await expect(page.locator('tbody tr')).toHaveCountGreaterThan(0);
  });

  test('should handle complete rejection workflow', async ({ page }) => {
    // Navigate to invoice details
    await page.goto('/Home/Details/1');
    
    const rejectButton = page.locator('button:has-text("Reject")');
    
    if (await rejectButton.count() > 0) {
      // Mock rejection API
      await page.route('**/api/invoices/1/reject', async route => {
        await route.fulfill({
          status: 200,
          body: JSON.stringify({ success: true, message: 'Invoice deleted' })
        });
      });
      
      // Handle confirmations
      let dialogCount = 0;
      page.on('dialog', async dialog => {
        dialogCount++;
        if (dialog.type() === 'confirm') {
          await dialog.accept();
        } else if (dialog.type() === 'alert') {
          expect(dialog.message()).toContain('Success');
          await dialog.accept();
        }
      });
      
      await rejectButton.click();
      
      // Should redirect to invoices list
      await page.waitForURL('/Home/Invoices');
      
      // Verify we had multiple confirmations
      expect(dialogCount).toBeGreaterThan(1);
    }
  });

  test('should handle file access workflow', async ({ page }) => {
    // Navigate to invoice details
    await page.goto('/Home/Details/1');
    
    // Click view original file
    const viewFileButton = page.locator('a:has-text("View Original")');
    await expect(viewFileButton).toBeVisible();
    
    // Mock file access response
    await page.route('**/api/invoices/1/file', async route => {
      await route.fulfill({
        status: 302,
        headers: { 'Location': 'https://storage.example.com/file123.pdf' }
      });
    });
    
    // Click should open in new tab
    const [newPage] = await Promise.all([
      page.context().waitForEvent('page'),
      viewFileButton.click()
    ]);
    
    await newPage.waitForLoadState();
    expect(newPage.url()).toContain('storage.example.com');
    
    await newPage.close();
  });
});
```

### 9.3 Error Handling and Edge Cases

```typescript
// tests/e2e/error-handling.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Error Handling and Edge Cases', () => {
  test('should handle network errors gracefully', async ({ page }) => {
    // Simulate network failure
    await page.route('**/api/**', async route => {
      await route.abort('failed');
    });
    
    await page.goto('/Home/Invoices');
    
    // Should show appropriate error message
    await expect(page.locator('.alert-danger')).toBeVisible();
  });

  test('should handle 404 errors for non-existent invoices', async ({ page }) => {
    await page.goto('/Home/Details/99999');
    
    await expect(page.locator('.alert-danger')).toBeVisible();
    await expect(page.locator('h4:has-text("Error Loading Invoice Details")')).toBeVisible();
  });

  test('should handle server errors during upload', async ({ page }) => {
    await page.goto('/');
    
    // Mock server error
    await page.route('**/Home/Upload', async route => {
      await route.fulfill({
        status: 500,
        body: 'Internal Server Error'
      });
    });
    
    const fileInput = page.locator('#file');
    await fileInput.setInputFiles({
      name: 'test-invoice.pdf',
      mimeType: 'application/pdf',
      buffer: Buffer.from('fake pdf content')
    });
    
    await page.locator('button[type="submit"]').click();
    
    await expect(page.locator('.alert-danger')).toBeVisible();
  });

  test('should handle timeout scenarios', async ({ page }) => {
    // Set aggressive timeout
    page.setDefaultTimeout(1000);
    
    // Mock slow response
    await page.route('**/api/invoices', async route => {
      await new Promise(resolve => setTimeout(resolve, 2000));
      await route.continue();
    });
    
    try {
      await page.goto('/Home/Invoices');
      // Should handle timeout gracefully
    } catch (error) {
      expect(error.message).toContain('timeout');
    }
  });

  test('should validate required fields', async ({ page }) => {
    await page.goto('/');
    
    // Try to submit form without file
    await page.locator('button[type="submit"]').click();
    
    // Should show validation message
    page.on('dialog', async dialog => {
      expect(dialog.message()).toContain('Please select a file');
      await dialog.accept();
    });
  });
});
```

### 9.4 API Integration Tests

```typescript
// tests/e2e/api-integration.spec.ts
import { test, expect } from '@playwright/test';

test.describe('API Integration Tests', () => {
  test('should access API endpoints from UI', async ({ page }) => {
    await page.goto('/Home/Invoices');
    
    // Click API endpoint link
    const apiButton = page.locator('a[href="/api/invoices"]');
    await expect(apiButton).toBeVisible();
    
    const [apiPage] = await Promise.all([
      page.context().waitForEvent('page'),
      apiButton.click()
    ]);
    
    // Should open API endpoint in new tab
    await apiPage.waitForLoadState();
    expect(apiPage.url()).toContain('/api/invoices');
    
    await apiPage.close();
  });

  test('should access Swagger documentation', async ({ page }) => {
    await page.goto('/');
    
    // Click API docs link
    await page.locator('.nav-link[href="/swagger"]').click();
    
    // Should navigate to Swagger UI
    await page.waitForURL('/swagger');
    await expect(page.locator('h2:has-text("swagger")')).toBeVisible();
  });

  test('should handle API rate limiting gracefully', async ({ page }) => {
    // Mock rate limit response
    await page.route('**/api/**', async route => {
      await route.fulfill({
        status: 429,
        body: JSON.stringify({ error: 'Rate limit exceeded' })
      });
    });
    
    await page.goto('/Home/Invoices');
    
    // Should show appropriate error message
    await expect(page.locator('.alert-danger')).toBeVisible();
  });
});
```

### 9.5 Performance and Load Testing

```typescript
// tests/e2e/performance.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Performance Tests', () => {
  test('should load homepage within acceptable time', async ({ page }) => {
    const startTime = Date.now();
    
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    const loadTime = Date.now() - startTime;
    expect(loadTime).toBeLessThan(5000); // 5 seconds
  });

  test('should handle large invoice lists efficiently', async ({ page }) => {
    // Mock large dataset
    const mockData = {
      items: Array.from({ length: 100 }, (_, i) => ({
        invoiceID: i + 1,
        invoiceNumber: `INV-${i + 1:04d}`,
        vehicleID: `VEH-${i + 1:03d}`,
        totalCost: 100 + i,
        approved: i % 2 === 0
      })),
      totalCount: 100,
      pageSize: 20,
      pageNumber: 1,
      totalPages: 5
    };
    
    await page.route('**/api/invoices*', async route => {
      await route.fulfill({
        status: 200,
        body: JSON.stringify(mockData)
      });
    });
    
    const startTime = Date.now();
    await page.goto('/Home/Invoices');
    await page.waitForSelector('tbody tr');
    
    const renderTime = Date.now() - startTime;
    expect(renderTime).toBeLessThan(3000); // 3 seconds
    
    // Verify all items rendered
    await expect(page.locator('tbody tr')).toHaveCount(20);
  });

  test('should handle file upload progress efficiently', async ({ page }) => {
    await page.goto('/');
    
    const fileInput = page.locator('#file');
    await fileInput.setInputFiles({
      name: 'large-invoice.pdf',
      mimeType: 'application/pdf',
      buffer: Buffer.alloc(5 * 1024 * 1024) // 5MB file
    });
    
    const uploadBtn = page.locator('button[type="submit"]');
    const startTime = Date.now();
    
    await uploadBtn.click();
    
    // Verify progress indicator appears quickly
    await expect(page.locator('#uploadProgress')).toBeVisible();
    
    const progressTime = Date.now() - startTime;
    expect(progressTime).toBeLessThan(1000); // 1 second to show progress
  });
});

---

## 4. Detailed Test Plans

### 4.1 Invoice Upload and Processing Test Plan

#### Test Scenarios
| Scenario | Input | Expected Output | Priority |
|----------|-------|-----------------|----------|
| Valid PDF Upload | Standard invoice PDF | Successful processing, data extracted | P0 |
| Valid PNG Upload | Invoice image file | Successful OCR, data stored | P0 |
| Oversized File | 11MB PDF file | Rejection with error message | P1 |
| Invalid Format | .docx file | Format validation error | P1 |
| Corrupted File | Broken PDF | Graceful error handling | P2 |

#### Automation Strategy
```typescript
// Playwright E2E test example
test('Complete invoice upload workflow', async ({ page }) => {
  await page.goto('/');
  await page.setInputFiles('[data-testid=file-input]', 'test-invoice.pdf');
  await page.click('[data-testid=upload-button]');
  
  // Verify processing indicators
  await expect(page.locator('[data-testid=processing-status]')).toBeVisible();
  
  // Wait for completion and verify results
  await page.waitForSelector('[data-testid=success-message]', { timeout: 30000 });
  const extractedData = await page.locator('[data-testid=extracted-data]').textContent();
  expect(extractedData).toContain('Invoice Number:');
});
```

### 4.2 AI Processing and Classification Test Plan

#### GPT-4o Integration Tests
```csharp
[Fact]
public async Task GPT4o_ProcessInvoice_ReturnsStructuredOutput()
{
    // Arrange
    var mockResponse = CreateMockGPTResponse();
    _httpMessageHandler.SetupResponse(mockResponse);
    
    // Act
    var result = await _gitHubModelsService.ProcessInvoiceAsync(invoiceData);
    
    // Assert
    result.Success.Should().BeTrue();
    result.VehicleId.Should().NotBeNullOrEmpty();
    result.LineItems.Should().HaveCountGreaterThan(0);
    result.Description.Should().NotBeNullOrEmpty();
}

[Fact]
public async Task GPT4o_RateLimit_FallsBackGracefully()
{
    // Arrange - Mock 429 rate limit response
    _httpMessageHandler.SetupRateLimitResponse();
    
    // Act
    var result = await _comprehensiveProcessingService.ProcessAsync(formRecognizerData);
    
    // Assert
    result.ProcessingMethod.Should().Be("Fallback-Database");
    result.Success.Should().BeTrue();
}
```

#### Classification Accuracy Tests
- Test part vs labor classification with known examples
- Validate confidence score calculations
- Test edge cases and ambiguous descriptions
- Verify fallback keyword matching

### 4.3 Approval Workflow Test Plan

#### Approval Process Tests
```csharp
[Fact]
public async Task ApproveInvoice_UpdatesStatusAndTimestamp()
{
    // Arrange
    var invoice = await CreateTestInvoiceAsync(approved: false);
    
    // Act
    var result = await _controller.ApproveInvoice(invoice.InvoiceID);
    
    // Assert
    result.Should().BeOfType<OkResult>();
    
    var updated = await _context.InvoiceHeaders.FindAsync(invoice.InvoiceID);
    updated.Approved.Should().BeTrue();
    updated.ApprovedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
}
```

#### UI Approval Tests
```typescript
test('Invoice approval workflow', async ({ page }) => {
  // Navigate to unapproved invoice
  await page.goto('/Invoice/Details/123');
  
  // Verify approval button is visible
  await expect(page.locator('[data-testid=approve-button]')).toBeVisible();
  
  // Click approve and handle confirmation dialog
  await page.click('[data-testid=approve-button]');
  await expect(page.locator('[data-testid=confirmation-dialog]')).toBeVisible();
  await page.click('[data-testid=confirm-approve]');
  
  // Verify success state
  await expect(page.locator('[data-testid=approved-badge]')).toBeVisible();
  await expect(page.locator('[data-testid=approve-button]')).not.toBeVisible();
});
```

### 4.4 Data Integrity and Validation Test Plan

#### Numeric Data Extraction Tests
```csharp
[Theory]
[InlineData("67,890", 67890)]
[InlineData("1,234,567", 1234567)]
[InlineData("123456", 123456)]
[InlineData(" 67,890 ", 67890)]
public void OdometerExtraction_HandlesVariousFormats(string input, int expected)
{
    var result = NumericExtractor.ParseOdometer(input);
    result.Should().Be(expected);
}

[Fact]
public void CurrencyExtraction_HandlesCurrencyFormats()
{
    var testCases = new[]
    {
        ("$1,234.56", 1234.56m),
        ("1,234.56", 1234.56m),
        ("$123.45", 123.45m),
        ("45.00", 45.00m)
    };
    
    foreach (var (input, expected) in testCases)
    {
        var result = NumericExtractor.ParseCurrency(input);
        result.Should().Be(expected);
    }
}
```

#### Database Integrity Tests
```csharp
[Fact]
public async Task RejectInvoice_DeletesAllRelatedData()
{
    // Arrange
    var invoice = await CreateCompleteTestInvoiceAsync();
    var originalBlobUrl = invoice.BlobFileUrl;
    
    // Act
    await _controller.RejectInvoice(invoice.InvoiceID);
    
    // Assert
    var deletedInvoice = await _context.InvoiceHeaders.FindAsync(invoice.InvoiceID);
    deletedInvoice.Should().BeNull();
    
    var relatedLines = await _context.InvoiceLines
        .Where(l => l.InvoiceID == invoice.InvoiceID)
        .ToListAsync();
    relatedLines.Should().BeEmpty();
    
    // Verify blob deletion (mock blob service)
    _mockBlobService.Verify(x => x.DeleteAsync(originalBlobUrl), Times.Once);
}
```

### 4.5 API Security and Access Control Test Plan

#### Approval Visibility Tests
```csharp
[Fact]
public async Task GetInvoices_OnlyReturnsApprovedInvoices()
{
    // Arrange
    await CreateTestInvoiceAsync(approved: true, invoiceNumber: "APPROVED-001");
    await CreateTestInvoiceAsync(approved: false, invoiceNumber: "PENDING-001");
    
    // Act
    var result = await _controller.GetInvoices();
    
    // Assert
    var invoices = result.Value;
    invoices.Should().HaveCount(1);
    invoices.First().InvoiceNumber.Should().Be("APPROVED-001");
}

[Fact]
public async Task GetInvoiceById_ReturnsNotFoundForUnapproved()
{
    // Arrange
    var unapprovedInvoice = await CreateTestInvoiceAsync(approved: false);
    
    // Act
    var result = await _controller.GetInvoiceById(unapprovedInvoice.InvoiceID);
    
    // Assert
    result.Result.Should().BeOfType<NotFoundResult>();
}
```

#### File Access Security Tests
```csharp
[Fact]
public async Task GetInvoiceFile_ReturnsNotFoundForUnapproved()
{
    var unapprovedInvoice = await CreateTestInvoiceAsync(approved: false);
    
    var result = await _controller.GetInvoiceFile(unapprovedInvoice.InvoiceID);
    
    result.Should().BeOfType<NotFoundResult>();
}

[Fact]
public async Task GetInvoiceFile_GeneratesSecureUrlWithExpiration()
{
    var approvedInvoice = await CreateTestInvoiceAsync(approved: true);
    
    var result = await _controller.GetInvoiceFile(approvedInvoice.InvoiceID);
    
    result.Should().BeOfType<RedirectResult>();
    var redirect = result as RedirectResult;
    redirect.Url.Should().Contain("sig="); // SAS signature
    redirect.Url.Should().Contain("se="); // Expiration
}
```

---

## 5. Performance Testing Plan

### 5.1 Load Testing Scenarios

#### Concurrent Upload Testing
```csharp
[Fact]
public async Task ConcurrentUploads_HandlesMultipleUsers()
{
    const int ConcurrentUsers = 50;
    const int InvoicesPerUser = 10;
    
    var tasks = Enumerable.Range(0, ConcurrentUsers)
        .Select(async userId =>
        {
            var client = _factory.CreateClient();
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < InvoicesPerUser; i++)
            {
                var response = await UploadTestInvoiceAsync(client, $"user{userId}-invoice{i}.pdf");
                response.EnsureSuccessStatusCode();
            }
            
            return stopwatch.Elapsed;
        });
    
    var results = await Task.WhenAll(tasks);
    
    // Assert performance requirements
    results.Average(t => t.TotalSeconds).Should().BeLessThan(30);
    results.Max(t => t.TotalSeconds).Should().BeLessThan(60);
}
```

#### API Response Time Testing
```csharp
[Theory]
[InlineData("/api/invoices", 2.0)] // 2 second max
[InlineData("/api/invoices/1", 1.0)] // 1 second max
[InlineData("/api/invoices/date/2025-08-28", 3.0)] // 3 second max
public async Task ApiEndpoints_MeetPerformanceTargets(string endpoint, double maxResponseTimeSeconds)
{
    var stopwatch = Stopwatch.StartNew();
    
    var response = await _client.GetAsync(endpoint);
    
    stopwatch.Stop();
    response.EnsureSuccessStatusCode();
    stopwatch.Elapsed.TotalSeconds.Should().BeLessThan(maxResponseTimeSeconds);
}
```

### 5.2 Processing Performance Tests

#### AI Processing Performance
```csharp
[Fact]
public async Task GPT4oProcessing_CompletesWithinTimeLimit()
{
    var largeInvoiceData = CreateLargeTestInvoice(); // 20+ line items
    var stopwatch = Stopwatch.StartNew();
    
    var result = await _gitHubModelsService.ProcessInvoiceAsync(largeInvoiceData);
    
    stopwatch.Stop();
    result.Success.Should().BeTrue();
    stopwatch.Elapsed.TotalSeconds.Should().BeLessThan(15);
}

[Fact]
public async Task EndToEndProcessing_CompletesWithinSLA()
{
    var testFile = CreateTestPdfFile();
    var stopwatch = Stopwatch.StartNew();
    
    var result = await _invoiceProcessingService.ProcessInvoiceAsync(testFile);
    
    stopwatch.Stop();
    result.IsSuccess.Should().BeTrue();
    stopwatch.Elapsed.TotalSeconds.Should().BeLessThan(30);
}
```

---

## 6. Test Data Management

### 6.1 Test Data Strategy

#### Sample Invoice Repository
```
TestInvoices/
├── Valid/
│   ├── Honda_OilChange_VEH001.pdf
│   ├── Ford_BrakeService_VEH002.pdf
│   ├── Toyota_Maintenance_VEH003.png
│   └── Chevy_ComplexRepair_VEH004.pdf
├── EdgeCases/
│   ├── CommaNumbers_VEH005.pdf
│   ├── MultiPage_VEH006.pdf
│   ├── PoorQuality_VEH007.png
│   └── NonStandardFormat_VEH008.pdf
├── Invalid/
│   ├── Corrupted.pdf
│   ├── WrongFormat.docx
│   ├── Oversized.pdf (11MB)
│   └── Empty.pdf
└── Performance/
    ├── Large_50LineItems.pdf
    ├── HighResolution.png
    └── Batch_Processing_Set/ (100 files)
```

#### Test Data Generation
```csharp
public static class TestDataFactory
{
    public static InvoiceHeader CreateTestInvoice(
        bool approved = false,
        string vehicleId = "TEST-VEH-001",
        decimal totalCost = 125.50m,
        int lineItemCount = 3)
    {
        return new InvoiceHeader
        {
            VehicleID = vehicleId,
            InvoiceNumber = $"TEST-INV-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7)),
            TotalCost = totalCost,
            Approved = approved,
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            InvoiceLines = GenerateTestLineItems(lineItemCount)
        };
    }
    
    public static List<InvoiceLine> GenerateTestLineItems(int count)
    {
        var items = new List<InvoiceLine>();
        for (int i = 1; i <= count; i++)
        {
            items.Add(new InvoiceLine
            {
                LineNumber = i,
                Description = $"Test Item {i}",
                UnitCost = 25.00m * i,
                Quantity = 1,
                TotalLineCost = 25.00m * i,
                ClassifiedCategory = i % 2 == 0 ? "Part" : "Labor",
                ClassificationConfidence = 85.5m,
                ClassificationMethod = "Test-Generated"
            });
        }
        return items;
    }
}
```

### 6.2 Database Seeding for Tests

#### Test Database Setup
```csharp
public class TestDbContext : IDisposable
{
    public InvoiceDbContext Context { get; }
    
    public TestDbContext()
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        Context = new InvoiceDbContext(options);
        Context.Database.EnsureCreated();
        
        // Seed test data
        SeedTestData();
    }
    
    private void SeedTestData()
    {
        // Add field mappings for fallback testing
        Context.InvoiceFields.AddRange(new[]
        {
            new InvoiceField { TargetFieldName = "VehicleID", ExpectedValue = "Unit #", MatchType = "EXACT" },
            new InvoiceField { TargetFieldName = "InvoiceNumber", ExpectedValue = "RO", MatchType = "EXACT" },
            new InvoiceField { TargetFieldName = "Odometer", ExpectedValue = "Miles", MatchType = "CONTAINS" }
        });
        
        // Add classification keywords for fallback testing
        Context.PartLaborKeywords.AddRange(new[]
        {
            new PartLaborKeyword { Keyword = "oil", Classification = "Part", MatchType = "CONTAINS" },
            new PartLaborKeyword { Keyword = "installation", Classification = "Labor", MatchType = "CONTAINS" },
            new PartLaborKeyword { Keyword = "diagnostic", Classification = "Labor", MatchType = "CONTAINS" }
        });
        
        Context.SaveChanges();
    }
    
    public void Dispose() => Context?.Dispose();
}
```

---

## 7. Test Environment Setup

### 7.1 Local Development Environment

#### Prerequisites Installation Script
```powershell
# setup-test-environment.ps1

Write-Host "Setting up E2E QA Test Environment..." -ForegroundColor Green

# Install .NET SDK and tools
winget install Microsoft.DotNet.SDK.8

# Install Node.js for Playwright
winget install OpenJS.NodeJS

# Install test runners and tools
dotnet tool install --global coverlet.console
dotnet tool install --global reportgenerator

# Install Playwright browsers
npx playwright install

# Setup test databases
Write-Host "Creating test database..." -ForegroundColor Yellow
# Add SQL Server setup commands here

Write-Host "Environment setup complete!" -ForegroundColor Green
```

#### Test Configuration
```json
// appsettings.Testing.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=VehicleMaintenance_Test;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "BlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "test-invoices"
  },
  "FormRecognizer": {
    "Endpoint": "https://test-form-recognizer.cognitiveservices.azure.com/",
    "ApiKey": "test-api-key",
    "ModelId": "prebuilt-invoice"
  },
  "GitHub": {
    "ModelsApiEndpoint": "https://models.inference.ai.azure.com",
    "PersonalAccessToken": "test-token"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

### 7.2 CI/CD Pipeline Configuration

#### GitHub Actions Workflow
```yaml
# .github/workflows/e2e-qa.yml
name: E2E QA Testing Pipeline

on:
  push:
    branches: [E2E-qa, main]
  pull_request:
    branches: [main]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore --configuration Release
      
    - name: Run Unit Tests
      run: |
        dotnet test tests/VehicleMaintenanceInvoiceSystem.Tests/ \
          --no-build --configuration Release \
          --collect:"XPlat Code Coverage" \
          --logger trx --results-directory ./test-results
          
    - name: Generate Coverage Report
      run: |
        dotnet tool install --global dotnet-reportgenerator-globaltool
        reportgenerator -reports:./test-results/*/coverage.cobertura.xml \
          -targetdir:./coverage-report -reporttypes:Html;Cobertura
          
    - name: Upload Coverage Reports
      uses: actions/upload-artifact@v4
      with:
        name: coverage-report
        path: ./coverage-report

  integration-tests:
    runs-on: ubuntu-latest
    needs: unit-tests
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          SA_PASSWORD: TestPassword123!
          ACCEPT_EULA: Y
        ports:
          - 1433:1433
    
    steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 8.0.x
        
    - name: Run Integration Tests
      run: |
        dotnet test tests/VehicleMaintenanceInvoiceSystem.IntegrationTests/ \
          --configuration Release --logger trx
      env:
        ConnectionStrings__DefaultConnection: "Server=localhost,1433;Database=VehicleMaintenance_Test;User Id=sa;Password=TestPassword123!;TrustServerCertificate=true"

  e2e-tests:
    runs-on: ubuntu-latest
    needs: [unit-tests, integration-tests]
    steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 8.0.x
        
    - name: Setup Node.js
      uses: actions/setup-node@v4
      with:
        node-version: '18'
        
    - name: Install Playwright
      run: |
        npm install playwright
        npx playwright install
        
    - name: Start Application
      run: |
        dotnet run --project src/VehicleMaintenanceInvoiceSystem.csproj &
        sleep 30
      env:
        ASPNETCORE_ENVIRONMENT: Testing
        
    - name: Run E2E Tests
      run: npx playwright test
      
    - name: Upload E2E Test Results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: playwright-report
        path: playwright-report/
```

---

## 8. Test Execution Strategy

### 8.1 Test Automation Framework

#### Playwright E2E Test Configuration
```typescript
// playwright.config.ts
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [
    ['html'],
    ['junit', { outputFile: 'test-results/junit.xml' }]
  ],
  use: {
    baseURL: 'http://localhost:5001',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },
    {
      name: 'mobile-chrome',
      use: { ...devices['Pixel 5'] },
    },
  ],

  webServer: {
    command: 'dotnet run --project src/VehicleMaintenanceInvoiceSystem.csproj',
    port: 5001,
    timeout: 120 * 1000,
    reuseExistingServer: !process.env.CI,
  },
});
```

#### Test Execution Commands
```powershell
# Local development testing
./run-all-tests.ps1

# Unit tests only
dotnet test tests/VehicleMaintenanceInvoiceSystem.Tests/ --verbosity normal

# Integration tests
dotnet test tests/VehicleMaintenanceInvoiceSystem.IntegrationTests/ --verbosity normal

# E2E tests
npx playwright test

# Performance tests
dotnet test tests/VehicleMaintenanceInvoiceSystem.PerformanceTests/ --verbosity normal

# Coverage report generation
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report -reporttypes:Html
```

### 8.2 Test Execution Schedule

#### Continuous Integration
- **Every Commit**: Unit tests + lint checks
- **Pull Request**: Full test suite including integration and E2E
- **Nightly**: Performance tests + full regression suite
- **Weekly**: Security scanning + penetration tests

#### Test Execution Matrix
| Test Type | Local Dev | PR Build | Nightly | Weekly |
|-----------|-----------|----------|---------|---------|
| Unit Tests | ✅ | ✅ | ✅ | ✅ |
| Integration Tests | ⚪ | ✅ | ✅ | ✅ |
| E2E Tests | ⚪ | ✅ | ✅ | ✅ |
| Performance Tests | ⚪ | ⚪ | ✅ | ✅ |
| Security Tests | ⚪ | ⚪ | ⚪ | ✅ |
| Load Tests | ⚪ | ⚪ | ⚪ | ✅ |

---

## 9. Quality Gates and Acceptance Criteria

### 9.1 Code Quality Gates

#### Coverage Requirements
- **Unit Test Coverage**: Minimum 90% for business logic
- **Integration Test Coverage**: Minimum 80% for API endpoints
- **E2E Test Coverage**: 100% for critical user journeys

#### Performance Gates
- **API Response Time**: 95th percentile < 2 seconds
- **Invoice Processing**: Complete workflow < 30 seconds
- **UI Responsiveness**: Time to Interactive < 3 seconds
- **Database Queries**: No N+1 queries, all queries indexed

#### Security Gates
- **No High/Critical Vulnerabilities**: Zero tolerance
- **Authentication**: All protected endpoints secured
- **Data Validation**: All inputs sanitized and validated
- **Audit Logging**: All sensitive operations logged

### 9.2 Functional Acceptance Criteria

#### Invoice Processing Accuracy
```gherkin
Feature: Invoice Data Extraction Accuracy

Scenario: Extract invoice data with high accuracy
  Given a valid invoice PDF file
  When the invoice is processed through the system
  Then the data extraction accuracy should be >= 95%
  And the processing time should be <= 30 seconds
  And all required fields should be populated

Scenario: Handle various numeric formats
  Given an invoice with comma-separated numbers
  When the numeric data is extracted
  Then "67,890" should be parsed as 67890
  And "1,234,567" should be parsed as 1234567
  And the extraction confidence should be >= 90%
```

#### Approval Workflow Validation
```gherkin
Feature: Invoice Approval Process

Scenario: Approve an invoice successfully
  Given an unapproved invoice exists
  When the user clicks the approve button
  And confirms the approval
  Then the invoice status should change to approved
  And the approval timestamp should be set
  And the approval buttons should be hidden

Scenario: Reject and delete an invoice
  Given an unapproved invoice exists
  When the user clicks the reject button
  And confirms the rejection
  Then all invoice data should be deleted
  And the original file should be removed
  And the user should be redirected to upload page
```

---

## 10. Monitoring and Reporting

### 10.1 Test Metrics Dashboard

#### Key Performance Indicators
- **Test Execution Time**: Track trends and optimize slow tests
- **Test Success Rate**: Monitor flaky tests and failure patterns
- **Code Coverage Trends**: Ensure coverage doesn't regress
- **Defect Density**: Track bugs found per feature area
- **Mean Time to Resolution**: Bug fixing efficiency

#### Automated Reporting
```csharp
// Test results reporter
public class TestMetricsReporter
{
    public async Task GenerateReport()
    {
        var metrics = new TestExecutionReport
        {
            Timestamp = DateTime.UtcNow,
            TotalTests = await CountTotalTests(),
            PassedTests = await CountPassedTests(),
            FailedTests = await CountFailedTests(),
            SkippedTests = await CountSkippedTests(),
            ExecutionTime = await GetTotalExecutionTime(),
            Coverage = await GetCodeCoverage()
        };
        
        await _reportingService.PublishMetrics(metrics);
        await _notificationService.NotifyOnFailures(metrics);
    }
}
```

### 10.2 Quality Reporting

#### Daily Quality Report
```markdown
# Daily QA Report - August 28, 2025

## Summary
- ✅ **Build Status**: Passing
- ✅ **Test Results**: 847/847 tests passed
- ✅ **Code Coverage**: 91.2% (+0.3% from yesterday)
- ⚠️ **Performance**: API response times slightly elevated (avg 1.8s)

## Test Execution Results
| Category | Total | Passed | Failed | Skipped | Duration |
|----------|-------|--------|--------|---------|----------|
| Unit | 650 | 650 | 0 | 0 | 2m 15s |
| Integration | 145 | 145 | 0 | 0 | 8m 32s |
| E2E | 52 | 52 | 0 | 0 | 15m 47s |

## Coverage by Component
| Component | Coverage | Trend |
|-----------|----------|-------|
| Services | 94.2% | ↑ |
| Controllers | 88.7% | ↔ |
| Models | 96.1% | ↑ |
| Utilities | 89.3% | ↓ |

## Action Items
- [ ] Investigate API performance regression
- [ ] Add missing unit tests for utility functions
- [ ] Update E2E test for new approval workflow
```

---

## 11. Risk Management and Contingency Plans

### 11.1 Testing Risk Assessment

#### High-Risk Areas
| Risk | Impact | Probability | Mitigation Strategy |
|------|--------|-------------|-------------------|
| AI Service Outages | High | Medium | Comprehensive fallback testing |
| Database Performance | High | Low | Load testing + query optimization |
| File Upload Limits | Medium | Low | Edge case testing + validation |
| Browser Compatibility | Medium | Medium | Cross-browser E2E testing |
| Network Timeouts | Medium | High | Retry logic testing |

### 11.2 Contingency Plans

#### Test Environment Failures
```powershell
# Backup test environment setup
function Initialize-BackupTestEnvironment {
    Write-Host "Primary test environment failed, initializing backup..." -ForegroundColor Yellow
    
    # Start local SQL Server instance
    docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Test123!' -p 1434:1433 -d mcr.microsoft.com/mssql/server:2019-latest
    
    # Update connection strings
    $env:ConnectionStrings__DefaultConnection = "Server=localhost,1434;Database=VehicleMaintenance_Test;User Id=sa;Password=Test123!;TrustServerCertificate=true"
    
    # Initialize Azure Storage Emulator
    azurite --silent --location ./azurite-data
    
    Write-Host "Backup environment ready" -ForegroundColor Green
}
```

#### Critical Test Failures
```csharp
[Fact]
public async Task CriticalPath_FailureHandling()
{
    // Test the most critical user journey with multiple failure scenarios
    var scenarios = new[]
    {
        () => SimulateNetworkTimeout(),
        () => SimulateDbConnectionFailure(),
        () => SimulateAIServiceOutage(),
        () => SimulateBlobStorageFailure()
    };
    
    foreach (var scenario in scenarios)
    {
        // Each scenario should fail gracefully
        var result = await ProcessInvoiceWithFailure(scenario);
        
        result.Should().NotThrow();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.IsSuccess.Should().BeFalse();
    }
}
```

---

## 12. Test Maintenance and Evolution

### 12.1 Test Maintenance Strategy

#### Regular Test Reviews
- **Monthly**: Review flaky tests and update test data
- **Quarterly**: Update E2E tests for UI changes
- **Release Cycle**: Full test suite validation and updates

#### Test Data Refresh
```csharp
public class TestDataMaintenance
{
    public async Task RefreshTestInvoices()
    {
        // Archive old test files
        await ArchiveOldTestFiles();
        
        // Generate new test scenarios
        await GenerateNewTestCases();
        
        // Update classification keywords
        await UpdateFallbackKeywords();
        
        // Refresh field mappings
        await RefreshFieldMappings();
    }
}
```

### 12.2 Continuous Improvement

#### Test Quality Metrics
- Track test execution time and optimize slow tests
- Monitor test flakiness and improve stability
- Measure test coverage and identify gaps
- Analyze failure patterns and improve assertions

#### Innovation and Updates
- Evaluate new testing tools and frameworks
- Implement visual regression testing for UI components
- Add accessibility testing to E2E suite
- Integrate security testing into pipeline

---

## 13. Documentation and Training

### 13.1 Test Documentation

#### Test Case Documentation
Each test should include:
- **Purpose**: Clear description of what is being tested
- **Prerequisites**: Required setup or test data
- **Steps**: Detailed execution steps
- **Expected Results**: Clear success criteria
- **Cleanup**: Post-test cleanup requirements

#### API Testing Documentation
```markdown
# API Testing Guide

## Authentication
All API tests use mock authentication unless testing auth specifically.

## Test Data Setup
Use `TestDataFactory.CreateTestInvoice()` for consistent test data.

## Common Assertions
- Response status codes
- JSON schema validation
- Performance requirements
- Error message format
```

### 13.2 Team Training Plan

#### Developer Onboarding
1. **Week 1**: Test framework introduction and local setup
2. **Week 2**: Writing unit tests and understanding test patterns
3. **Week 3**: Integration testing and mocking strategies
4. **Week 4**: E2E testing and debugging techniques

#### Ongoing Education
- Monthly test review sessions
- Best practices sharing
- Tool updates and new techniques
- Cross-team collaboration on testing standards

---

## 14. Success Metrics and KPIs

### 14.1 Primary Success Metrics

#### Quality Metrics
- **Defect Escape Rate**: < 5% of bugs make it to production
- **Test Coverage**: Maintain > 90% coverage for critical paths
- **Mean Time to Detection**: < 4 hours for critical issues
- **Mean Time to Resolution**: < 24 hours for high priority bugs

#### Performance Metrics
- **Test Execution Time**: Full suite < 45 minutes
- **Feedback Loop**: < 10 minutes from commit to test results
- **Deployment Success Rate**: > 95% successful deployments
- **System Uptime**: > 99.9% availability

### 14.2 Reporting and Reviews

#### Weekly Quality Review
- Test execution results and trends
- Coverage reports and gap analysis
- Performance benchmarking results
- Risk assessment updates

#### Monthly Retrospective
- Test strategy effectiveness review
- Process improvement opportunities
- Tool and framework evaluations
- Team feedback and training needs

---

## 15. Appendix

### 15.1 Test Environment URLs
- **Local Development**: http://localhost:5001
- **CI/CD Environment**: Configured per pipeline
- **Staging Environment**: https://maintenance-plus-staging.azurewebsites.net
- **Production Monitoring**: https://maintenance-plus.azurewebsites.net

### 15.2 Tool Versions and Dependencies
```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
<PackageReference Include="Playwright" Version="1.40.0" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="NSubstitute" Version="5.1.0" />
<PackageReference Include="xunit" Version="2.6.1" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
<PackageReference Include="coverlet.collector" Version="6.0.0" />
```

### 15.3 Contact Information
- **QA Lead**: [Contact Information]
- **Development Team**: [Team Contact]
- **Product Owner**: [PO Contact]
- **DevOps Support**: [DevOps Contact]

---

**Document Version**: 1.0
**Last Updated**: August 28, 2025
**Next Review**: September 15, 2025
**Status**: Active - E2E-qa Branch Implementation
