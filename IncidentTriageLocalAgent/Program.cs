using IncidentTriageAgent.App;
using IncidentTriageAgent.Domain;
using IncidentTriageAgent.Llm;
using IncidentTriageAgent.Services;
using Microsoft.Extensions.Configuration;
using System.Text;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables(prefix: "ITA_")
    .Build();

var config = AgentAppConfig.Load(configuration);
config.Validate();

var chatClient = new OllamaChatClient(new Uri(config.BaseUrl), config.ModelId);
var triageService = new IncidentTriageService(chatClient);

Console.WriteLine("=== Incident Triage Agent (OllamaSharp Local-First) ===");
Console.WriteLine($"Provider: {config.Provider} | Model: {config.ModelId}");
Console.WriteLine($"Endpoint: {config.BaseUrl}");
Console.WriteLine("Commands: /stream <incident>, /sample, /exit");
Console.WriteLine();

while (true)
{
    Console.Write("Incident> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (input.Equals("/sample", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine(SampleIncident());
        Console.WriteLine();
        continue;
    }

    if (input.StartsWith("/stream ", StringComparison.OrdinalIgnoreCase))
    {
        var streamPrompt = BuildPrompt(input[8..]);
        await RunStreamingAsync(triageService, streamPrompt);
        Console.WriteLine();
        continue;
    }

    await RunTypedOrFallbackAsync(triageService, input);
    Console.WriteLine();
}

static async Task RunTypedOrFallbackAsync(
    IncidentTriageService triageService,
    string incidentInput)
{
    var prompt = BuildPrompt(incidentInput);

    try
    {
        var typedResponse = await triageService.RunStructuredAsync(prompt);
        var policyAdjusted = TriagePolicyEnforcer.Apply(incidentInput, typedResponse);
        PrintReport(policyAdjusted);
        PrintSlaHint(policyAdjusted.Severity);
    }
    catch
    {
        Console.WriteLine("[Structured mode failed on this model. Falling back to text mode.]\n");

        var fallback = await triageService.RunTextAsync(prompt);
        Console.WriteLine(fallback);
    }
}

static async Task RunStreamingAsync(
    IncidentTriageService triageService,
    string prompt)
{
    Console.WriteLine();
    Console.WriteLine("[Streaming triage response]");

    await foreach (var chunk in triageService.RunStreamingTextAsync(prompt))
    {
        Console.Write(chunk);
    }

    Console.WriteLine();
}

static string BuildPrompt(string incidentInput)
{
    var sb = new StringBuilder();
    sb.AppendLine("Analyze this production incident and provide triage output.");
    sb.AppendLine("Use only the information supplied.");
    sb.AppendLine();
    sb.AppendLine("Incident details:");
    sb.AppendLine(incidentInput.Trim());
    sb.AppendLine();
    sb.AppendLine("Expectations:");
    sb.AppendLine("- Classify severity as P1/P2/P3/P4.");
    sb.AppendLine("- Identify the most likely technical domain.");
    sb.AppendLine("- Provide 3 immediate 15-minute actions and 3 stabilization actions for the next hour.");
    sb.AppendLine("- Draft one stakeholder update message.");
    sb.AppendLine("- List missing critical data if confidence is low.");

    return sb.ToString();
}

static void PrintReport(IncidentTriageReport report)
{
    Console.WriteLine("=== Triage Report ===");
    Console.WriteLine($"Summary: {report.IncidentSummary}");
    Console.WriteLine($"Severity: {report.Severity}");
    Console.WriteLine($"Domain: {report.PrimaryDomain}");
    Console.WriteLine($"Customer Impact: {report.CustomerImpact}");

    PrintList("Top Likely Causes", report.TopLikelyCauses);
    PrintList("Immediate Actions (Next 15 Minutes)", report.ImmediateActions15Minutes);
    PrintList("Stabilization Actions (Next 60 Minutes)", report.StabilizationActions60Minutes);
    PrintList("Escalation Targets", report.EscalationTargets);

    Console.WriteLine("Stakeholder Update Draft:");
    Console.WriteLine(report.StakeholderUpdateDraft);

    PrintList("Missing Critical Data", report.MissingCriticalData);
}

static void PrintList(string title, List<string>? items)
{
    Console.WriteLine($"{title}:");

    if (items is null || items.Count == 0)
    {
        Console.WriteLine("- None");
        return;
    }

    foreach (var item in items)
    {
        Console.WriteLine($"- {item}");
    }
}

static void PrintSlaHint(string severity)
{
    var normalized = severity.Trim().ToUpperInvariant();

    var ack = normalized switch
    {
        "P1" => "Acknowledge within 5 minutes; page primary + secondary on-call immediately.",
        "P2" => "Acknowledge within 15 minutes; engage on-call owner and incident commander.",
        "P3" => "Acknowledge within 30 minutes; queue remediation in active sprint.",
        "P4" => "Acknowledge within business hours; track as non-urgent reliability task.",
        _ => "Unknown severity. Validate with incident commander."
    };

    Console.WriteLine();
    Console.WriteLine($"SLA Hint: {ack}");
}

static string SampleIncident() =>
    "Checkout API p95 latency jumped from 220ms to 4.8s after deploy 2026.02.20. " +
    "Error rate is 18% on POST /checkout. CPU is normal, but DB connections are at 98% saturation. " +
    "US-East customers report payment timeouts.";
