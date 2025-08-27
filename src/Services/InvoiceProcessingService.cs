using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VehicleMaintenanceInvoiceSystem.Data;
using VehicleMaintenanceInvoiceSystem.Models;

namespace VehicleMaintenanceInvoiceSystem.Services;

/// <summary>
/// Interface for invoice processing operations
/// </summary>
public interface IInvoiceProcessingService
{
    Task<InvoiceProcessingResponse> ProcessInvoiceAsync(IFormFile file);
    Task<PaginatedResult<InvoiceSummaryDto>> GetInvoicesAsync(int page = 1, int pageSize = 20);
    Task<PaginatedResult<InvoiceSummaryDto>> GetProcessedInvoicesAsync(int page = 1, int pageSize = 20, string? status = "all");
    Task<InvoiceDetailsDto?> GetInvoiceByIdAsync(int invoiceId, bool includeUnapproved = false);
    Task<PaginatedResult<InvoiceSummaryDto>> GetInvoicesByVehicleAsync(string vehicleId, int page = 1, int pageSize = 20);
    Task<PaginatedResult<InvoiceSummaryDto>> GetInvoicesByDateAsync(DateTime date, int page = 1, int pageSize = 20);
    Task<PaginatedResult<InvoiceSummaryDto>> GetInvoicesByUploadedDateAsync(DateTime date, int page = 1, int pageSize = 20);
    Task<string> GetSecureFileUrlAsync(int invoiceId, string? userIdentifier = null);
    Task<InvoiceActionResponse> ApproveInvoiceAsync(int invoiceId, string approvedBy);
    Task<InvoiceActionResponse> RejectInvoiceAsync(int invoiceId);
    Task<object?> GetRawExtractedDataAsync(int invoiceId);
}

/// <summary>
/// Service for invoice processing and data operations
/// </summary>
public class InvoiceProcessingService : IInvoiceProcessingService
{
    private readonly InvoiceDbContext _context;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IFormRecognizerService _formRecognizerService;
    private readonly IComprehensiveProcessingService _comprehensiveProcessingService;
    private readonly IInvoiceFallbackService _fallbackService;
    private readonly ILogger<InvoiceProcessingService> _logger;

    public InvoiceProcessingService(
        InvoiceDbContext context,
        IBlobStorageService blobStorageService,
        IFormRecognizerService formRecognizerService,
        IComprehensiveProcessingService comprehensiveProcessingService,
        IInvoiceFallbackService fallbackService,
        ILogger<InvoiceProcessingService> logger)
    {
        _context = context;
        _blobStorageService = blobStorageService;
        _formRecognizerService = formRecognizerService;
        _comprehensiveProcessingService = comprehensiveProcessingService;
        _fallbackService = fallbackService;
        _logger = logger;
    }

