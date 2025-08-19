namespace VehicleMaintenanceInvoiceSystem.Services;

/// <summary>
/// Result of field normalization operation
/// </summary>
public class NormalizationResult
{
    public string NormalizedLabel { get; set; } = string.Empty;
    public string OriginalLabel { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool WasNormalized { get; set; }
}

/// <summary>
/// Interface for field normalization services
/// Allows for easy swapping between dictionary-based and semantic-based implementations
/// </summary>
public interface IFieldNormalizer
{
    Task<NormalizationResult> NormalizeAsync(string originalLabel, string fieldContext = "");
    string NormalizerVersion { get; }
    string NormalizerType { get; }
}

/// <summary>
/// Dictionary-based field normalizer for Phase 2 implementation
/// Uses predefined mappings to normalize field labels to standard schema
/// </summary>
public class DictionaryBasedFieldNormalizer : IFieldNormalizer
{
    private readonly ILogger<DictionaryBasedFieldNormalizer> _logger;
    
    public string NormalizerVersion => "v1.0";
    public string NormalizerType => "Dictionary-based";

    // Invoice Number variations
    private static readonly Dictionary<string, string> InvoiceNumberMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Invoice", "InvoiceNumber" },
        { "Invoice No", "InvoiceNumber" },
        { "Invoice #", "InvoiceNumber" },
        { "Invoice Number", "InvoiceNumber" },
        { "Inv", "InvoiceNumber" },
        { "Inv No", "InvoiceNumber" },
        { "Inv #", "InvoiceNumber" },
        { "RO", "InvoiceNumber" },
        { "RO#", "InvoiceNumber" },
        { "RO No", "InvoiceNumber" },
        { "RO Number", "InvoiceNumber" },
        { "Repair Order", "InvoiceNumber" },
        { "Repair Order #", "InvoiceNumber" },
        { "Work Order", "InvoiceNumber" },
        { "Work Order #", "InvoiceNumber" },
        { "WO", "InvoiceNumber" },
        { "WO#", "InvoiceNumber" },
        { "Job", "InvoiceNumber" },
        { "Job #", "InvoiceNumber" },
        { "Job Number", "InvoiceNumber" },
        { "Ticket", "InvoiceNumber" },
        { "Ticket #", "InvoiceNumber" },
        { "Service #", "InvoiceNumber" },
        { "Order #", "InvoiceNumber" }
    };

