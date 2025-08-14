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
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            
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
    /// Retrieve invoices processed on a specific date
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
}
