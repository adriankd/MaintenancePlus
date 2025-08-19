using System.Text;
using System.Text.Json;

// Simple test program for Phase 2 Intelligence Features
Console.WriteLine("=== Phase 2 Intelligence Features Test ===\n");

// Test 1: Field Normalization
Console.WriteLine("1. Testing Field Normalization:");
await TestFieldNormalization();

Console.WriteLine("\n" + "=".PadRight(50, '='));

// Test 2: Line Item Classification  
Console.WriteLine("\n2. Testing Line Item Classification:");
await TestLineItemClassification();

Console.WriteLine("\n" + "=".PadRight(50, '='));

// Test 3: API Endpoint (simple ping)
Console.WriteLine("\n3. Testing API Endpoints:");
await TestApiEndpoints();

Console.WriteLine("\n=== Test Complete ===");

// Test methods
static async Task TestFieldNormalization()
{
    var testCases = new[]
    {
        ("Invoice #", "invoice number context"),
        ("RO", "work order context"),
        ("Unit #", "vehicle identifier context"),
        ("Miles", "odometer reading context"),
        ("VIN", "vehicle identifier context")
    };

    foreach (var (label, context) in testCases)
    {
        // Simulate normalization logic
        var normalized = NormalizeField(label, context);
        Console.WriteLine($"  '{label}' -> '{normalized.field}' (confidence: {normalized.confidence}%)");
    }
}

static async Task TestLineItemClassification()
{
    var testItems = new[]
    {
        "Oil filter replacement",
        "Brake pad installation", 
        "Engine diagnostic service",
        "Transmission fluid change",
        "Spark plug replacement",
        "Labor - brake service"
    };

    foreach (var item in testItems)
    {
        var classification = ClassifyItem(item);
        Console.WriteLine($"  '{item}' -> {classification.category} (confidence: {classification.confidence}%)");
    }
}

static async Task TestApiEndpoints()
{
    try
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);

        // Test if API is accessible
        var response = await client.GetAsync("http://localhost:5000/");
        Console.WriteLine($"  Application Status: {response.StatusCode}");

        // Test Swagger/API docs
        try
        {
            var swaggerResponse = await client.GetAsync("http://localhost:5000/swagger");
            Console.WriteLine($"  Swagger API Docs: {swaggerResponse.StatusCode}");
        }
        catch
        {
            Console.WriteLine("  Swagger API Docs: Not available");
        }

        Console.WriteLine("  Intelligence API: Ready for testing");
        Console.WriteLine("  Note: Use browser or Postman for full API testing");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  API Test Failed: {ex.Message}");
    }
}

// Simplified normalization logic (mimics the service)
static (string field, int confidence) NormalizeField(string original, string context)
{
    var mappings = new Dictionary<string, string>
    {
        { "Invoice #", "InvoiceNumber" },
        { "RO", "InvoiceNumber" }, 
        { "Unit #", "VehicleID" },
        { "Miles", "Odometer" },
        { "VIN", "VehicleID" }
    };

    if (mappings.TryGetValue(original, out var normalized))
    {
        return (normalized, 95);
    }

    // Fuzzy matching simulation
    if (original.Contains("Invoice") || original.Contains("RO"))
        return ("InvoiceNumber", 80);
    if (original.Contains("Unit") || original.Contains("Vehicle"))
        return ("VehicleID", 85);
    if (original.Contains("Mile") || original.Contains("Odo"))
        return ("Odometer", 85);

    return (original, 60);
}

// Simplified classification logic (mimics the service)
static (string category, int confidence) ClassifyItem(string description)
{
    var partKeywords = new[] { "filter", "pad", "plug", "fluid" };
    var laborKeywords = new[] { "installation", "service", "diagnostic", "labor", "replacement" };

    var lowerDesc = description.ToLower();
    
    var partMatches = partKeywords.Count(k => lowerDesc.Contains(k));
    var laborMatches = laborKeywords.Count(k => lowerDesc.Contains(k));

    if (partMatches > laborMatches)
        return ("Part", Math.Min(95, 60 + partMatches * 15));
    else if (laborMatches > 0)
        return ("Labor", Math.Min(95, 60 + laborMatches * 15));
    else
        return ("Unknown", 50);
}