    // Vehicle identifier variations
    private static readonly Dictionary<string, string> VehicleIdMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Vehicle", "VehicleID" },
        { "Vehicle ID", "VehicleID" },
        { "Vehicle #", "VehicleID" },
        { "Vehicle Number", "VehicleID" },
        { "Veh", "VehicleID" },
        { "Veh ID", "VehicleID" },
        { "Veh #", "VehicleID" },
        { "Unit", "VehicleID" },
        { "Unit #", "VehicleID" },
        { "Unit Number", "VehicleID" },
        { "Fleet #", "VehicleID" },
        { "Fleet Number", "VehicleID" },
        { "Vehicle Registration", "VehicleID" },
        { "Registration", "VehicleID" },
        { "Reg", "VehicleID" },
        { "Reg #", "VehicleID" },
        { "License", "VehicleID" },
        { "License #", "VehicleID" },
        { "License Plate", "VehicleID" },
        { "Plate", "VehicleID" },
        { "Plate #", "VehicleID" },
        { "VIN", "VehicleID" },
        { "Stock", "VehicleID" },
        { "Stock #", "VehicleID" },
        { "Asset #", "VehicleID" },
        { "Tag #", "VehicleID" }
    };

    // Odometer/Mileage variations
    private static readonly Dictionary<string, string> OdometerMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Odometer", "Odometer" },
        { "Odo", "Odometer" },
        { "Mileage", "Odometer" },
        { "Miles", "Odometer" },
        { "Mi", "Odometer" },
        { "Kilometers", "Odometer" },
        { "Km", "Odometer" },
        { "KMs", "Odometer" },
        { "Odometer Reading", "Odometer" },
        { "Current Mileage", "Odometer" },
        { "Total Miles", "Odometer" },
        { "Distance", "Odometer" },
        { "Reading", "Odometer" },
        { "Meter", "Odometer" },
        { "Mile", "Odometer" }
    };

    // Date-related variations
    private static readonly Dictionary<string, string> DateMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Date", "InvoiceDate" },
        { "Invoice Date", "InvoiceDate" },
        { "Service Date", "InvoiceDate" },
        { "Repair Date", "InvoiceDate" },
        { "Work Date", "InvoiceDate" },
        { "Completed", "InvoiceDate" },
        { "Completed Date", "InvoiceDate" },
        { "Date Completed", "InvoiceDate" },
        { "Date of Service", "InvoiceDate" },
        { "Date Serviced", "InvoiceDate" },
        { "Serviced", "InvoiceDate" },
        { "Date In", "InvoiceDate" },
        { "Date Out", "InvoiceDate" }
    };

    // Combined mapping dictionary for easy lookup
    private static readonly Dictionary<string, (string normalized, string type)> AllMappings;

    static DictionaryBasedFieldNormalizer()
    {
        AllMappings = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var kvp in InvoiceNumberMappings)
            AllMappings[kvp.Key] = (kvp.Value, "InvoiceNumber");
            
        foreach (var kvp in VehicleIdMappings)
            AllMappings[kvp.Key] = (kvp.Value, "VehicleID");
            
        foreach (var kvp in OdometerMappings)
            AllMappings[kvp.Key] = (kvp.Value, "Odometer");
            
        foreach (var kvp in DateMappings)
            AllMappings[kvp.Key] = (kvp.Value, "Date");
    }

    public DictionaryBasedFieldNormalizer(ILogger<DictionaryBasedFieldNormalizer> logger)
    {
        _logger = logger;
    }

    public async Task<NormalizationResult> NormalizeAsync(string originalLabel, string fieldContext = "")
    {
        if (string.IsNullOrWhiteSpace(originalLabel))
        {
            return new NormalizationResult
            {
                NormalizedLabel = originalLabel ?? "",
                OriginalLabel = originalLabel ?? "",
                Confidence = 0,
                Method = NormalizerType,
                Version = NormalizerVersion,
                WasNormalized = false
            };
        }

        var result = await Task.Run(() => NormalizeInternal(originalLabel, fieldContext));
        
        if (result.WasNormalized)
        {
            _logger.LogDebug("Normalized '{OriginalLabel}' to '{NormalizedLabel}' with {Confidence}% confidence",
                originalLabel, result.NormalizedLabel, result.Confidence);
        }
            
        return result;
    }

    private NormalizationResult NormalizeInternal(string originalLabel, string fieldContext)
    {
        var cleanLabel = originalLabel.Trim();
        
        // Direct dictionary lookup (highest confidence)
        if (AllMappings.TryGetValue(cleanLabel, out var directMatch))
        {
            return new NormalizationResult
            {
                NormalizedLabel = directMatch.normalized,
                OriginalLabel = originalLabel,
                Confidence = 95,
                Method = NormalizerType,
                Version = NormalizerVersion,
                WasNormalized = !string.Equals(originalLabel, directMatch.normalized, StringComparison.OrdinalIgnoreCase)
            };
        }

        // Try without special characters
        var alphanumericLabel = new string(cleanLabel.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray()).Trim();
        if (AllMappings.TryGetValue(alphanumericLabel, out var alphanumericMatch))
        {
            return new NormalizationResult
            {
                NormalizedLabel = alphanumericMatch.normalized,
                OriginalLabel = originalLabel,
                Confidence = 85,
                Method = NormalizerType,
                Version = NormalizerVersion,
                WasNormalized = true
            };
        }

        // Try fuzzy matching for common variations
        var fuzzyMatch = FindFuzzyMatch(cleanLabel);
        if (fuzzyMatch.HasValue)
        {
            return new NormalizationResult
            {
                NormalizedLabel = fuzzyMatch.Value.normalized,
                OriginalLabel = originalLabel,
                Confidence = fuzzyMatch.Value.confidence,
                Method = NormalizerType,
                Version = NormalizerVersion,
                WasNormalized = true
            };
        }

        // Use field context if provided
        if (!string.IsNullOrWhiteSpace(fieldContext))
        {
            var contextMatch = InferFromContext(cleanLabel, fieldContext);
            if (contextMatch.HasValue)
            {
                return new NormalizationResult
                {
                    NormalizedLabel = contextMatch.Value.normalized,
                    OriginalLabel = originalLabel,
                    Confidence = contextMatch.Value.confidence,
                    Method = NormalizerType,
                    Version = NormalizerVersion,
                    WasNormalized = true
                };
            }
        }

        // No normalization found - return original
        return new NormalizationResult
        {
            NormalizedLabel = originalLabel,
            OriginalLabel = originalLabel,
            Confidence = 100,
            Method = NormalizerType,
            Version = NormalizerVersion,
            WasNormalized = false
        };
    }

    private static (string normalized, decimal confidence)? FindFuzzyMatch(string label)
    {
        var bestMatch = "";
        var bestScore = 0.0;
        var bestType = "";

        foreach (var mapping in AllMappings)
        {
            var similarity = CalculateSimilarity(label, mapping.Key);
            if (similarity > bestScore && similarity >= 0.7) // 70% similarity threshold
            {
                bestScore = similarity;
                bestMatch = mapping.Value.normalized;
                bestType = mapping.Value.type;
            }
        }

        if (bestScore >= 0.7)
        {
            var confidence = (decimal)(50 + (bestScore - 0.7) * 100); // 50-80% confidence range
            return (bestMatch, Math.Min(80, confidence));
        }

        return null;
    }

    private static (string normalized, decimal confidence)? InferFromContext(string label, string context)
    {
        context = context.ToLower();
        label = label.ToLower();

        // Inference based on common patterns
        if ((context.Contains("invoice") || context.Contains("number") || context.Contains("ro")) &&
            (label.Contains("#") || label.Contains("no") || label.Contains("num")))
        {
            return ("InvoiceNumber", 60);
        }

        if ((context.Contains("vehicle") || context.Contains("unit") || context.Contains("fleet")) &&
            (label.Contains("#") || label.Contains("id") || label.Contains("reg")))
        {
            return ("VehicleID", 60);
        }

        if ((context.Contains("mile") || context.Contains("km") || context.Contains("distance")) &&
            (label.Contains("reading") || label.Contains("meter") || label.Contains("odo")))
        {
            return ("Odometer", 60);
        }

        if (context.Contains("date") || label.Contains("date"))
        {
            return ("InvoiceDate", 55);
        }

        return null;
    }

    private static double CalculateSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;

        s1 = s1.ToLower();
        s2 = s2.ToLower();

        if (s1 == s2)
            return 1.0;

        // Simple Levenshtein distance-based similarity
        var distance = LevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);
        
        return 1.0 - ((double)distance / maxLength);
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[s1.Length, s2.Length];
    }
}
