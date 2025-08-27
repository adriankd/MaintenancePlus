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

public class InvoicesControllerMoreTests
{
    private InvoicesController Create(out IInvoiceProcessingService svc)
    {
        svc = Substitute.For<IInvoiceProcessingService>();
        return new InvoicesController(svc, NullLogger<InvoicesController>.Instance);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetInvoices_Validates_Pagination(int page, int pageSize)
    {
        var controller = Create(out var svc);
        var res = await controller.GetInvoices(page, pageSize);
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetInvoices_Ok_On_Service_Result()
    {
        var controller = Create(out var svc);
    svc.GetInvoicesAsync(1, 20).Returns(new PaginatedResult<InvoiceSummaryDto>{ Items = new(), PageNumber=1, PageSize=20, TotalCount=0});
        var res = await controller.GetInvoices(1,20) as OkObjectResult;
        res.Should().NotBeNull();
    }

    [Fact]
    public async Task GetInvoiceById_NotFound_When_Null()
    {
        var controller = Create(out var svc);
        svc.GetInvoiceByIdAsync(123).Returns((InvoiceDetailsDto?)null);
        var res = await controller.GetInvoiceById(123);
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetInvoiceById_Ok_When_Present()
    {
        var controller = Create(out var svc);
        svc.GetInvoiceByIdAsync(1).Returns(new InvoiceDetailsDto());
        var res = await controller.GetInvoiceById(1);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetInvoicesByVehicle_Validates_Input()
    {
        var controller = Create(out var _);
        var res = await controller.GetInvoicesByVehicle("", 1, 20);
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetInvoicesByVehicle_Ok()
    {
        var controller = Create(out var svc);
    svc.GetInvoicesByVehicleAsync("V1", 1, 20).Returns(new PaginatedResult<InvoiceSummaryDto>{ Items=new(), PageNumber=1, PageSize=20, TotalCount=0});
        var res = await controller.GetInvoicesByVehicle("V1",1,20);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetInvoicesByDate_Ok()
    {
        var controller = Create(out var svc);
    svc.GetInvoicesByDateAsync(Arg.Any<DateTime>(),1,20).Returns(new PaginatedResult<InvoiceSummaryDto>{ Items=new(), PageNumber=1, PageSize=20, TotalCount=0});
        var res = await controller.GetInvoicesByDate(DateTime.UtcNow,1,20);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetInvoicesByUploadedDate_Ok()
    {
        var controller = Create(out var svc);
    svc.GetInvoicesByUploadedDateAsync(Arg.Any<DateTime>(),1,20).Returns(new PaginatedResult<InvoiceSummaryDto>{ Items=new(), PageNumber=1, PageSize=20, TotalCount=0});
        var res = await controller.GetInvoicesByUploadedDate(DateTime.UtcNow,1,20);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetInvoiceFile_Redirect_When_Url()
    {
        var controller = Create(out var svc);
    controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        svc.GetSecureFileUrlAsync(1, Arg.Any<string?>()).Returns("https://example/");
        var res = await controller.GetInvoiceFile(1);
        res.Should().BeOfType<RedirectResult>();
    }

    [Fact]
    public async Task GetInvoiceFile_NotFound_When_Missing()
    {
        var controller = Create(out var svc);
    controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        svc.GetSecureFileUrlAsync(1, Arg.Any<string?>()).Returns((string?)null);
        var res = await controller.GetInvoiceFile(1);
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ApproveInvoice_Validates_Input()
    {
        var controller = Create(out var _);
        var res = await controller.ApproveInvoice(1, new ApproveInvoiceRequest{ ApprovedBy = "" });
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ApproveInvoice_Returns_Ok_On_Success()
    {
        var controller = Create(out var svc);
        svc.ApproveInvoiceAsync(1, "me").Returns(new InvoiceActionResponse{ Success = true });
        var res = await controller.ApproveInvoice(1, new ApproveInvoiceRequest{ ApprovedBy = "me"});
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ApproveInvoice_Returns_BadRequest_On_Failure()
    {
        var controller = Create(out var svc);
        svc.ApproveInvoiceAsync(1, "me").Returns(new InvoiceActionResponse{ Success = false, Message="x" });
        var res = await controller.ApproveInvoice(1, new ApproveInvoiceRequest{ ApprovedBy = "me"});
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RejectInvoice_Ok_On_Success()
    {
        var controller = Create(out var svc);
        svc.RejectInvoiceAsync(1).Returns(new InvoiceActionResponse{ Success = true });
        var res = await controller.RejectInvoice(1);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RejectInvoice_BadRequest_On_Failure()
    {
        var controller = Create(out var svc);
        svc.RejectInvoiceAsync(1).Returns(new InvoiceActionResponse{ Success = false, Message="oops" });
        var res = await controller.RejectInvoice(1);
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetInvoiceDebugData_NotFound_When_Null()
    {
        var controller = Create(out var svc);
        svc.GetRawExtractedDataAsync(999).Returns((object?)null);
        var res = await controller.GetInvoiceDebugData(999);
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetInvoiceDebugData_Ok_When_Object()
    {
        var controller = Create(out var svc);
        svc.GetRawExtractedDataAsync(1).Returns(new { ok = true });
        var res = await controller.GetInvoiceDebugData(1);
        res.Should().BeOfType<OkObjectResult>();
    }
}
