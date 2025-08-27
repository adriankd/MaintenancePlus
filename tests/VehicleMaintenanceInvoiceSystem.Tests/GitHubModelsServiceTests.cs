using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VehicleMaintenanceInvoiceSystem.Services;
using Xunit;

namespace VehicleMaintenanceInvoiceSystem.Tests;

public class GitHubModelsServiceTests
{
    private class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    private IGitHubModelsService CreateServiceReturning(string content)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                choices = new[]
                {
                    new { message = new { content = content } }
                }
            }), Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler);
        var logger = NullLogger<GitHubModelsService>.Instance;
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GitHubModels:ApiToken"] = "dummy"
        }).Build();
        return new GitHubModelsService(client, logger, cfg);
    }

    [Fact]
    public async Task ProcessInvoiceComprehensivelyAsync_Should_Parse_Json_From_Backticked_Block()
    {
        // Arrange: JSON wrapped in ```json fenced code block
        var json = """
```json
{"success":true,"header":{"vehicleId":"VEH-001","invoiceNumber":"INV-123","invoiceDate":"2025-08-22","odometer":"67890","totalCost":"284.50","description":"Routine Maintenance"},"lineItems":[{"lineNumber":1,"description":"Oil Filter","classification":"Part","unitCost":45,"quantity":1,"totalCost":45,"confidence":0.9}],"overallConfidence":0.92,"processingNotes":["ok"]}
```
""";

        var service = CreateServiceReturning(json);

        // Minimal FR payload; not used by stubbed call except length
        var input = "{\"Content\":\"Hello\"}";

        // Act
        var result = await service.ProcessInvoiceComprehensivelyAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        result.VehicleId.Should().Be("VEH-001");
        result.Odometer.Should().Be(67890);
        result.TotalCost.Should().Be(284.50m);
        result.LineItems.Should().HaveCount(1);
        result.LineItems[0].Classification.Should().Be("Part");
        // Confidence is converted 0-1 to 0-100
        result.LineItems[0].Confidence.Should().BeApproximately(90m, 0.1m);
        result.OverallConfidence.Should().BeApproximately(92m, 0.1m);
    }

    [Theory]
    [InlineData("`{\"success\":true}`")]
    [InlineData("Some preface {\"success\":true} some suffix")]
    public async Task ProcessInvoiceComprehensivelyAsync_Should_Handle_Inline_Json(string content)
    {
        var service = CreateServiceReturning(content);
        var input = "{\"Content\":\"x\"}";
        var result = await service.ProcessInvoiceComprehensivelyAsync(input);
        result.Success.Should().BeTrue();
    }
}
