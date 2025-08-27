using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using VehicleMaintenanceInvoiceSystem.Controllers;
using VehicleMaintenanceInvoiceSystem.Models;
using VehicleMaintenanceInvoiceSystem.Services;
using Xunit;

namespace VehicleMaintenanceInvoiceSystem.Tests;

public class InvoicesControllerTests
{
    [Fact]
    public async Task UploadInvoice_NoFile_Returns_BadRequest()
    {
        var svc = Substitute.For<IInvoiceProcessingService>();
        var controller = new InvoicesController(svc, NullLogger<InvoicesController>.Instance);
        var result = await controller.UploadInvoice(null!);
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("No file uploaded");
    }

    [Fact]
    public async Task UploadInvoice_ValidFile_Returns_Ok_On_Success()
    {
        var svc = Substitute.For<IInvoiceProcessingService>();
        svc.ProcessInvoiceAsync(Arg.Any<IFormFile>())
            .Returns(new InvoiceProcessingResponse { Success = true, InvoiceId = 123 });

        var controller = new InvoicesController(svc, NullLogger<InvoicesController>.Instance);

        var file = new FormFile(new MemoryStream(new byte[]{1,2,3}), 0, 3, "file", "test.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var result = await controller.UploadInvoice(file);
        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        ok!.Value.Should().BeOfType<InvoiceProcessingResponse>();
        (ok.Value as InvoiceProcessingResponse)!.Success.Should().BeTrue();
    }
}
