using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using VehicleMaintenanceInvoiceSystem.Models;

namespace VehicleMaintenanceInvoiceSystem.Services;

/// <summary>
/// Interface for blob storage operations
/// </summary>
public interface IBlobStorageService
{
    Task<FileUploadResponse> UploadFileAsync(IFormFile file, string fileName);
    Task<Stream?> DownloadFileAsync(string fileName);
    Task<bool> DeleteFileAsync(string fileName);
    Task<string> GetFileUrlAsync(string fileName);
}

/// <summary>
/// Service for Azure Blob Storage operations
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobStorageOptions _options;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IOptions<BlobStorageOptions> options, ILogger<BlobStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _blobServiceClient = new BlobServiceClient(_options.ConnectionString);
    }

    public async Task<FileUploadResponse> UploadFileAsync(IFormFile file, string fileName)
    {
        try
        {
            // Validate file size
            if (file.Length > _options.MaxFileSizeMB * 1024 * 1024)
            {
                return new FileUploadResponse
                {
                    Success = false,
                    Message = $"File size exceeds maximum allowed size of {_options.MaxFileSizeMB}MB"
                };
            }

            // Validate file type
            var allowedExtensions = new[] { ".pdf", ".png" };
            var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return new FileUploadResponse
                {
                    Success = false,
                    Message = "Only PDF and PNG files are allowed"
                };
            }

            // Get container client
            var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            // Set content type
            var contentType = fileExtension switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            // Upload file
            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });

            _logger.LogInformation("File {FileName} uploaded successfully as {UniqueFileName}", fileName, uniqueFileName);

            return new FileUploadResponse
            {
                Success = true,
                Message = "File uploaded successfully",
                BlobUrl = blobClient.Uri.ToString(),
                FileName = uniqueFileName,
                FileSize = file.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", fileName);
            return new FileUploadResponse
            {
                Success = false,
                Message = "An error occurred while uploading the file"
            };
        }
    }

    public async Task<Stream?> DownloadFileAsync(string fileName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (await blobClient.ExistsAsync())
            {
                var response = await blobClient.DownloadStreamingAsync();
                return response.Value.Content;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileName}", fileName);
            return null;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var response = await blobClient.DeleteIfExistsAsync();
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileName}", fileName);
            return false;
        }
    }

    public async Task<string> GetFileUrlAsync(string fileName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (await blobClient.ExistsAsync())
            {
                return blobClient.Uri.ToString();
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file URL for {FileName}", fileName);
            return string.Empty;
        }
    }
}