    public async Task<InvoiceProcessingResponse> ProcessInvoiceAsync(IFormFile file)
    {
        var response = new InvoiceProcessingResponse();

        try
        {
            _logger.LogInformation("Starting processing for file: {FileName}", file.FileName);

            // Step 1: Upload to Blob Storage
            var uploadResult = await _blobStorageService.UploadFileAsync(file, file.FileName);
            if (!uploadResult.Success)
            {
                response.Success = false;
                response.Message = uploadResult.Message;
                response.Errors.Add($"File upload failed: {uploadResult.Message}");
                return response;
            }

            _logger.LogInformation("File uploaded successfully: {BlobUrl}", uploadResult.BlobUrl);

            // Step 2: Standard OCR Processing with Form Recognizer (revert to original)
            FormRecognizerResult ocrResult;
            using (var stream = file.OpenReadStream())
            {
                ocrResult = await _formRecognizerService.AnalyzeInvoiceAsync(stream);
            }

            if (!ocrResult.Success || ocrResult.InvoiceData == null)
            {
                // Clean up uploaded blob since OCR processing failed
                await CleanupBlobFileAsync(uploadResult.BlobUrl ?? string.Empty, "OCR failure");
                
                response.Success = false;
                response.Message = "OCR processing failed";
                response.Errors.Add($"Form Recognizer error: {ocrResult.ErrorMessage}");
                return response;
            }

            _logger.LogInformation("OCR processing completed with confidence: {Confidence}%", ocrResult.OverallConfidence);

            // Step 3: Comprehensive AI Processing (GPT-4o with intelligent fallback)
            _logger.LogInformation("Starting comprehensive AI processing for invoice");
            var comprehensiveResult = await _comprehensiveProcessingService.ProcessInvoiceComprehensivelyAsync(
                ocrResult.RawJson ?? "", ocrResult.InvoiceData);

            if (!comprehensiveResult.Success)
            {
                // Clean up uploaded blob since AI processing failed
                await CleanupBlobFileAsync(uploadResult.BlobUrl ?? string.Empty, "AI processing failure");
                
                response.Success = false;
                response.Message = $"AI processing failed: {comprehensiveResult.ErrorMessage}";
                response.Errors.Add($"Comprehensive processing error: {comprehensiveResult.ErrorMessage}");
                return response;
            }

            // Update invoice data with AI-enhanced results
            UpdateInvoiceDataFromAI(ocrResult.InvoiceData, comprehensiveResult);

            _logger.LogInformation("Comprehensive AI processing completed using: {Method}", comprehensiveResult.ProcessingMethod);

            // Step 4: Validate enhanced data
            var enhancedValidationResult = ValidateInvoiceData(ocrResult.InvoiceData);
            if (!enhancedValidationResult.IsValid)
            {
                // Clean up uploaded blob since data validation failed
                await CleanupBlobFileAsync(uploadResult.BlobUrl ?? string.Empty, "validation failure");
                
                response.Success = false;
                response.Message = "Data validation failed";
                response.Errors.AddRange(enhancedValidationResult.Errors);
                return response;
            }

            if (enhancedValidationResult.Warnings.Any())
            {
                response.Warnings.AddRange(enhancedValidationResult.Warnings);
            }

            // Add AI processing notes as warnings for user visibility
            if (comprehensiveResult.ProcessingNotes.Any())
            {
                response.Warnings.AddRange(comprehensiveResult.ProcessingNotes);
            }

            // Step 5: Save to database
            var validationResult = ValidateInvoiceData(ocrResult.InvoiceData);
            if (!validationResult.IsValid)
            {
                // Clean up uploaded blob since data validation failed
                await CleanupBlobFileAsync(uploadResult.BlobUrl ?? string.Empty, "validation failure");
                
                response.Success = false;
                response.Message = "Data validation failed";
                response.Errors.AddRange(validationResult.Errors);
                return response;
            }

            if (validationResult.Warnings.Any())
            {
                response.Warnings.AddRange(validationResult.Warnings);
            }

            // Step 4: Save to database
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var invoiceHeader = CreateInvoiceHeader(ocrResult.InvoiceData, uploadResult.BlobUrl!, ocrResult.OverallConfidence, ocrResult.RawJson);
                _context.InvoiceHeaders.Add(invoiceHeader);
                
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    // Handle unique constraint violations and other database errors
                    await transaction.RollbackAsync();
                    
                    var innerMessage = ex.InnerException?.Message ?? ex.Message;
                    
                    if (innerMessage.Contains("IX_InvoiceHeader_InvoiceNumber") || 
                        innerMessage.Contains("duplicate") && innerMessage.Contains("InvoiceNumber"))
                    {
                        // Race condition: another request created an invoice with the same number
                        // Clean up the uploaded blob file to prevent storage leak
                        await CleanupBlobFileAsync(uploadResult.BlobUrl ?? string.Empty, "duplicate invoice detection");
                        
                        response.Success = false;
                        response.Message = $"Invoice number '{invoiceHeader.InvoiceNumber}' already exists";
                        response.Errors.Add($"Duplicate invoice number detected: {invoiceHeader.InvoiceNumber}");
                        _logger.LogWarning("Duplicate invoice number conflict for {InvoiceNumber}: {Error}", 
                            invoiceHeader.InvoiceNumber, innerMessage);
                        return response;
                    }
                    else
                    {
                        // Other database constraint violations - clean up blob before rethrowing
                        await CleanupBlobFileAsync(uploadResult.BlobUrl ?? string.Empty, "database constraint violation");
                        
                        response.Success = false;
                        response.Message = "Database constraint violation occurred";
                        response.Errors.Add($"Database error: {ex.Message}");
                        _logger.LogError(ex, "Database constraint violation when saving invoice {InvoiceNumber}", 
                            invoiceHeader.InvoiceNumber);
                        throw; // Re-throw for other types of database errors
                    }
                }

                // Add line items
                foreach (var lineData in ocrResult.InvoiceData.LineItems)
                {
                    var invoiceLine = CreateInvoiceLine(lineData, invoiceHeader.InvoiceID);
                    _context.InvoiceLines.Add(invoiceLine);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Invoice {InvoiceNumber} saved successfully with ID: {InvoiceId}", 
                    invoiceHeader.InvoiceNumber, invoiceHeader.InvoiceID);

                // Phase 2: Intelligence processing is now handled by comprehensive processing service above
                // Old intelligence processing removed - results are already applied from comprehensive processing
                
                _logger.LogInformation("Invoice processing completed with comprehensive AI processing for Invoice ID: {InvoiceId}", invoiceHeader.InvoiceID);

                response.Success = true;
                response.Message = "Invoice processed successfully";
                response.InvoiceId = invoiceHeader.InvoiceID;
                response.ConfidenceScore = ocrResult.OverallConfidence;

                return response;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving invoice to database");
                response.Success = false;
                response.Message = "Database save failed";
                response.Errors.Add($"Database error: {ex.Message}");
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing invoice: {FileName}", file.FileName);
            response.Success = false;
            response.Message = "Unexpected error occurred";
            response.Errors.Add($"Processing error: {ex.Message}");
            return response;
        }
    }

public async Task<PaginatedResult<InvoiceSummaryDto>> GetInvoicesAsync(int page = 1, int pageSize = 20)
{
    pageSize =  Math.Clamp(pageSize, 1, 100); // Limit page size
    page = Math.Max(page, 1);
    var skip = (page - 1) * pageSize;

    var query = _context.InvoiceHeaders
        .AsNoTracking()
        .Where(i => i.Approved)
        .OrderByDescending(i => i.CreatedAt);

    var totalCount = await query.CountAsync();

    var invoices = await query
        .Skip(skip)
        .Take(pageSize)
        .Select(i => new InvoiceSummaryDto
        {
            InvoiceID = i.InvoiceID,
            VehicleID = i.VehicleID,
            Odometer = i.Odometer,
            InvoiceNumber = i.InvoiceNumber,
            InvoiceDate = i.InvoiceDate,
            TotalCost = i.TotalCost,
            TotalPartsCost = i.TotalPartsCost,
            TotalLaborCost = i.TotalLaborCost,
            Description = i.Description,
            ConfidenceScore = i.ConfidenceScore,
            CreatedAt = i.CreatedAt,
            Approved = i.Approved,
            ApprovedAt = i.ApprovedAt,
            ApprovedBy = i.ApprovedBy,
            LineItemCount = i.InvoiceLines.Count(),
            LineItems = i.InvoiceLines.Select(l => new InvoiceLineDto
            {
                LineID = l.LineID,
                LineNumber = l.LineNumber,
                Description = l.Description,
                UnitCost = l.UnitCost,
                Quantity = l.Quantity,
                TotalLineCost = l.TotalLineCost,
                PartNumber = l.PartNumber,
                Category = l.Category,
                ConfidenceScore = l.ExtractionConfidence
            }).OrderBy(l => l.LineNumber).ToList()
        })
        .ToListAsync();

    return new PaginatedResult<InvoiceSummaryDto>
    {
        Items = invoices,
        TotalCount = totalCount,
        PageNumber = page,
        PageSize = pageSize
    };
}

