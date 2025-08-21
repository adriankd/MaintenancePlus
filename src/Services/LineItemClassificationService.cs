namespace VehicleMaintenanceInvoiceSystem.Services;

/// <summary>
/// Result of line item classification operation
/// </summary>
public class ClassificationResult
{
    public string Classification { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public List<string> MatchedKeywords { get; set; } = new List<string>();
}

/// <summary>
/// Interface for line item classification services
/// Allows for easy swapping between rule-based and ML-based implementations
/// </summary>
public interface ILineItemClassifier
{
    Task<ClassificationResult> ClassifyAsync(string description, decimal? unitCost = null, decimal? quantity = null);
    string ClassifierVersion { get; }
    string ClassifierType { get; }
}

/// <summary>
/// Rule-based line item classifier for Phase 2 implementation
/// Uses keyword matching to classify items as Part or Labor
/// </summary>
public class RuleBasedLineItemClassifier : ILineItemClassifier
{
    private readonly ILogger<RuleBasedLineItemClassifier> _logger;
    
    public string ClassifierVersion => "v1.0";
    public string ClassifierType => "Rule-based";

    // Part keywords - items that are physical components
    private static readonly HashSet<string> PartKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Automotive parts
        "filter", "oil", "brake", "tire", "battery", "spark", "plug", "belt", "hose",
        "fluid", "gasket", "seal", "bearing", "rotor", "pad", "shoe", "disc", "drum",
        "alternator", "starter", "radiator", "thermostat", "pump", "sensor", "switch",
        "bulb", "fuse", "relay", "wire", "cable", "part", "component", "kit",
        
        // Fluids and consumables
        "coolant", "antifreeze", "transmission", "differential", "hydraulic", "grease",
        "additive", "cleaner", "sealant", "gasoline", "diesel", "windshield", "washer",
        
        // Engine components  
        "piston", "cylinder", "head", "block", "valve", "cam", "timing", "chain",
        "tensioner", "guide", "manifold", "injector", "throttle", "air", "intake",
        
        // Suspension and steering
        "shock", "strut", "spring", "control", "arm", "ball", "joint", "tie", "rod",
        "rack", "pinion", "power", "steering", "wheel", "alignment",
        
        // Electrical
        "ignition", "coil", "distributor", "cap", "rotor", "points", "condenser",
        "voltage", "regulator", "wiring", "harness", "connector", "terminal",
        
        // Drivetrain
        "clutch", "flywheel", "driveshaft", "axle", "differential", "cv", "joint",
        "universal", "mount", "bushing"
    };

    // Labor keywords - services and work performed
    private static readonly HashSet<string> LaborKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // General labor terms
        "labor", "labour", "service", "install", "installation", "remove", "removal",
        "replace", "replacement", "repair", "diagnostic", "diagnosis", "inspect",
        "inspection", "adjust", "adjustment", "align", "alignment", "balance",
        "calibrate", "calibration", "flush", "bleed", "test", "testing", "check",
        "maintenance", "tune", "up", "overhaul", "rebuild", "refurbish",
        
        // Time-based indicators
        "hour", "hours", "hr", "hrs", "time", "flat", "rate", "charge", "fee",
        "bench", "shop", "minimum", "diagnostic", "programming", "setup",
        
        // Specific services
        "oil", "change", "lube", "lubrication", "rotation", "mount", "mounting",
        "balance", "balancing", "alignment", "front", "end", "brake", "service",
        "transmission", "service", "coolant", "flush", "radiator", "flush",
        "power", "steering", "flush", "differential", "service", "tune", "up",
        
