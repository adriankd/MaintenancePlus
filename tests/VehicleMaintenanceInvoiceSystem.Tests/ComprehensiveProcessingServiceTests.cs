using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using VehicleMaintenanceInvoiceSystem.Models;
using VehicleMaintenanceInvoiceSystem.Services;
using Xunit;

namespace VehicleMaintenanceInvoiceSystem.Tests;

public class ComprehensiveProcessingServiceTests
{
    private (ComprehensiveProcessingService Svc, IGitHubModelsService Gh, IInvoiceFallbackService Fb) Create()
    {
        var gh = Substitute.For<IGitHubModelsService>();
        var fb = Substitute.For<IInvoiceFallbackService>();
        var svc = new ComprehensiveProcessingService(gh, fb, NullLogger<ComprehensiveProcessingService>.Instance);
        return (svc, gh, fb);
    }

    [Fact]
    public async Task Uses_GPT_Result_And_Fills_Missing_From_FR()
    {
        var (svc, gh, fb) = Create();
        gh.ProcessInvoiceComprehensivelyAsync(Arg.Any<string>()).Returns(new ComprehensiveInvoiceProcessingResult
        {
            Success = true,
            LineItems = new List<ProcessedLineItem>()
        });

        var fr = new InvoiceData
        {
            VehicleId = "VEH1",
            InvoiceNumber = "INV-1",
            InvoiceDate = new DateTime(2025,1,1),
            Odometer = 123,
            TotalCost = 45.0m,
            LineItems = new List<InvoiceLineData>
            {
                new InvoiceLineData{ LineNumber=1, Description="Oil", Quantity=1, UnitCost=5, TotalCost=5 }
            }
        };

        var result = await svc.ProcessInvoiceComprehensivelyAsync("{}", fr);
        result.Success.Should().BeTrue();
        result.VehicleId.Should().Be("VEH1");
        result.InvoiceNumber.Should().Be("INV-1");
        result.Odometer.Should().Be(123);
        result.TotalCost.Should().Be(45.0m);
        result.Description.Should().NotBeNullOrEmpty();
        result.ProcessingNotes.Should().Contain(note => note.Contains("filled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Falls_Back_On_Rate_Limit()
    {
        var (svc, gh, fb) = Create();
        gh.ProcessInvoiceComprehensivelyAsync(Arg.Any<string>()).Returns(new ComprehensiveInvoiceProcessingResult
        {
            Success = false,
            RateLimitEncountered = true,
            ErrorMessage = "Rate limit"
        });
        fb.ProcessInvoiceAsync(Arg.Any<string>(), Arg.Any<InvoiceData>()).Returns(new ComprehensiveInvoiceProcessingResult
        {
            Success = true
        });

        var res = await svc.ProcessInvoiceComprehensivelyAsync("{}", new InvoiceData{ LineItems = new() });
        res.Success.Should().BeTrue();
        res.ProcessingNotes.Should().Contain(n => n.Contains("Rate limit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Falls_Back_On_Failure()
    {
        var (svc, gh, fb) = Create();
        gh.ProcessInvoiceComprehensivelyAsync(Arg.Any<string>()).Returns(new ComprehensiveInvoiceProcessingResult
        {
            Success = false,
            ErrorMessage = "boom"
        });
        fb.ProcessInvoiceAsync(Arg.Any<string>(), Arg.Any<InvoiceData>()).Returns(new ComprehensiveInvoiceProcessingResult
        {
            Success = true
        });

        var res = await svc.ProcessInvoiceComprehensivelyAsync("{}", new InvoiceData{ LineItems = new() });
        res.Success.Should().BeTrue();
        res.ProcessingNotes.Should().Contain(n => n.Contains("failed", StringComparison.OrdinalIgnoreCase));
    }
}
