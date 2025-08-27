using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using VehicleMaintenanceInvoiceSystem.Controllers;
using VehicleMaintenanceInvoiceSystem.Services;
using Xunit;

namespace VehicleMaintenanceInvoiceSystem.Tests;

public class LLMControllerTests
{
    [Fact]
    public async Task TestConnection_Returns_Ok_With_Response()
    {
        var svc = Substitute.For<IGitHubModelsService>();
        svc.ProcessInvoiceTextAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns("{\"status\":\"connected\"}");
        var controller = new LLMController(svc, NullLogger<LLMController>.Instance);
        var res = await controller.TestConnection() as OkObjectResult;
        res.Should().NotBeNull();
        res!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task EnhanceInvoice_Returns_BadRequest_On_Service_Error()
    {
        var svc = Substitute.For<IGitHubModelsService>();
        svc.EnhanceInvoiceDataAsync(Arg.Any<string>())
            .Returns(new InvoiceEnhancementResult { Success = false, ErrorMessage = "err" });
        var controller = new LLMController(svc, NullLogger<LLMController>.Instance);
        var res = await controller.EnhanceInvoice(new EnhanceInvoiceRequest { InvoiceData = "data" });
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ExtractParts_Returns_Ok()
    {
        var svc = Substitute.For<IGitHubModelsService>();
        svc.ProcessInvoiceTextAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns("[]");
        var controller = new LLMController(svc, NullLogger<LLMController>.Instance);
        var res = await controller.ExtractPartNumbers(new ExtractPartsRequest { InvoiceText = "x" });
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void TestPromptSize_Returns_Analysis()
    {
        var controller = new LLMController(Substitute.For<IGitHubModelsService>(), NullLogger<LLMController>.Instance);
        var res = controller.TestPromptSize(new TestPromptRequest());
        res.Should().BeOfType<OkObjectResult>();
    }
}
