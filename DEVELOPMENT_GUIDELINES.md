# Vehicle Maintenance Invoice System - Development Guidelines
# These guidelines help CodeRabbit and team members maintain code quality

## 🎯 Overview
This document outlines coding standards, review criteria, and best practices specific to the Vehicle Maintenance Invoice System. It serves as a reference for both automated (CodeRabbit) and manual code reviews.

## 🏗️ Architecture Principles

### Separation of Concerns
- **Controllers**: Handle HTTP requests, basic validation, and response formatting
- **Services**: Contain business logic and orchestrate operations
- **Data Layer**: Handle database operations and entity management
- **Models/DTOs**: Define data contracts and transfer objects

### Dependency Injection
- Use constructor injection for all dependencies
- Register services with appropriate lifetimes (Singleton, Scoped, Transient)
- Avoid service locator pattern

## 🔧 C# Coding Standards

### Naming Conventions
```csharp
// Classes, Methods, Properties - PascalCase
public class InvoiceProcessingService
public async Task<InvoiceDto> ProcessInvoiceAsync(int invoiceId)
public string InvoiceNumber { get; set; }

// Parameters, Local Variables - camelCase
public void ProcessInvoice(int invoiceId, string fileName)
var processedInvoice = await service.GetInvoiceAsync(invoiceId);

// Private fields - camelCase with underscore prefix
private readonly ILogger<InvoiceService> _logger;
private readonly InvoiceDbContext _context;

// Constants - UPPER_CASE
public const int MAX_FILE_SIZE_MB = 10;
public const string DEFAULT_CURRENCY = "USD";
```

### Async/Await Patterns
```csharp
// Good - Async all the way
public async Task<InvoiceDto> GetInvoiceAsync(int id)
{
    var invoice = await _context.InvoiceHeaders
        .Include(i => i.InvoiceLines)
        .FirstOrDefaultAsync(i => i.InvoiceID == id);
    
    return _mapper.Map<InvoiceDto>(invoice);
}

// Bad - Blocking async calls
public InvoiceDto GetInvoice(int id)
{
    return GetInvoiceAsync(id).Result; // Don't do this!
}
```

### Error Handling
```csharp
// Good - Specific exception handling with logging
public async Task<Result<InvoiceDto>> ProcessInvoiceAsync(IFormFile file)
{
    try
    {
        _logger.LogInformation("Starting invoice processing for file {FileName}", file.FileName);
        
        // Process invoice
        var result = await _ocrService.ExtractDataAsync(file);
        
        _logger.LogInformation("Successfully processed invoice {InvoiceNumber}", result.InvoiceNumber);
        return Result.Success(result);
    }
    catch (InvalidFileFormatException ex)
    {
        _logger.LogWarning("Invalid file format: {Error}", ex.Message);
        return Result.Failure<InvoiceDto>("Invalid file format");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error processing invoice");
        return Result.Failure<InvoiceDto>("An unexpected error occurred");
    }
}
```

## 🗄️ Database Guidelines

### Entity Framework Best Practices
```csharp
// Good - Explicit loading with proper filtering
public async Task<List<InvoiceHeaderDto>> GetInvoicesAsync(int pageSize, int pageNumber)
{
    return await _context.InvoiceHeaders
        .Where(i => !i.IsDeleted)
        .OrderByDescending(i => i.CreatedAt)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .Select(i => new InvoiceHeaderDto
        {
            InvoiceID = i.InvoiceID,
            InvoiceNumber = i.InvoiceNumber,
            TotalCost = i.TotalCost
        })
        .ToListAsync();
}

// Bad - Loading entire entities unnecessarily
public async Task<List<InvoiceHeader>> GetInvoices()
{
    return await _context.InvoiceHeaders
        .Include(i => i.InvoiceLines) // Unnecessary if not using lines
        .ToListAsync(); // Loads all data
}
```

### Migration Standards
```csharp
// Good - Descriptive migration names and safe operations
public partial class AddApprovalFieldsToInvoiceHeader : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "Approved",
            table: "InvoiceHeader",
            nullable: false,
            defaultValue: false);
            
        migrationBuilder.AddColumn<DateTime>(
            name: "ApprovedAt",
            table: "InvoiceHeader",
            nullable: true);
    }
    
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ApprovedAt", table: "InvoiceHeader");
        migrationBuilder.DropColumn(name: "Approved", table: "InvoiceHeader");
    }
}
```

## 🔒 Security Guidelines

### Input Validation
```csharp
// Good - Comprehensive validation
public async Task<IActionResult> UploadInvoice([FromForm] InvoiceUploadRequest request)
{
    if (request.File == null || request.File.Length == 0)
        return BadRequest("File is required");
    
    if (request.File.Length > MAX_FILE_SIZE)
        return BadRequest("File too large");
    
    var allowedExtensions = new[] { ".pdf", ".png", ".jpg", ".jpeg" };
    var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
    
    if (!allowedExtensions.Contains(extension))
        return BadRequest("Invalid file format");
    
    // Additional MIME type validation
    if (!IsValidMimeType(request.File))
        return BadRequest("Invalid file type");
}

private bool IsValidMimeType(IFormFile file)
{
    var allowedMimeTypes = new[] 
    { 
        "application/pdf", 
        "image/png", 
        "image/jpeg" 
    };
    
    return allowedMimeTypes.Contains(file.ContentType);
}
```

