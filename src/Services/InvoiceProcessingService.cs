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
    Task<InvoiceDetailsDto?> GetInvoiceByIdAsync(int invoiceId);
    Task<PaginatedResult<InvoiceSummaryDto>> GetInvoicesByVehicleAsync(string vehicleId, int page = 1, int pageSize = 20);
    Task<PaginatedResult<InvoiceSummaryDto>> GetInvoicesByDateAsync(DateTime date, int page = 1, int pageSize = 20);
    Task<string> GetSecureFileUrlAsync(int invoiceId, string? userIdentifier = null);
}

/// <summary>
/// Service for invoice processing and data operations
/// </summary>
public class InvoiceProcessingService : IInvoiceProcessingService
{
    private readonly InvoiceDbContext _context;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IFormRecognizerService _formRecognizerService;
    private readonly ILogger<InvoiceProcessingService> _logger;

    public InvoiceProcessingService(
        InvoiceDbContext context,
        IBlobStorageService blobStorageService,
        IFormRecognizerService formRecognizerService,
        ILogger<InvoiceProcessingService> logger)
    {
        _context = context;
        _blobStorageService = blobStorageService;
        _formRecognizerService = formRecognizerService;
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

            // Step 2: OCR Processing with Form Recognizer
            FormRecognizerResult ocrResult;
            using (var stream = file.OpenReadStream())
            {
                ocrResult = await _formRecognizerService.AnalyzeInvoiceAsync(stream);
            }

            if (!ocrResult.Success || ocrResult.InvoiceData == null)
            {
                response.Success = false;
                response.Message = "OCR processing failed";
                response.Errors.Add($"Form Recognizer error: {ocrResult.ErrorMessage}");
                return response;
            }

            _logger.LogInformation("OCR processing completed with confidence: {Confidence}%", ocrResult.OverallConfidence);

            // Step 3: Validate extracted data
            var validationResult = ValidateInvoiceData(ocrResult.InvoiceData);
            if (!validationResult.IsValid)
            {
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
                await _context.SaveChangesAsync();

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
        pageSize = Math.Min(pageSize, 100); // Limit page size
        var skip = (page - 1) * pageSize;

        var query = _context.InvoiceHeaders
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt);

        var totalCount = await query.CountAsync();
        
        var invoices = await query
            .Skip(skip)
            .Take(pageSize)
            .Select(i => new InvoiceSummaryDto
            {
                InvoiceID = i.InvoiceID,
                VehicleID = i.VehicleID,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                TotalCost = i.TotalCost,
                TotalPartsCost = i.TotalPartsCost,
                TotalLaborCost = i.TotalLaborCost,
                ConfidenceScore = i.ConfidenceScore,
                CreatedAt = i.CreatedAt,
                LineItemCount = i.InvoiceLines.Count()
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

    public async Task<InvoiceDetailsDto?> GetInvoiceByIdAsync(int invoiceId)
    {
        try
        {
            _logger.LogInformation("Starting GetInvoiceByIdAsync for Invoice ID: {InvoiceId}", invoiceId);
            
            _logger.LogInformation("Step 1: Querying database for Invoice ID: {InvoiceId}", invoiceId);
            var invoice = await _context.InvoiceHeaders
                .AsNoTracking()
                .Include(i => i.InvoiceLines)
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId);

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
            BlobFileUrl = invoice.BlobFileUrl,
            ConfidenceScore = invoice.ConfidenceScore,
            CreatedAt = invoice.CreatedAt
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
                        Category = l.Category,
                        ConfidenceScore = l.ConfidenceScore
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
        pageSize = Math.Min(pageSize, 100);
        var skip = (page - 1) * pageSize;

        var query = _context.InvoiceHeaders
            .AsNoTracking()
            .Where(i => i.VehicleID == vehicleId)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.CreatedAt);

        var totalCount = await query.CountAsync();
        
        var invoices = await query
            .Skip(skip)
            .Take(pageSize)
            .Select(i => new InvoiceSummaryDto
            {
                InvoiceID = i.InvoiceID,
                VehicleID = i.VehicleID,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                TotalCost = i.TotalCost,
                TotalPartsCost = i.TotalPartsCost,
                TotalLaborCost = i.TotalLaborCost,
                ConfidenceScore = i.ConfidenceScore,
                CreatedAt = i.CreatedAt,
                LineItemCount = i.InvoiceLines.Count()
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
        pageSize = Math.Min(pageSize, 100);
        var skip = (page - 1) * pageSize;

        var startDate = date.Date;
        var endDate = startDate.AddDays(1);

        var query = _context.InvoiceHeaders
            .AsNoTracking()
            .Where(i => i.CreatedAt >= startDate && i.CreatedAt < endDate)
            .OrderByDescending(i => i.CreatedAt);

        var totalCount = await query.CountAsync();
        
        var invoices = await query
            .Skip(skip)
            .Take(pageSize)
            .Select(i => new InvoiceSummaryDto
            {
                InvoiceID = i.InvoiceID,
                VehicleID = i.VehicleID,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                TotalCost = i.TotalCost,
                TotalPartsCost = i.TotalPartsCost,
                TotalLaborCost = i.TotalLaborCost,
                ConfidenceScore = i.ConfidenceScore,
                CreatedAt = i.CreatedAt,
                LineItemCount = i.InvoiceLines.Count()
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

    private DataValidationResult ValidateInvoiceData(InvoiceData data)
    {
        var result = new DataValidationResult();

        // Required field validation
        if (string.IsNullOrWhiteSpace(data.VehicleId))
        {
            // For testing purposes, generate a default Vehicle ID if not extracted
            data.VehicleId = $"VEH-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            result.Warnings.Add($"Vehicle ID not found in document, generated default: {data.VehicleId}");
        }

        if (string.IsNullOrWhiteSpace(data.InvoiceNumber))
        {
            // Generate a default invoice number if not extracted
            data.InvoiceNumber = $"INV-{DateTime.Now:yyyyMMddHHmmss}";
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
        if (data.InvoiceDate.HasValue && data.InvoiceDate > DateTime.Now.AddDays(1))
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
            BlobFileUrl = blobUrl,
            ExtractedData = rawJson,
            ConfidenceScore = confidence,
            CreatedAt = DateTime.Now
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
            ConfidenceScore = data.ConfidenceScore,
            CreatedAt = DateTime.Now
        };
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