public async Task<PaginatedResult<InvoiceSummaryDto>> GetProcessedInvoicesAsync(int page = 1, int pageSize = 20, string? status = "all")
{
    pageSize = Math.Clamp(pageSize, 1, 100); // Limit page size
    page = Math.Max(page, 1);
    var skip = (page - 1) * pageSize;

    var query = _context.InvoiceHeaders
        .AsNoTracking()
        .OrderByDescending(i => i.CreatedAt)
        .AsQueryable();

    // Apply status filter: all (default), approved, unapproved
    if (!string.IsNullOrWhiteSpace(status))
    {
        if (status.Equals("approved", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(i => i.Approved);
        }
        else if (status.Equals("unapproved", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(i => !i.Approved);
        }
    }

    var totalCount = await query.CountAsync();

    var invoices = await query
        .Skip(skip)
        .Take(pageSize)
        .Select(i => new InvoiceSummaryDto
        {
            InvoiceID = i.InvoiceID,
            VehicleID = i.VehicleID,
            Odometer = i.Odometer,
            InvoiceNumber = i.InvoiceNumber,
            InvoiceDate = i.InvoiceDate,
            TotalCost = i.TotalCost,
            TotalPartsCost = i.TotalPartsCost,
            TotalLaborCost = i.TotalLaborCost,
            Description = i.Description,
            ConfidenceScore = i.ConfidenceScore,
            CreatedAt = i.CreatedAt,
            Approved = i.Approved,
            ApprovedAt = i.ApprovedAt,
            ApprovedBy = i.ApprovedBy,
            LineItemCount = i.InvoiceLines.Count(),
            LineItems = i.InvoiceLines.Select(l => new InvoiceLineDto
            {
                LineID = l.LineID,
                LineNumber = l.LineNumber,
                Description = l.Description,
                UnitCost = l.UnitCost,
                Quantity = l.Quantity,
                TotalLineCost = l.TotalLineCost,
                PartNumber = l.PartNumber,
                Category = l.Category,
                ConfidenceScore = l.ExtractionConfidence
            }).OrderBy(l => l.LineNumber).ToList()
        })
        .ToListAsync();

    return new PaginatedResult<InvoiceSummaryDto>
    {
        Items = invoices,
        TotalCount = totalCount,
        PageNumber = page,
        PageSize = pageSize
    };
}

    public async Task<InvoiceDetailsDto?> GetInvoiceByIdAsync(int invoiceId, bool includeUnapproved = false)
    {
        try
        {
            _logger.LogInformation("Starting GetInvoiceByIdAsync for Invoice ID: {InvoiceId}", invoiceId);
            
            _logger.LogInformation("Step 1: Querying database for Invoice ID: {InvoiceId}", invoiceId);
            var query = _context.InvoiceHeaders
                .AsNoTracking()
                .Include(i => i.InvoiceLines)
                .Where(i => i.InvoiceID == invoiceId);

            if (!includeUnapproved)
            {
                query = query.Where(i => i.Approved);
            }

            var invoice = await query.FirstOrDefaultAsync();

            if (invoice == null)
            {
                _logger.LogWarning("Step 1 Result: No invoice found with ID: {InvoiceId}", invoiceId);
                return null;
            }

            _logger.LogInformation("Step 1 Result: Found invoice with ID: {InvoiceId}, LineItems count: {LineItemCount}", invoiceId, invoice.InvoiceLines?.Count ?? 0);

            _logger.LogInformation("Step 2: Creating InvoiceDetailsDto for Invoice ID: {InvoiceId}", invoiceId);
            var result = new InvoiceDetailsDto
            {
                InvoiceID = invoice.InvoiceID,
                VehicleID = invoice.VehicleID,
                Odometer = invoice.Odometer,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                TotalCost = invoice.TotalCost,
                TotalPartsCost = invoice.TotalPartsCost,
                TotalLaborCost = invoice.TotalLaborCost,
                Description = invoice.Description,
                BlobFileUrl = invoice.BlobFileUrl,
                ConfidenceScore = invoice.ConfidenceScore,
                CreatedAt = invoice.CreatedAt,
                Approved = invoice.Approved,
                ApprovedAt = invoice.ApprovedAt,
                ApprovedBy = invoice.ApprovedBy
            };

        _logger.LogInformation("Step 3: Processing {LineItemCount} line items for Invoice ID: {InvoiceId}", invoice.InvoiceLines?.Count ?? 0, invoiceId);
        
        try
        {
            result.LineItems = invoice.InvoiceLines?
                .OrderBy(l => l.LineNumber)
                .Select(l => {
                    _logger.LogDebug("Processing line item {LineNumber} for Invoice {InvoiceId}: Quantity={Quantity}, UnitCost={UnitCost}, TotalLineCost={TotalLineCost}", 
                        l.LineNumber, invoiceId, l.Quantity, l.UnitCost, l.TotalLineCost);
                    
                    return new InvoiceLineDto
                    {
                        LineID = l.LineID,
                        LineNumber = l.LineNumber,
                        Description = l.Description,
                        UnitCost = l.UnitCost,
                        Quantity = l.Quantity,
                        TotalLineCost = l.TotalLineCost,
                        PartNumber = l.PartNumber,
                        Category = string.IsNullOrWhiteSpace(l.ClassifiedCategory) ? l.Category : 
                                 (!l.ClassifiedCategory.Equals("Unclassified", StringComparison.OrdinalIgnoreCase) ? l.ClassifiedCategory : l.Category),
                        ClassifiedCategory = l.ClassifiedCategory,
                        ClassificationConfidence = l.ClassificationConfidence,
                        ConfidenceScore = l.ExtractionConfidence
                    };
                })
                .ToList() ?? new List<InvoiceLineDto>();
                
            _logger.LogInformation("Step 3 Success: Processed all line items for Invoice ID: {InvoiceId}", invoiceId);
        }
        catch (InvalidCastException castEx)
        {
            _logger.LogError(castEx, "CASTING ERROR processing line items for Invoice ID: {InvoiceId}. This indicates database column type mismatch (INT vs DECIMAL): {ErrorMessage}", invoiceId, castEx.Message);
            throw new InvalidOperationException($"Database schema error for Invoice {invoiceId}: Cannot convert stored integer values to decimal. Please run database schema fix.", castEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR processing line items for Invoice ID: {InvoiceId}: {ErrorMessage}", invoiceId, ex.Message);
            throw;
        }

        _logger.LogInformation("Step 4: Successfully completed GetInvoiceByIdAsync for Invoice ID: {InvoiceId}", invoiceId);
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "FATAL ERROR in GetInvoiceByIdAsync for Invoice ID: {InvoiceId}: {ErrorMessage}", invoiceId, ex.Message);
        throw;
    }
    }

public async Task<PaginatedResult<InvoiceSummaryDto>> GetInvoicesByVehicleAsync(string vehicleId, int page = 1, int pageSize = 20)
{
    page = Math.Max(page, 1); // Ensure page >= 1
    pageSize = Math.Clamp(pageSize, 1, 100); // Enforce [1,100]
    var skip = (page - 1) * pageSize;

    var query = _context.InvoiceHeaders
        .AsNoTracking()
        .Where(i => i.VehicleID == vehicleId && i.Approved)
        .OrderByDescending(i => i.CreatedAt);

    var totalCount = await query.CountAsync();

    var invoices = await query
        .Skip(skip)
        .Take(pageSize)
        .Select(i => new InvoiceSummaryDto
        {
            InvoiceID = i.InvoiceID,
            VehicleID = i.VehicleID,
            Odometer = i.Odometer,
            InvoiceNumber = i.InvoiceNumber,
            InvoiceDate = i.InvoiceDate,
            TotalCost = i.TotalCost,
            TotalPartsCost = i.TotalPartsCost,
            TotalLaborCost = i.TotalLaborCost,
            Description = i.Description,
            ConfidenceScore = i.ConfidenceScore,
            CreatedAt = i.CreatedAt,
            Approved = i.Approved,
            ApprovedAt = i.ApprovedAt,
            ApprovedBy = i.ApprovedBy,
            LineItemCount = i.InvoiceLines.Count(),

            LineItems = i.InvoiceLines
                .OrderBy(l => l.LineNumber)
                .Select(l => new InvoiceLineDto
                {
                    LineID = l.LineID,
                    LineNumber = l.LineNumber,
                    Description = l.Description,
                    UnitCost = l.UnitCost,
                    Quantity = l.Quantity,
                    TotalLineCost = l.TotalLineCost,
                    PartNumber = l.PartNumber,
                    Category = l.Category,
                    ConfidenceScore = l.ExtractionConfidence
                })
                .ToList()
        })
        .ToListAsync();

    return new PaginatedResult<InvoiceSummaryDto>
    {
        Items = invoices,
        TotalCount = totalCount,
        PageNumber = page,
        PageSize = pageSize
    };
}

public async Task<PaginatedResult<InvoiceSummaryDto>> GetInvoicesByDateAsync(DateTime date, int page = 1, int pageSize = 20)
{
    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, 100); // Enforce [1,100]
    var skip = (page - 1) * pageSize;
    
    // Create date range for the entire day
    var startOfDay = date.Date;
    var endOfDay = startOfDay.AddDays(1);

    var query = _context.InvoiceHeaders
        .AsNoTracking()
        .Where(i => i.InvoiceDate >= startOfDay && i.InvoiceDate < endOfDay && i.Approved)
        .OrderByDescending(i => i.CreatedAt);

    var totalCount = await query.CountAsync();

    var invoices = await query
        .Skip(skip)
        .Take(pageSize)
        .Select(i => new InvoiceSummaryDto
        {
            InvoiceID = i.InvoiceID,
            VehicleID = i.VehicleID,
            Odometer = i.Odometer,
            InvoiceNumber = i.InvoiceNumber,
            InvoiceDate = i.InvoiceDate,
            TotalCost = i.TotalCost,
            TotalPartsCost = i.TotalPartsCost,
            TotalLaborCost = i.TotalLaborCost,
            Description = i.Description,
            ConfidenceScore = i.ConfidenceScore,
            CreatedAt = i.CreatedAt,
            Approved = i.Approved,
            ApprovedAt = i.ApprovedAt,
            ApprovedBy = i.ApprovedBy,
            LineItemCount = i.InvoiceLines.Count(),

            LineItems = i.InvoiceLines
                .OrderBy(l => l.LineNumber)
                .Select(l => new InvoiceLineDto
                {
                    LineID = l.LineID,
                    LineNumber = l.LineNumber,
                    Description = l.Description,
                    UnitCost = l.UnitCost,
                    Quantity = l.Quantity,
                    TotalLineCost = l.TotalLineCost,
                    PartNumber = l.PartNumber,
                    Category = l.Category,
                    ConfidenceScore = l.ExtractionConfidence
                })
                .ToList()
        })
        .ToListAsync();

    return new PaginatedResult<InvoiceSummaryDto>
    {
        Items = invoices,
        TotalCount = totalCount,
        PageNumber = page,
        PageSize = pageSize
    };
}

    public async Task<PaginatedResult<InvoiceSummaryDto>> GetInvoicesByUploadedDateAsync(DateTime date, int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100); // Enforce [1,100] range
        var skip = (page - 1) * pageSize;

        // Create date range for the entire day
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var query = _context.InvoiceHeaders
            .AsNoTracking()
            .Where(i => i.CreatedAt >= startOfDay && i.CreatedAt < endOfDay && i.Approved)
            .OrderByDescending(i => i.CreatedAt);

        var totalCount = await query.CountAsync();

        var invoices = await query
            .Skip(skip)
            .Take(pageSize)
            .Select(i => new InvoiceSummaryDto
            {
                InvoiceID = i.InvoiceID,
                VehicleID = i.VehicleID,
                Odometer = i.Odometer,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                TotalCost = i.TotalCost,
                TotalPartsCost = i.TotalPartsCost,
                TotalLaborCost = i.TotalLaborCost,
                Description = i.Description,
                ConfidenceScore = i.ConfidenceScore,
                CreatedAt = i.CreatedAt,
                Approved = i.Approved,
                ApprovedAt = i.ApprovedAt,
                ApprovedBy = i.ApprovedBy,
                LineItemCount = i.InvoiceLines.Count(),

                LineItems = i.InvoiceLines
                    .OrderBy(l => l.LineNumber)
                    .Select(l => new InvoiceLineDto
                    {
                        LineID = l.LineID,
                        LineNumber = l.LineNumber,
                        Description = l.Description,
                        UnitCost = l.UnitCost,
                        Quantity = l.Quantity,
                        TotalLineCost = l.TotalLineCost,
                        PartNumber = l.PartNumber,
                        Category = l.Category,
                        ConfidenceScore = l.ExtractionConfidence
                    })
                    .ToList()
            })
            .ToListAsync();

        return new PaginatedResult<InvoiceSummaryDto>
        {
            Items = invoices,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<string> GetSecureFileUrlAsync(int invoiceId, string? userIdentifier = null)
    {
        try
        {
            _logger.LogInformation("Generating secure file URL for Invoice ID: {InvoiceId}, User: {UserIdentifier}", 
                invoiceId, userIdentifier ?? "Anonymous");

            // Get invoice to retrieve blob URL
            var invoice = await _context.InvoiceHeaders
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice {InvoiceId} not found for file access request", invoiceId);
                return string.Empty;
            }

            if (string.IsNullOrEmpty(invoice.BlobFileUrl))
            {
                _logger.LogWarning("Invoice {InvoiceId} has no blob file URL", invoiceId);
                return string.Empty;
            }

            // Generate secure URL with 1-hour expiration (per PRD)
            var secureUrl = await _blobStorageService.GenerateSecureFileUrlAsync(invoice.BlobFileUrl, 1);

            if (!string.IsNullOrEmpty(secureUrl))
            {
                // Log file access for audit purposes (per PRD requirement)
                _logger.LogInformation("File access granted: Invoice {InvoiceId}, User: {UserIdentifier}, BlobUrl: {BlobUrl}", 
                    invoiceId, userIdentifier ?? "Anonymous", invoice.BlobFileUrl);
            }

            return secureUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating secure file URL for Invoice ID: {InvoiceId}", invoiceId);
            return string.Empty;
        }
    }

    public async Task<InvoiceActionResponse> ApproveInvoiceAsync(int invoiceId, string approvedBy)
    {
        var response = new InvoiceActionResponse
        {
            InvoiceId = invoiceId,
            Action = "approved"
        };

        try
        {
            _logger.LogInformation("Attempting to approve Invoice ID: {InvoiceId} by: {ApprovedBy}", invoiceId, approvedBy);

            // Find the invoice
            var invoice = await _context.InvoiceHeaders
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId);

            if (invoice == null)
            {
                response.Success = false;
                response.Message = $"Invoice with ID {invoiceId} not found.";
                _logger.LogWarning("Invoice {InvoiceId} not found for approval", invoiceId);
                return response;
            }

            // Check if invoice is already approved
            if (invoice.Approved)
            {
                response.Success = false;
                response.Message = $"Invoice {invoice.InvoiceNumber} is already approved.";
                response.ActionTimestamp = invoice.ApprovedAt;
                response.ActionBy = invoice.ApprovedBy;
                _logger.LogWarning("Invoice {InvoiceId} is already approved", invoiceId);
                return response;
            }

            // Approve the invoice
            invoice.Approved = true;
            invoice.ApprovedAt = DateTime.UtcNow;
            invoice.ApprovedBy = approvedBy;

            await _context.SaveChangesAsync();

            response.Success = true;
            response.Message = $"Invoice {invoice.InvoiceNumber} has been approved successfully.";
            response.ActionTimestamp = invoice.ApprovedAt;
            response.ActionBy = invoice.ApprovedBy;

            _logger.LogInformation("Invoice {InvoiceId} approved successfully by {ApprovedBy} at {ApprovedAt}", 
                invoiceId, approvedBy, invoice.ApprovedAt);

            return response;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = "An error occurred while approving the invoice.";
            _logger.LogError(ex, "Error approving Invoice ID: {InvoiceId}", invoiceId);
            return response;
        }
    }

    public async Task<InvoiceActionResponse> RejectInvoiceAsync(int invoiceId)
    {
        var response = new InvoiceActionResponse
        {
            InvoiceId = invoiceId,
            Action = "rejected"
        };

        try
        {
            _logger.LogInformation("Attempting to reject (delete) Invoice ID: {InvoiceId}", invoiceId);

            // Start a transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Find the invoice with its line items
                var invoice = await _context.InvoiceHeaders
                    .Include(i => i.InvoiceLines)
                    .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId);

                if (invoice == null)
                {
                    response.Success = false;
                    response.Message = $"Invoice with ID {invoiceId} not found.";
                    _logger.LogWarning("Invoice {InvoiceId} not found for rejection", invoiceId);
                    return response;
                }

                // Store blob URL for cleanup
                var blobUrl = invoice.BlobFileUrl;
                var invoiceNumber = invoice.InvoiceNumber;

                // Delete line items first (due to foreign key constraint)
                if (invoice.InvoiceLines.Any())
                {
                    _context.InvoiceLines.RemoveRange(invoice.InvoiceLines);
                    _logger.LogInformation("Removed {LineItemCount} line items for Invoice {InvoiceId}", 
                        invoice.InvoiceLines.Count, invoiceId);
                }

                // Delete the invoice header
                _context.InvoiceHeaders.Remove(invoice);

                // Save database changes
                await _context.SaveChangesAsync();

                // Commit the transaction
                await transaction.CommitAsync();

                _logger.LogInformation("Invoice {InvoiceId} deleted from database successfully", invoiceId);

                // Delete the file from blob storage
                if (!string.IsNullOrEmpty(blobUrl))
                {
                    try
                    {
                        await _blobStorageService.DeleteFileAsync(blobUrl);
                        _logger.LogInformation("Blob file deleted successfully for Invoice {InvoiceId}: {BlobUrl}", 
                            invoiceId, blobUrl);
                    }
                    catch (Exception blobEx)
                    {
                        _logger.LogWarning(blobEx, "Failed to delete blob file for Invoice {InvoiceId}: {BlobUrl}", 
                            invoiceId, blobUrl);
                        // Don't fail the entire operation if blob deletion fails
                    }
                }

                response.Success = true;
                response.Message = $"Invoice {invoiceNumber} has been rejected and permanently deleted.";
                response.ActionTimestamp = DateTime.UtcNow;

                _logger.LogInformation("Invoice {InvoiceId} rejected and deleted successfully", invoiceId);

                return response;
            }
            catch (Exception)
            {
                // Rollback transaction on error
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = "An error occurred while rejecting the invoice.";
            _logger.LogError(ex, "Error rejecting Invoice ID: {InvoiceId}", invoiceId);
            return response;
        }
    }

    private DataValidationResult ValidateInvoiceData(InvoiceData data)
    {
        var result = new DataValidationResult();

        // Required field validation
        if (string.IsNullOrWhiteSpace(data.VehicleId))
        {
            // For testing purposes, generate a default Vehicle ID if not extracted
            data.VehicleId = $"VEH-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            result.Warnings.Add($"Vehicle ID not found in document, generated default: {data.VehicleId}");
        }

        if (string.IsNullOrWhiteSpace(data.InvoiceNumber))
        {
            // Generate a default invoice number if not extracted
            data.InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}";
            result.Warnings.Add($"Invoice Number not found in document, generated default: {data.InvoiceNumber}");
        }

        if (!data.InvoiceDate.HasValue)
        {
            // Use today's date if not extracted
            data.InvoiceDate = DateTime.Today;
            result.Warnings.Add("Invoice Date not found in document, using today's date");
        }

        if (!data.TotalCost.HasValue || data.TotalCost <= 0)
            result.Errors.Add("Total Cost must be greater than 0");

        // Business rule validation
        if (data.InvoiceDate.HasValue && data.InvoiceDate > DateTime.UtcNow.AddDays(1))
            result.Warnings.Add("Invoice date is in the future");

        if (data.Odometer.HasValue && data.Odometer < 0)
            result.Errors.Add("Odometer reading cannot be negative");

        // Line items validation
        if (!data.LineItems.Any())
            result.Warnings.Add("No line items found in invoice");

        foreach (var line in data.LineItems)
        {
            if (string.IsNullOrWhiteSpace(line.Description))
                result.Errors.Add($"Line {line.LineNumber}: Description is required");

            if (line.UnitCost < 0)
                result.Errors.Add($"Line {line.LineNumber}: Unit cost cannot be negative");

            if (line.Quantity <= 0)
                result.Errors.Add($"Line {line.LineNumber}: Quantity must be greater than 0");

            if (line.TotalCost < 0)
                result.Errors.Add($"Line {line.LineNumber}: Total cost cannot be negative");
        }

        result.IsValid = !result.Errors.Any();
        return result;
    }

    private void UpdateInvoiceDataFromAI(InvoiceData invoiceData, ComprehensiveInvoiceProcessingResult aiResult)
    {
        try
        {
            // Update header-level data from AI result
            if (!string.IsNullOrWhiteSpace(aiResult.Description))
            {
                invoiceData.Description = aiResult.Description;
            }

            // Update normalized fields from AI result
            if (!string.IsNullOrWhiteSpace(aiResult.VehicleId))
                invoiceData.VehicleId = aiResult.VehicleId;
            
            if (!string.IsNullOrWhiteSpace(aiResult.InvoiceNumber))
                invoiceData.InvoiceNumber = aiResult.InvoiceNumber;
            
            if (aiResult.InvoiceDate.HasValue)
                invoiceData.InvoiceDate = aiResult.InvoiceDate.Value;
            
            if (aiResult.Odometer.HasValue)
                invoiceData.Odometer = aiResult.Odometer.Value;
            
            if (aiResult.TotalCost.HasValue)
                invoiceData.TotalCost = aiResult.TotalCost.Value;

            // Update line items with AI classifications
            if (aiResult.LineItems != null)
            {
                foreach (var aiLine in aiResult.LineItems)
                {
                    var matchingLine = invoiceData.LineItems.FirstOrDefault(l => l.LineNumber == aiLine.LineNumber);
                    if (matchingLine != null)
                    {
                        matchingLine.ClassifiedCategory = aiLine.Classification;
                        matchingLine.ClassificationConfidence = aiLine.Confidence;
                        matchingLine.PartNumber = aiLine.PartNumber ?? matchingLine.PartNumber;
                        
                        // Update description if AI provided a better one
                        if (!string.IsNullOrWhiteSpace(aiLine.Description))
                        {
                            matchingLine.Description = aiLine.Description;
                        }
                    }
                }
            }

            _logger.LogInformation("Successfully updated invoice data with AI results. Description: {HasDescription}, Line classifications: {ClassificationCount}", 
                !string.IsNullOrWhiteSpace(aiResult.Description),
                aiResult.LineItems?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice data with AI results");
            // Don't throw - we want processing to continue even if AI updates fail
        }
    }

    private InvoiceHeader CreateInvoiceHeader(InvoiceData data, string blobUrl, decimal confidence, string? rawJson)
    {
        return new InvoiceHeader
        {
            VehicleID = data.VehicleId!,
            Odometer = data.Odometer,
            InvoiceNumber = data.InvoiceNumber!,
            InvoiceDate = data.InvoiceDate!.Value.Date,
            TotalCost = data.TotalCost!.Value,
            TotalPartsCost = data.TotalPartsCost ?? 0,
            TotalLaborCost = data.TotalLaborCost ?? 0,
            Description = data.Description,
            BlobFileUrl = blobUrl,
            ExtractedData = rawJson,
            ConfidenceScore = confidence
        };
    }

    private InvoiceLine CreateInvoiceLine(InvoiceLineData data, int invoiceId)
    {
        return new InvoiceLine
        {
            InvoiceID = invoiceId,
            LineNumber = data.LineNumber,
            Description = data.Description,
            UnitCost = data.UnitCost,
            Quantity = data.Quantity,
            TotalLineCost = data.TotalCost,
            PartNumber = data.PartNumber,
            Category = data.Category,
            ClassifiedCategory = data.ClassifiedCategory ?? "Unclassified",
            ClassificationConfidence = data.ClassificationConfidence,
            ClassificationMethod = !string.IsNullOrEmpty(data.ClassifiedCategory) ? "GPT4o-Enhanced" : "Rule-based",
            ExtractionConfidence = data.ConfidenceScore
        };
    }

    public async Task<object?> GetRawExtractedDataAsync(int invoiceId)
    {
        try
        {
            var invoice = await _context.InvoiceHeaders
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId && i.Approved);

            if (invoice?.ExtractedData == null)
            {
                return null;
            }

            // Parse and return the raw JSON data
            return JsonSerializer.Deserialize<object>(invoice.ExtractedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving raw data for invoice {InvoiceId}", invoiceId);
            return null;
        }
    }

    /// <summary>
    /// Helper method to clean up uploaded blob file in case of processing failure
    /// </summary>
    private async Task CleanupBlobFileAsync(string blobUrl, string reason)
    {
        if (string.IsNullOrEmpty(blobUrl))
            return;

        try
        {
            var uri = new Uri(blobUrl);
            var blobFileName = uri.Segments.Last();
            
            var deletionSuccess = await _blobStorageService.DeleteFileAsync(blobFileName);
            if (deletionSuccess)
            {
                _logger.LogInformation("Successfully cleaned up blob file after {Reason}: {BlobFileName}", reason, blobFileName);
            }
            else
            {
                _logger.LogWarning("Failed to delete blob file after {Reason}: {BlobFileName}", reason, blobFileName);
            }
        }
        catch (Exception blobEx)
        {
            _logger.LogError(blobEx, "Error during blob cleanup after {Reason}: {BlobUrl}", reason, blobUrl);
        }
    }

    /// <summary>
    /// Apply GPT-4o AI enhancement to invoice data for improved accuracy and data quality
    /// </summary>
    private Task<AIEnhancementResult> ApplyAIEnhancementAsync(InvoiceHeader invoiceWithLines, string? rawFormRecognizerJson)
    {
        var result = new AIEnhancementResult();

        try
        {
            if (string.IsNullOrEmpty(rawFormRecognizerJson))
            {
                result.Success = false;
                result.ErrorMessage = "No raw Form Recognizer data available for AI enhancement";
                return Task.FromResult(result);
            }

            _logger.LogInformation("Starting GPT-4o enhancement for Invoice {InvoiceId} with {LineCount} lines", 
                invoiceWithLines.InvoiceID, invoiceWithLines.InvoiceLines?.Count ?? 0);

            // Step 1: Comprehensive invoice enhancement using GPT-4o
            // Old AI enhancement removed - using comprehensive processing instead
            /*
            if (!enhancementResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = enhancementResult.ErrorMessage;
                return result;
            }

            result.OverallConfidence = enhancementResult.ConfidenceScore;
            result.DataQualityImprovements.AddRange(enhancementResult.Improvements);
            */

            // Step 2: Extract and enhance part numbers for each line item
            if (invoiceWithLines.InvoiceLines?.Any() == true)
            {
                foreach (var line in invoiceWithLines.InvoiceLines)
                {
                    var enhancedLine = new EnhancedLineItem
                    {
                        LineId = line.LineID,
                        OriginalDescription = line.Description
                    };

                    try
                    {
                        // Use GPT-4o to extract part numbers from the line description
                        if (!string.IsNullOrEmpty(line.Description) && 
                            (line.ClassifiedCategory?.ToLower().Contains("part") == true || string.IsNullOrEmpty(line.ClassifiedCategory)))
                        {
                            var partExtractionPrompt = @"Extract automotive part numbers from this invoice line item text using expert automotive knowledge:

INSTRUCTIONS:
1. Identify ALL part numbers using OEM-specific patterns (Honda: 12345-ABC-123, Toyota: 90210-54321, Ford: F1XZ-1234-AB, etc.)
2. Look for cross-references and alternative part numbers
3. Validate using surrounding context
4. Return only the most likely primary part number
5. If no part number found, return empty string

Return ONLY the part number, no explanation.

TEXT: " + line.Description;

                            /* Old GPT calls removed
                            var partNumberResult = await _gitHubModelsService.ProcessInvoiceTextAsync(line.Description, partExtractionPrompt);
                            */
                            string partNumberResult = ""; // Placeholder for removed functionality
                            
                            if (!string.IsNullOrEmpty(partNumberResult) && 
                                partNumberResult != "GPT-4o Integration Error" &&
                                !partNumberResult.Contains("Error") &&
                                IsValidPartNumber(partNumberResult))
                            {
                                enhancedLine.AIPartNumber = partNumberResult.Trim();
                                enhancedLine.AIConfidence = 0.90; // High confidence for GPT-4o part extraction
                            }
                        }

                        // Use GPT-4o to enhance service classification
                        if (!string.IsNullOrEmpty(line.Description))
                        {
                            var classificationPrompt = @"Classify this automotive service line item into ONE of these categories:

CATEGORIES: Oil Change, Brake Service, Engine Repair, Transmission Work, Electrical Repair, Tire Service, Battery Service, Air Filter, Fuel System, Cooling System, Suspension, Steering, Parts, Labor, Fee, Tax, Other

INSTRUCTIONS:
1. Analyze the description for automotive service type
2. Choose the most specific applicable category
3. Return ONLY the category name, no explanation

DESCRIPTION: " + line.Description;

                            /* Old GPT calls removed
                            var categoryResult = await _gitHubModelsService.ProcessInvoiceTextAsync(line.Description, classificationPrompt);
                            */
                            string categoryResult = ""; // Placeholder for removed functionality
                            
                            if (!string.IsNullOrEmpty(categoryResult) && 
                                categoryResult != "GPT-4o Integration Error" &&
                                !categoryResult.Contains("Error"))
                            {
                                var cleanCategory = categoryResult.Trim().Replace("\"", "").Replace("'", "");
                                enhancedLine.AIServiceCategory = cleanCategory;
                                enhancedLine.AIConfidence = Math.Max(enhancedLine.AIConfidence, 0.85);
                            }
                        }
                    }
                    catch (Exception lineEx)
                    {
                        _logger.LogWarning(lineEx, "AI enhancement failed for line {LineId}: {Description}", line.LineID, line.Description);
                        enhancedLine.AIConfidence = 0.0;
                    }

                    if (enhancedLine.HasEnhancements)
                    {
                        result.EnhancedLineItems.Add(enhancedLine);
                    }
                }
            }

            result.Success = true;
            _logger.LogInformation("GPT-4o AI enhancement completed for Invoice {InvoiceId}. Enhanced {Count} line items with average confidence {Confidence:F2}", 
                invoiceWithLines.InvoiceID, result.EnhancedLineItems.Count, result.OverallConfidence);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GPT-4o AI enhancement for Invoice {InvoiceId}", invoiceWithLines.InvoiceID);
            result.Success = false;
            result.ErrorMessage = $"AI enhancement error: {ex.Message}";
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Validate if a string looks like a genuine automotive part number
    /// </summary>
    private bool IsValidPartNumber(string partNumber)
    {
        if (string.IsNullOrWhiteSpace(partNumber) || partNumber.Length < 3)
            return false;

        // Must contain at least one letter or number
        if (!partNumber.Any(char.IsLetterOrDigit))
            return false;

        // Must not be purely generic text
        var lowerPart = partNumber.ToLower();
        if (lowerPart.Contains("part number") || lowerPart.Contains("no part") || lowerPart.Contains("not found"))
            return false;

        // Common automotive part number patterns
        return partNumber.Length <= 30 && // Reasonable length
               (partNumber.Any(char.IsDigit) || partNumber.Any(char.IsLetter)) &&
               !partNumber.All(char.IsWhiteSpace);
    }
}

/// <summary>
/// Result of data validation
/// </summary>
public class DataValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Result of GPT-4o AI enhancement processing
/// </summary>
public class AIEnhancementResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = "";
    public double OverallConfidence { get; set; }
    public List<EnhancedLineItem> EnhancedLineItems { get; set; } = new();
    public List<string> DataQualityImprovements { get; set; } = new();
}

/// <summary>
/// Represents an invoice line item enhanced by GPT-4o AI
/// </summary>
public class EnhancedLineItem
{
    public int LineId { get; set; }
    public string OriginalDescription { get; set; } = "";
    public string? AIPartNumber { get; set; }
    public string? AIServiceCategory { get; set; }
    public double AIConfidence { get; set; }
    
    public bool HasEnhancements => !string.IsNullOrEmpty(AIPartNumber) || !string.IsNullOrEmpty(AIServiceCategory);
}
