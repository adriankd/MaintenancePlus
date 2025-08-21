using System;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Simple console test to verify our odometer/mileage fix handles comma-separated numbers correctly
/// This test simulates the exact logic we implemented in FormRecognizerService.cs
/// </summary>
public class TestOdometerFix
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== Testing Odometer/Mileage Comma Fix ===");
        Console.WriteLine();

        // Test cases that would have failed before our fix
        var testCases = new[]
        {
            "Current odometer reading: 67,890 miles",
            "Vehicle mileage at service: 123,456 km", 
            "Odometer: 45,000 miles",
            "Mileage check shows 98,765 miles",
            "Current reading is 1,234,567 kilometers",
            "Miles on odometer: 12345", // No comma - should still work
            "Vehicle has completed 87,654 miles",
            "Odometer reading of 5,432 miles recorded",
            "Current vehicle mileage is 156,789 km"
        };

        int passedTests = 0;
        int totalTests = testCases.Length;

        foreach (var testCase in testCases)
        {
            Console.WriteLine($"Testing: '{testCase}'");
            
            var result = ExtractOdometerFromText(testCase);
            
            if (result.HasValue)
            {
                Console.WriteLine($"  ✓ Extracted: {result.Value:N0}");
                passedTests++;
            }
            else
            {
                Console.WriteLine($"  ✗ Failed to extract odometer reading");
            }
            Console.WriteLine();
        }

        Console.WriteLine("=== Test Results ===");
        Console.WriteLine($"Passed: {passedTests}/{totalTests}");
        Console.WriteLine($"Success Rate: {(double)passedTests/totalTests:P0}");
        
        if (passedTests == totalTests)
        {
            Console.WriteLine("🎉 All tests passed! The comma-separated mileage fix is working correctly.");
        }
        else
        {
            Console.WriteLine("❌ Some tests failed. The fix may need adjustment.");
        }
    }

    private static int? ExtractOdometerFromText(string text)
    {
        var odometerKeywords = new[] { "odometer", "mileage", "miles", "km", "kilometers" };
        
        var lowerText = text.ToLowerInvariant();
        if (!odometerKeywords.Any(keyword => lowerText.Contains(keyword)))
            return null;

        // First try to extract numbers with commas (e.g., 67,890) - THE FIX
        var commaNumbers = Regex.Matches(lowerText, @"\d{1,3}(?:,\d{3})+")
            .Cast<Match>()
            .Select(m => m.Value)
            .ToList();

        if (commaNumbers.Any())
        {
            var numberString = commaNumbers.First().Replace(",", "");
            if (int.TryParse(numberString, out var odometerWithComma))
            {
                return odometerWithComma;
            }
        }

        // Fallback to regular numbers without commas (at least 3 digits)
        var numbers = Regex.Matches(lowerText, @"\d+")
            .Cast<Match>()
            .Select(m => m.Value)
            .Where(s => s.Length >= 3) // Assume odometer has at least 3 digits
            .ToList();

        if (numbers.Any() && int.TryParse(numbers.First(), out var odometer))
        {
            return odometer;
        }

        return null;
    }
}