        // Repair types
        "weld", "welding", "machine", "machining", "resurface", "resurfacing",
        "bore", "boring", "hone", "honing", "press", "pressing", "cut", "cutting",
        "grind", "grinding", "thread", "threading", "tap", "tapping"
    };

    // Tax and fee keywords - neither part nor labor
    private static readonly HashSet<string> TaxFeeKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "tax", "sales", "gst", "pst", "hst", "vat", "fee", "disposal", "environmental",
        "core", "charge", "hazmat", "hazardous", "material", "recycling", "shop",
        "supplies", "consumables", "misc", "miscellaneous", "surcharge", "freight",
        "shipping", "handling", "documentation", "admin", "administrative"
    };

    public RuleBasedLineItemClassifier(ILogger<RuleBasedLineItemClassifier> logger)
    {
        _logger = logger;
    }

    public async Task<ClassificationResult> ClassifyAsync(string description, decimal? unitCost = null, decimal? quantity = null)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return new ClassificationResult
            {
                Classification = "Unclassified",
                Confidence = 0,
                Method = ClassifierType,
                Version = ClassifierVersion
            };
        }

        var result = await Task.Run(() => ClassifyInternal(description, unitCost, quantity));
        
        _logger.LogDebug("Classified '{Description}' as '{Classification}' with {Confidence}% confidence",
            description, result.Classification, result.Confidence);
            
        return result;
    }

    private ClassificationResult ClassifyInternal(string description, decimal? unitCost, decimal? quantity)
    {
        var words = description.Split(new[] { ' ', '-', '_', '/', '\\', ',', '.', '(', ')', '[', ']' }, 
            StringSplitOptions.RemoveEmptyEntries);

        var partMatches = new List<string>();
        var laborMatches = new List<string>();
        var taxFeeMatches = new List<string>();

        // Check for keyword matches
        foreach (var word in words)
        {
            if (PartKeywords.Contains(word))
                partMatches.Add(word);
            else if (LaborKeywords.Contains(word))
                laborMatches.Add(word);
            else if (TaxFeeKeywords.Contains(word))
                taxFeeMatches.Add(word);
        }

        // Tax/Fee takes precedence (usually easy to identify)
        if (taxFeeMatches.Count > 0)
        {
            return new ClassificationResult
            {
                Classification = "Tax/Fee",
                Confidence = Math.Min(95, 70 + (taxFeeMatches.Count * 10)),
                Method = ClassifierType,
                Version = ClassifierVersion,
                MatchedKeywords = taxFeeMatches
            };
        }

        // Calculate base confidence from keyword matches
        var partScore = partMatches.Count * 15;
        var laborScore = laborMatches.Count * 15;

        // Additional heuristics
        
        // High unit cost with low quantity often indicates parts
        if (unitCost.HasValue && quantity.HasValue && unitCost.Value > 50 && quantity.Value <= 2)
        {
            partScore += 10;
        }
        
        // Strong labor indicators (service actions) get extra weight
        if (ContainsServiceAction(description))
        {
            laborScore += 25; // Higher weight for service actions
        }
        
        // Time-based patterns indicate labor
        if (ContainsTimePattern(description))
        {
            laborScore += 20;
        }
        
        // Part number patterns indicate parts
        if (ContainsPartNumberPattern(description))
        {
            partScore += 15;
        }

        // Determine classification
        if (partScore > laborScore)
        {
            var confidence = Math.Min(95, Math.Max(50, partScore + 10));
            return new ClassificationResult
            {
                Classification = "Part",
                Confidence = confidence,
                Method = ClassifierType,
                Version = ClassifierVersion,
                MatchedKeywords = partMatches
            };
        }
        else if (laborScore > partScore)
        {
            var confidence = Math.Min(95, Math.Max(50, laborScore + 10));
            return new ClassificationResult
            {
                Classification = "Labor",
                Confidence = confidence,
                Method = ClassifierType,
                Version = ClassifierVersion,
                MatchedKeywords = laborMatches
            };
        }
        else
        {
            // No clear winner - use additional context and tie-breaking rules
            
            // If there are service actions, favor labor classification
            if (ContainsServiceAction(description))
            {
                return new ClassificationResult
                {
                    Classification = "Labor",
                    Confidence = 60,
                    Method = ClassifierType,
                    Version = ClassifierVersion,
                    MatchedKeywords = laborMatches
                };
            }
            
            // If there are part keywords but no service actions, favor parts
            if (partMatches.Any() && !laborMatches.Any())
            {
                return new ClassificationResult
                {
                    Classification = "Part",
                    Confidence = 60,
                    Method = ClassifierType,
                    Version = ClassifierVersion,
                    MatchedKeywords = partMatches
                };
            }
            
            // Use cost-based heuristics as fallback
            if (unitCost.HasValue)
            {
                // Very low unit costs often indicate labor (hourly rates)
                if (unitCost.Value < 20)
                {
                    return new ClassificationResult
                    {
                        Classification = "Labor",
                        Confidence = 40,
                        Method = ClassifierType,
                        Version = ClassifierVersion
                    };
                }
                // High unit costs often indicate parts
                else if (unitCost.Value > 100)
                {
                    return new ClassificationResult
                    {
                        Classification = "Part",
                        Confidence = 45,
                        Method = ClassifierType,
                        Version = ClassifierVersion
                    };
                }
            }

            // Default to unclassified for ambiguous cases
            return new ClassificationResult
            {
                Classification = "Unclassified",
                Confidence = 20,
                Method = ClassifierType,
                Version = ClassifierVersion
            };
        }
    }

    private static bool ContainsTimePattern(string description)
    {
        // Look for patterns like "2.5 hr", "1 hour", "0.5 hrs", etc.
        var timePatterns = new[] { "hr", "hrs", "hour", "hours", "time" };
        var words = description.ToLower().Split();
        
        for (int i = 0; i < words.Length; i++)
        {
            if (timePatterns.Contains(words[i]))
            {
                // Check if preceded by a number
                if (i > 0 && (decimal.TryParse(words[i - 1], out _) || 
                    words[i - 1].Contains('.') || words[i - 1].Contains(',')))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static bool ContainsPartNumberPattern(string description)
    {
        // Look for patterns like part numbers: AC-PF52, 123-456-789, etc.
        var words = description.Split();
        return words.Any(word => 
            (word.Contains('-') && word.Length > 4 && char.IsLetterOrDigit(word[0])) ||
            (char.IsLetter(word[0]) && word.Any(char.IsDigit) && word.Length > 3));
    }

    private static bool ContainsServiceAction(string description)
    {
        var serviceActions = new[] { 
            "replace", "replacement", "install", "installation", "repair", "service", 
            "change", "flush", "adjust", "adjustment", "align", "alignment", 
            "mount", "mounting", "remove", "removal", "check", "inspect", "inspection" 
        };
        var words = description.ToLower().Split();
        return words.Any(word => serviceActions.Contains(word));
    }
}
