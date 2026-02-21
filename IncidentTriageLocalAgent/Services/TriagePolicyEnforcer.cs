using IncidentTriageAgent.Domain;
using System.Text.RegularExpressions;

namespace IncidentTriageAgent.Services;

public static class TriagePolicyEnforcer
{
    private static readonly HashSet<string> AllowedDomains =
    [
        "API",
        "Database",
        "Queue",
        "Networking",
        "Compute",
        "Storage",
        "Identity",
        "ThirdPartyDependency",
        "Unknown"
    ];

    public static IncidentTriageReport Apply(string incidentInput, IncidentTriageReport modelReport)
    {
        var report = Clone(modelReport);
        var text = $"{incidentInput} {modelReport.IncidentSummary} {modelReport.CustomerImpact}".ToLowerInvariant();

        report.Severity = DetermineSeverity(text, modelReport.Severity);
        report.PrimaryDomain = NormalizeDomain(modelReport.PrimaryDomain, text);
        return report;
    }

    private static string DetermineSeverity(string text, string modelSeverity)
    {
        var hasNoCustomerImpact = text.Contains("no customer impact") || text.Contains("customer-facing apis are unaffected");
        var hasWorkaround = text.Contains("workaround exists") || text.Contains("temporary workaround");
        var internalOnly = text.Contains("internal dashboard") || text.Contains("internal");
        var allRegions = text.Contains("all regions") || text.Contains("global");
        var highImpactWords = text.Contains("timeouts") || text.Contains("outage") || text.Contains("unavailable") || text.Contains("payment timeouts");
        var queueCrisis = text.Contains("queue depth") && text.Contains("consumer lag keeps increasing");

        var errorRate = ExtractPercentNearKeywords(text, "error rate", "fail", "failure");
        var dbSaturation = ExtractPercentNearKeywords(text, "db connections", "saturated", "saturation");
        var latencyMs = ExtractLargestNumberNearSuffix(text, "ms");

        if ((hasNoCustomerImpact && hasWorkaround) || (internalOnly && hasNoCustomerImpact))
        {
            return "P4";
        }

        if (allRegions && (errorRate >= 20 || highImpactWords))
        {
            return "P1";
        }

        if (queueCrisis || errorRate >= 15 || dbSaturation >= 95 || latencyMs >= 4000)
        {
            return "P1";
        }

        if (errorRate >= 5 || dbSaturation >= 85 || latencyMs >= 1000 || highImpactWords)
        {
            return "P2";
        }

        if (hasNoCustomerImpact)
        {
            return "P3";
        }

        return NormalizeSeverityOrFallback(modelSeverity);
    }

    private static string NormalizeDomain(string modelDomain, string text)
    {
        if (AllowedDomains.Contains(modelDomain))
        {
            return modelDomain;
        }

        if (ContainsAny(text, "api", "endpoint", "http", "latency"))
        {
            return "API";
        }

        if (ContainsAny(text, "database", "db", "sql", "connection pool"))
        {
            return "Database";
        }

        if (ContainsAny(text, "queue", "consumer lag", "backlog", "kafka", "service bus"))
        {
            return "Queue";
        }

        if (ContainsAny(text, "dns", "network", "packet", "tcp", "routing"))
        {
            return "Networking";
        }

        if (ContainsAny(text, "cpu", "memory", "node", "pod", "instance"))
        {
            return "Compute";
        }

        if (ContainsAny(text, "disk", "storage", "filesystem", "cache", "volume", "blob", "s3"))
        {
            return "Storage";
        }

        if (ContainsAny(text, "identity", "auth", "sso", "oauth", "login"))
        {
            return "Identity";
        }

        if (ContainsAny(text, "upstream", "provider", "third-party", "third party", "vendor"))
        {
            return "ThirdPartyDependency";
        }

        return "Unknown";
    }

    private static string NormalizeSeverityOrFallback(string modelSeverity)
    {
        var normalized = modelSeverity.Trim().ToUpperInvariant();
        return normalized is "P1" or "P2" or "P3" or "P4" ? normalized : "P3";
    }

    private static bool ContainsAny(string text, params string[] values) =>
        values.Any(v => text.Contains(v, StringComparison.Ordinal));

    private static int ExtractPercentNearKeywords(string text, params string[] keywords)
    {
        var matches = Regex.Matches(text, @"\b(\d{1,3})\s*%");
        var best = -1;

        foreach (Match match in matches)
        {
            if (!match.Success)
            {
                continue;
            }

            var value = int.Parse(match.Groups[1].Value);
            var index = match.Index;
            var windowStart = Math.Max(0, index - 80);
            var windowLength = Math.Min(text.Length - windowStart, 160);
            var window = text.Substring(windowStart, windowLength);

            if (keywords.Any(k => window.Contains(k, StringComparison.Ordinal)))
            {
                best = Math.Max(best, value);
            }
        }

        return best;
    }

    private static int ExtractLargestNumberNearSuffix(string text, string suffix)
    {
        var matches = Regex.Matches(text, @"\b(\d{2,6})\s*" + Regex.Escape(suffix) + @"\b");
        var best = -1;

        foreach (Match match in matches)
        {
            if (!match.Success)
            {
                continue;
            }

            var value = int.Parse(match.Groups[1].Value);
            best = Math.Max(best, value);
        }

        return best;
    }

    private static IncidentTriageReport Clone(IncidentTriageReport source) =>
        new()
        {
            IncidentSummary = source.IncidentSummary,
            Severity = source.Severity,
            PrimaryDomain = source.PrimaryDomain,
            CustomerImpact = source.CustomerImpact,
            TopLikelyCauses = [.. source.TopLikelyCauses],
            ImmediateActions15Minutes = [.. source.ImmediateActions15Minutes],
            StabilizationActions60Minutes = [.. source.StabilizationActions60Minutes],
            EscalationTargets = [.. source.EscalationTargets],
            StakeholderUpdateDraft = source.StakeholderUpdateDraft,
            MissingCriticalData = [.. source.MissingCriticalData]
        };
}
