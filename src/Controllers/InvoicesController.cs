using Microsoft.AspNetCore.Mvc;
using VehicleMaintenanceInvoiceSystem.Models;
using VehicleMaintenanceInvoiceSystem.Services;

namespace VehicleMaintenanceInvoiceSystem.Controllers;

/// <summary>
/// API Controller for invoice operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceProcessingService _invoiceService;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(IInvoiceProcessingService invoiceService, ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Upload and process a new invoice file
    /// </summary>
    /// <param name="file">PDF or PNG invoice file (max 10MB)</param>
    /// <returns>Processing result with invoice ID</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(InvoiceProcessingResponse), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> UploadInvoice(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            _logger.LogInformation("Processing upload for file: {FileName} ({FileSize} bytes)", 
                file.FileName, file.Length);

            var result = await _invoiceService.ProcessInvoiceAsync(file);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file upload: {FileName}", file?.FileName);
            return StatusCode(500, "An error occurred while processing the file");
        }
    }

    /// <summary>
    /// Retrieve all invoices with pagination
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <returns>Paginated list of invoice summaries</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<InvoiceSummaryDto>), 200)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<IActionResult> GetInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1)
            {
                return BadRequest("Page number must be greater than 0");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100");
            }

            var result = await _invoiceService.GetInvoicesAsync(page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices");
            return StatusCode(500, "An error occurred while retrieving invoices");
        }
    }

    /// <summary>
    /// Retrieve processed invoices (approved and unapproved) with pagination
    /// Mirrors /api/Invoices but includes optional status filter
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="status">Filter by status: all (default), approved, unapproved</param>
    /// <returns>Paginated list of invoice summaries</returns>
    [HttpGet("processed-list")]
    [ProducesResponseType(typeof(PaginatedResult<InvoiceSummaryDto>), 200)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<IActionResult> GetProcessedInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = "all")
    {
        try
        {
            if (page < 1)
            {
                return BadRequest("Page number must be greater than 0");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100");
            }

            var result = await _invoiceService.GetProcessedInvoicesAsync(page, pageSize, status);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving processed invoices");
            return StatusCode(500, "An error occurred while retrieving processed invoices");
        }
    }

    /// <summary>
    /// Retrieve a specific invoice with all line items
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <returns>Complete invoice details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InvoiceDetailsDto), 200)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> GetInvoiceById(int id)
    {
        try
        {
            // Return invoice regardless of approval status (single-invoice endpoint)
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id, includeUnapproved: true);
            
            if (invoice == null)
            {
                return NotFound($"Invoice with ID {id} not found");
            }

            return Ok(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice {InvoiceId}", id);
            return StatusCode(500, "An error occurred while retrieving the invoice");
        }
    }

    /// <summary>
    /// Search invoices by vehicle ID
    /// </summary>
    /// <param name="vehicleId">Vehicle identifier</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <returns>Paginated list of invoices for the vehicle</returns>
    [HttpGet("vehicle/{vehicleId}")]
    [ProducesResponseType(typeof(PaginatedResult<InvoiceSummaryDto>), 200)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<IActionResult> GetInvoicesByVehicle(string vehicleId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(vehicleId))
            {
                return BadRequest("Vehicle ID is required");
            }

            if (page < 1)
            {
                return BadRequest("Page number must be greater than 0");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100");
            }

            var result = await _invoiceService.GetInvoicesByVehicleAsync(vehicleId, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices for vehicle {VehicleId}", vehicleId);
            return StatusCode(500, "An error occurred while retrieving invoices");
        }
    }

    /// <summary>
    /// Retrieve invoices by invoice date (the date on the invoice document)
    /// </summary>
    /// <param name="date">Date in YYYY-MM-DD format</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <returns>Paginated list of invoices for the date</returns>
    [HttpGet("date/{date:datetime}")]
    [ProducesResponseType(typeof(PaginatedResult<InvoiceSummaryDto>), 200)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<IActionResult> GetInvoicesByDate(DateTime date, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1)
            {
                return BadRequest("Page number must be greater than 0");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100");
            }

            var result = await _invoiceService.GetInvoicesByDateAsync(date, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices for date {Date}", date);
            return StatusCode(500, "An error occurred while retrieving invoices");
        }
    }

    /// <summary>
    /// Retrieve invoices by upload date (CreatedAt, when the system processed the invoice)
    /// </summary>
    /// <param name="date">Date in YYYY-MM-DD format</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <returns>Paginated list of invoices uploaded on the date</returns>
    [HttpGet("uploaded-date/{date:datetime}")]
    [ProducesResponseType(typeof(PaginatedResult<InvoiceSummaryDto>), 200)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<IActionResult> GetInvoicesByUploadedDate(DateTime date, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1)
            {
                return BadRequest("Page number must be greater than 0");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100");
            }

            var result = await _invoiceService.GetInvoicesByUploadedDateAsync(date, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices for uploaded date {Date}", date);
            return StatusCode(500, "An error occurred while retrieving invoices");
        }
    }

    /// <summary>
    /// Access original invoice file from blob storage
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <returns>Secure redirect to blob storage URL</returns>
    [HttpGet("{id:int}/file")]
    [ProducesResponseType(302)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> GetInvoiceFile(int id)
    {
        try
        {
            _logger.LogInformation("File access requested for Invoice ID: {InvoiceId}", id);

            // Get user identifier for audit logging (could be enhanced with authentication)
            var userIdentifier = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var secureUrl = await _invoiceService.GetSecureFileUrlAsync(id, userIdentifier);

            if (string.IsNullOrEmpty(secureUrl))
            {
                return NotFound($"Invoice file not found for ID {id}");
            }

            _logger.LogInformation("Redirecting to secure file URL for Invoice ID: {InvoiceId}", id);

            // Return redirect to secure blob URL (per PRD requirement)
            return Redirect(secureUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing file for invoice {InvoiceId}", id);
            return StatusCode(500, "An error occurred while accessing the invoice file");
        }
    }

    /// <summary>
    /// Approve an invoice for payment
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="request">Approval request with approver information</param>
    /// <returns>Approval confirmation</returns>
    [HttpPut("{id:int}/approve")]
    [ProducesResponseType(typeof(InvoiceActionResponse), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> ApproveInvoice(int id, [FromBody] ApproveInvoiceRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request?.ApprovedBy))
            {
                return BadRequest("ApprovedBy field is required");
            }

            _logger.LogInformation("Approval requested for Invoice ID: {InvoiceId} by: {ApprovedBy}", id, request.ApprovedBy);

            var result = await _invoiceService.ApproveInvoiceAsync(id, request.ApprovedBy);

            if (result.Success)
            {
                _logger.LogInformation("Invoice {InvoiceId} approved successfully", id);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Failed to approve Invoice {InvoiceId}: {Message}", id, result.Message);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving invoice {InvoiceId}", id);
            return StatusCode(500, "An error occurred while approving the invoice");
        }
    }

    /// <summary>
    /// Reject an invoice (permanently delete it)
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <returns>Rejection confirmation</returns>
    [HttpDelete("{id:int}/reject")]
    [ProducesResponseType(typeof(InvoiceActionResponse), 200)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> RejectInvoice(int id)
    {
        try
        {
            _logger.LogInformation("Rejection requested for Invoice ID: {InvoiceId}", id);

            var result = await _invoiceService.RejectInvoiceAsync(id);

            if (result.Success)
            {
                _logger.LogInformation("Invoice {InvoiceId} rejected and deleted successfully", id);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Failed to reject Invoice {InvoiceId}: {Message}", id, result.Message);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting invoice {InvoiceId}", id);
            return StatusCode(500, "An error occurred while rejecting the invoice");
        }
    }

    /// <summary>
    /// Debug endpoint to examine raw ExtractedData for an invoice
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <returns>Raw Form Recognizer data</returns>
    [HttpGet("{id:int}/debug")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> GetInvoiceDebugData(int id)
    {
        try
        {
            var debugData = await _invoiceService.GetRawExtractedDataAsync(id);
            
            if (debugData == null)
            {
                return NotFound($"Invoice with ID {id} not found");
            }

            return Ok(debugData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving debug data for invoice {InvoiceId}", id);
            return StatusCode(500, "An error occurred while retrieving debug data");
        }
    }
}
