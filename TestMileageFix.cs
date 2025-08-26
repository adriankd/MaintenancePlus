using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace VehicleMaintenanceInvoiceSystem.Tests
{
    /// <summary>
    /// Simple test to verify the mileage extraction fix
    /// </summary>
    public class TestMileageFix
    {
#if DEBUG && TEST_HARNESS
        public static void Main(string[] args)
        {
            Console.WriteLine("Testing Mileage Extraction Fix");
            Console.WriteLine("==============================");

            // Test cases that would have failed before the fix
            var testCases = new[]
            {
                "Current mileage: 67,890 miles",
                "Odometer reading 123,456",
                "Mileage 45,000 KM",
                "Vehicle has 98,765 miles on odometer",
                "Odometer: 1,234,567",
                "Current miles: 12345" // This should work with both old and new logic
            };

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"\nTesting: '{testCase}'");
                
                var result = ExtractOdometerFromText(testCase);
                Console.WriteLine($"Extracted: {result}");
            }
        }

        private static int? ExtractOdometerFromText(string text)
        {
            var odometerKeywords = new[] { "odometer", "mileage", "miles", "km", "kilometers" };
            
            var lowerText = text.ToLowerInvariant();
            if (!odometerKeywords.Any(keyword => lowerText.Contains(keyword)))
                return null;

            // First try to extract numbers with commas (e.g., 67,890)
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
#endif
    }
}
