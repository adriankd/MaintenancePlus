using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using VehicleMaintenanceInvoiceSystem.Controllers;
using VehicleMaintenanceInvoiceSystem.Data;
using VehicleMaintenanceInvoiceSystem.Models;
using VehicleMaintenanceInvoiceSystem.Services;
using Xunit;

namespace VehicleMaintenanceInvoiceSystem.Tests;

public class IntelligenceControllerTests
{
    private (IntelligenceController Controller, InvoiceDbContext Ctx) Create()
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new InvoiceDbContext(options);
        ctx.Database.EnsureCreated();

        var svc = Substitute.For<IInvoiceIntelligenceService>();
        var logger = NullLogger<IntelligenceController>.Instance;
        var controller = new IntelligenceController(svc, ctx, logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return (controller, ctx);
    }

    [Fact]
    public async Task NormalizationFeedback_Parses_Odometer_With_Commas()
    {
        var (controller, ctx) = Create();
        var header = new InvoiceHeader
        {
            InvoiceID = 1,
            InvoiceNumber = "INV-1",
            CreatedAt = DateTime.UtcNow
        };
        ctx.InvoiceHeaders.Add(header);
        await ctx.SaveChangesAsync();

        var request = new NormalizationFeedbackRequest
        {
            FieldName = "OdometerLabel",
            ExpectedValue = "67,890"
        };

        var result = await controller.SubmitNormalizationFeedback(header.InvoiceID, request) as OkObjectResult;
        result.Should().NotBeNull();

        var updated = await ctx.InvoiceHeaders.FindAsync(header.InvoiceID);
        updated!.Odometer.Should().Be(67890);
        updated.NormalizationVersion.Should().Be("User Corrected");
    }

    [Fact]
    public async Task NormalizationFeedback_Invalid_Odometer_Does_Not_Update()
    {
        var (controller, ctx) = Create();
        var header = new InvoiceHeader
        {
            InvoiceID = 2,
            InvoiceNumber = "INV-2",
            CreatedAt = DateTime.UtcNow
        };
        ctx.InvoiceHeaders.Add(header);
        await ctx.SaveChangesAsync();

        var request = new NormalizationFeedbackRequest
        {
            FieldName = "OdometerLabel",
            ExpectedValue = "abc"
        };

        var result = await controller.SubmitNormalizationFeedback(header.InvoiceID, request) as OkObjectResult;
        result.Should().NotBeNull();

        var updated = await ctx.InvoiceHeaders.FindAsync(header.InvoiceID);
        updated!.Odometer.Should().BeNull();
    }
}