### Authentication & Authorization
```csharp
// Good - Proper authorization attributes
[Authorize(Roles = "Manager")]
[HttpPost("approve/{id}")]
public async Task<IActionResult> ApproveInvoice(int id)
{
    var currentUser = User.Identity.Name;
    var result = await _invoiceService.ApproveInvoiceAsync(id, currentUser);
    
    if (result.IsSuccess)
    {
        _logger.LogInformation("Invoice {InvoiceId} approved by {User}", id, currentUser);
        return Ok(result.Value);
    }
    
    return BadRequest(result.Error);
}
```

## 🚀 Performance Guidelines

### Caching Strategies
```csharp
// Good - Appropriate caching for reference data
public async Task<List<VehicleDto>> GetVehiclesAsync()
{
    const string cacheKey = "vehicles_list";
    
    if (_memoryCache.TryGetValue(cacheKey, out List<VehicleDto> cachedVehicles))
    {
        return cachedVehicles;
    }
    
    var vehicles = await _context.Vehicles
        .Where(v => v.IsActive)
        .Select(v => new VehicleDto { Id = v.Id, Name = v.Name })
        .ToListAsync();
    
    _memoryCache.Set(cacheKey, vehicles, TimeSpan.FromHours(1));
    return vehicles;
}
```

### Efficient Queries
```csharp
// Good - Pagination and projection
public async Task<PagedResult<InvoiceListItemDto>> GetInvoicesPagedAsync(
    int page, int pageSize, string searchTerm = null)
{
    var query = _context.InvoiceHeaders.AsQueryable();
    
    if (!string.IsNullOrEmpty(searchTerm))
    {
        query = query.Where(i => 
            i.InvoiceNumber.Contains(searchTerm) || 
            i.VehicleId.ToString().Contains(searchTerm));
    }
    
    var totalCount = await query.CountAsync();
    
    var items = await query
        .OrderByDescending(i => i.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(i => new InvoiceListItemDto
        {
            InvoiceID = i.InvoiceID,
            InvoiceNumber = i.InvoiceNumber,
            TotalCost = i.TotalCost,
            CreatedAt = i.CreatedAt
        })
        .ToListAsync();
    
    return new PagedResult<InvoiceListItemDto>
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
```

## 🎨 Frontend Guidelines

### Razor Views
```html
<!-- Good - Proper model binding and validation -->
<form asp-action="CreateInvoice" method="post" enctype="multipart/form-data">
    <div class="form-group">
        <label asp-for="VehicleId" class="form-label">Vehicle</label>
        <select asp-for="VehicleId" asp-items="ViewBag.Vehicles" class="form-control">
            <option value="">-- Select Vehicle --</option>
        </select>
        <span asp-validation-for="VehicleId" class="text-danger"></span>
    </div>
    
    <div class="form-group">
        <label asp-for="InvoiceFile" class="form-label">Invoice File</label>
        <input asp-for="InvoiceFile" type="file" class="form-control" 
               accept=".pdf,.png,.jpg,.jpeg" />
        <span asp-validation-for="InvoiceFile" class="text-danger"></span>
    </div>
    
    <button type="submit" class="btn btn-primary">Upload Invoice</button>
</form>
```

### CSS Organization
```css
/* Good - BEM methodology and component organization */
.invoice-card {
    background: var(--card-background);
    border: 1px solid var(--border-color);
    border-radius: var(--border-radius);
    padding: var(--spacing-md);
}

.invoice-card__header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: var(--spacing-sm);
}

.invoice-card__title {
    font-size: var(--font-size-lg);
    font-weight: var(--font-weight-semibold);
    color: var(--text-primary);
}

.invoice-card__status--approved {
    background-color: var(--success-color);
    color: var(--success-text);
}

.invoice-card__status--pending {
    background-color: var(--warning-color);
    color: var(--warning-text);
}
```

## 🧪 Testing Guidelines

### Unit Tests
```csharp
[Test]
public async Task ProcessInvoiceAsync_ValidFile_ReturnsSuccess()
{
    // Arrange
    var mockFile = CreateMockFile("test-invoice.pdf", "application/pdf");
    var expectedResult = new InvoiceDto { InvoiceNumber = "INV-001" };
    
    _mockOcrService
        .Setup(x => x.ExtractDataAsync(It.IsAny<IFormFile>()))
        .ReturnsAsync(expectedResult);
    
    // Act
    var result = await _invoiceService.ProcessInvoiceAsync(mockFile);
    
    // Assert
    Assert.IsTrue(result.IsSuccess);
    Assert.AreEqual(expectedResult.InvoiceNumber, result.Value.InvoiceNumber);
    _mockLogger.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully processed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);
}
```

## 🔍 Code Review Checklist

### Automated Checks (CodeRabbit will verify)
- [ ] Code follows naming conventions
- [ ] Proper async/await usage
- [ ] Error handling implemented
- [ ] Security best practices followed
- [ ] Performance considerations addressed
- [ ] Documentation updated

### Manual Review Focus Areas
- [ ] Business logic correctness
- [ ] User experience impact
- [ ] Database schema changes
- [ ] Integration points
- [ ] Security implications
- [ ] Performance impact

## 📋 Domain-Specific Guidelines

### Invoice Processing
- Always validate file types and sizes
- Implement comprehensive logging for audit trails  
- Handle OCR service failures gracefully
- Ensure data accuracy validation after OCR processing
- Maintain invoice state transitions properly

### File Handling
- Validate file signatures, not just extensions
- Implement virus scanning for uploaded files
- Use streaming for large files
- Clean up temporary files after processing
- Implement proper access controls for stored files

### Approval Workflow
- Log all approval actions with timestamps and user information
- Implement proper authorization checks
- Ensure state transitions are valid
- Provide audit trail for all changes
- Handle concurrent approval attempts

---

This document is living and should be updated as the project evolves. All team members should follow these guidelines to ensure code quality and consistency.
