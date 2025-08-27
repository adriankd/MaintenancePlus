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

public class IntelligenceController_ClassificationFeedbackTests
{
    private (IntelligenceController Controller, InvoiceDbContext Ctx, IInvoiceIntelligenceService Svc) Create()
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new InvoiceDbContext(options);
        ctx.Database.EnsureCreated();

        var svc = Substitute.For<IInvoiceIntelligenceService>();
        var controller = new IntelligenceController(svc, ctx, NullLogger<IntelligenceController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return (controller, ctx, svc);
    }

    [Fact]
    public async Task SubmitClassificationFeedback_Valid_Updates_Line_And_Saves()
    {
        var (controller, ctx, svc) = Create();
        var header = new InvoiceHeader { InvoiceID = 1, InvoiceNumber = "INV-1", CreatedAt = DateTime.UtcNow };
        var line = new InvoiceLine { LineID = 10, InvoiceID = 1, Description = "Oil" };
        ctx.InvoiceHeaders.Add(header);
        ctx.InvoiceLines.Add(line);
        await ctx.SaveChangesAsync();

        var req = new ClassificationFeedbackRequest
        {
            CorrectCategory = "Labor",
            OriginalClassification = "Part",
            OriginalConfidence = 0.5m,
            ClassificationVersion = "v1"
        };

        var result = await controller.SubmitClassificationFeedback(1, 10, req);
        result.Should().BeOfType<OkObjectResult>();

        var updated = await ctx.InvoiceLines.FindAsync(10);
        updated!.ClassifiedCategory.Should().Be("Labor");
        updated.ClassificationConfidence.Should().Be(1.0m);
        updated.ClassificationMethod.Should().Be("User Correction");
        await svc.Received().RecordClassificationFeedbackAsync(10, "Labor", Arg.Any<string>());
    }

    [Fact]
    public async Task SubmitClassificationFeedback_Missing_Category_Returns_BadRequest()
    {
        var (controller, _, _) = Create();
        var res = await controller.SubmitClassificationFeedback(1, 1, new ClassificationFeedbackRequest { CorrectCategory = "" });
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SubmitClassificationFeedback_Line_Not_Found_Returns_NotFound()
    {
        var (controller, _, _) = Create();
        var req = new ClassificationFeedbackRequest { CorrectCategory = "Labor" };
        var res = await controller.SubmitClassificationFeedback(1, 999, req);
        res.Should().BeOfType<NotFoundObjectResult>();
    }
}
