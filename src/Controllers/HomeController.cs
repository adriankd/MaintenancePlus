using Microsoft.AspNetCore.Mvc;
using VehicleMaintenanceInvoiceSystem.Services;

namespace VehicleMaintenanceInvoiceSystem.Controllers;

/// <summary>
/// MVC Controller for web interface
/// </summary>
public class HomeController : Controller
{
    private readonly IInvoiceProcessingService _invoiceService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IInvoiceProcessingService invoiceService, ILogger<HomeController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Home page with file upload interface. This is a test comment.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            // Get KPI data for dashboard
            var allInvoices = await _invoiceService.GetInvoicesAsync(1, 1000); // Get all invoices for KPIs
            var approvedCount = allInvoices.Items.Count(i => i.Approved);
            var pendingCount = allInvoices.Items.Count(i => !i.Approved);
            var totalValue = allInvoices.Items.Sum(i => i.TotalCost);

            ViewBag.TotalInvoices = allInvoices.TotalCount;
            ViewBag.ApprovedInvoices = approvedCount;
            ViewBag.PendingInvoices = pendingCount;
            ViewBag.TotalValue = totalValue.ToString("N0");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load KPI data for dashboard");
            // Set default values if KPI data fails to load
            ViewBag.TotalInvoices = "N/A";
            ViewBag.ApprovedInvoices = "N/A";
            ViewBag.PendingInvoices = "N/A";
            ViewBag.TotalValue = "N/A";
        }

        return View();
    }

    /// <summary>
    /// Display list of processed invoices
    /// </summary>
    public async Task<IActionResult> Invoices(int page = 1)
    {
        try
        {
            // Use processed-list behavior (includes approved and unapproved)
            var result = await _invoiceService.GetProcessedInvoicesAsync(page, 20, "all");
            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading invoices page");
            ViewBag.Error = "An error occurred while loading invoices";
            return View();
        }
    }

    /// <summary>
    /// Display invoice details
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            _logger.LogInformation("Attempting to load invoice details for ID: {InvoiceId}", id);
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            
            if (invoice == null)
            {
                _logger.LogWarning("Invoice with ID {InvoiceId} not found", id);
                return NotFound();
            }
            
            _logger.LogInformation("Successfully loaded invoice details for ID: {InvoiceId}", id);
            return View(invoice);
        }
        catch (InvalidCastException castEx)
        {
            _logger.LogError(castEx, "CASTING ERROR for Invoice ID {InvoiceId}: {ErrorMessage}. This suggests database column type mismatch (likely INT columns that should be DECIMAL)", id, castEx.Message);
            ViewBag.Error = $"Database schema error for Invoice {id}: {castEx.Message}. Please check if decimal columns are stored as integers.";
            ViewBag.ErrorDetails = castEx.ToString();
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GENERAL ERROR loading invoice details for ID: {InvoiceId}. Error Type: {ErrorType}, Message: {ErrorMessage}", id, ex.GetType().Name, ex.Message);
            ViewBag.Error = $"Error loading invoice {id}: {ex.Message}";
            ViewBag.ErrorDetails = ex.ToString();
            return View();
        }
    }

    /// <summary>
    /// Handle file upload from web interface
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        _logger.LogInformation("Upload action called with file: {FileName}, Size: {FileSize}", 
            file?.FileName ?? "null", file?.Length ?? 0);
        
        try
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file provided in upload request");
                ViewBag.Error = "Please select a file to upload";
                return View("Index");
            }

            var result = await _invoiceService.ProcessInvoiceAsync(file);
            
            if (result.Success)
            {
                ViewBag.Success = $"Invoice processed successfully! Invoice ID: {result.InvoiceId}";
                ViewBag.InvoiceId = result.InvoiceId;
                ViewBag.Confidence = result.ConfidenceScore;
                
                if (result.Warnings.Any())
                {
                    ViewBag.Warnings = result.Warnings;
                }
            }
            else
            {
                ViewBag.Error = result.Message;
                ViewBag.Errors = result.Errors;
            }

            return View("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file upload: {FileName}", file?.FileName);
            ViewBag.Error = "An unexpected error occurred while processing the file";
            return View("Index");
        }
    }

    /// <summary>
    /// Test action to verify routing
    /// </summary>
    [HttpPost]
    public IActionResult TestUpload()
    {
        _logger.LogInformation("TestUpload action called");
        return Json(new { message = "TestUpload action reached successfully", timestamp = DateTime.UtcNow });
    }
}
