using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VehicleMaintenanceInvoiceSystem.Data;
using VehicleMaintenanceInvoiceSystem.Services;
using Xunit;

namespace VehicleMaintenanceInvoiceSystem.Tests;

public class InvoiceFallbackServiceTests
{
    private InvoiceFallbackService CreateService()
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new InvoiceDbContext(options);
        var logger = NullLogger<InvoiceFallbackService>.Instance;
        return new InvoiceFallbackService(ctx, logger);
    }

    [Theory]
    [InlineData("Engine oil change labor", "Part")] // Contains 'oil' -> parts dictionary wins
    [InlineData("Install cabin filter", "Part")]    // Contains 'cabin filter' -> Part
    [InlineData("Brake pads", "Part")]
    [InlineData("Shop supplies", "Fee")]
    [InlineData("Sales tax", "Tax")]
    [InlineData("Unknown item text", "Other")]
    public async Task ClassifyLineItems_By_Keywords(string desc, string expected)
    {
        var svc = CreateService();
        var data = new InvoiceData
        {
            LineItems = new List<InvoiceLineData>
            {
                new InvoiceLineData{ LineNumber = 1, Description = desc, Quantity = 1, UnitCost = 10, TotalCost = 10 }
            }
        };

        var result = await svc.ProcessInvoiceAsync("{}", data);

        result.Success.Should().BeTrue();
        result.LineItems.Should().ContainSingle();
        result.LineItems[0].Classification.Should().Be(expected);
    }

    [Fact]
    public async Task Extracts_PartNumber_From_Description_When_Present()
    {
        var svc = CreateService();
        var data = new InvoiceData
        {
            LineItems = new List<InvoiceLineData>
            {
                new InvoiceLineData{ LineNumber = 1, Description = "Replace oil filter 15400-RTA-003", Quantity = 1, UnitCost = 10, TotalCost = 10 }
            }
        };

        var result = await svc.ProcessInvoiceAsync("{}", data);

        result.LineItems[0].PartNumber.Should().Be("15400-RTA-003");
    }

    [Fact]
    public async Task Normalizes_Invoice_Number_And_VehicleID()
    {
        var svc = CreateService();
        var data = new InvoiceData
        {
            InvoiceNumber = "INV: 123",
            VehicleId = "Vehicle # ABC123",
            LineItems = new List<InvoiceLineData>()
        };

        var result = await svc.ProcessInvoiceAsync("{}", data);
        result.InvoiceNumber.Should().Be("123");
        result.VehicleId.Should().Be("ABC123");
    }

    [Fact]
    public async Task Generates_Description_Summary_From_Classifications()
    {
        var svc = CreateService();
        var data = new InvoiceData
        {
            LineItems = new List<InvoiceLineData>
            {
                new InvoiceLineData{ LineNumber=1, Description="Oil filter", Quantity=1, UnitCost=5, TotalCost=5 },
                new InvoiceLineData{ LineNumber=2, Description="Shop supplies", Quantity=1, UnitCost=2, TotalCost=2 }
            }
        };

        var result = await svc.ProcessInvoiceAsync("{}", data);
        result.Description.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("ACDELCO PF454", "PF454")] // Generic pattern extracts the alphanumeric part number
    [InlineData("Toyota filter 90915-YZZD2", "90915-YZZD2")] // Toyota pattern
    public async Task ExtractPartNumber_Covers_More_Patterns(string input, string expectedContains)
    {
        var svc = CreateService();
        var data = new InvoiceData
        {
            LineItems = new List<InvoiceLineData>
            {
                new InvoiceLineData{ LineNumber=1, Description=input, Quantity=1, UnitCost=1, TotalCost=1 }
            }
        };
        var result = await svc.ProcessInvoiceAsync("{}", data);
        result.LineItems[0].PartNumber.Should().NotBeNull();
        result.LineItems[0].PartNumber!.Should().Contain(expectedContains);
    }
}
