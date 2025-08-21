using System.Text;
using System.Text.Json;

// Test if Count(predicate) works without explicit using System.Linq;
var keywords = new[] { "test", "example", "sample" };
var description = "This is a test example";

var matches = keywords.Count(k => description.Contains(k));
Console.WriteLine($"Matches found: {matches}");
