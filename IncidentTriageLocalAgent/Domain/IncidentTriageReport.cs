namespace IncidentTriageAgent.Domain;

public sealed class IncidentTriageReport
{
    public string IncidentSummary { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string PrimaryDomain { get; set; } = string.Empty;
    public string CustomerImpact { get; set; } = string.Empty;
    public List<string> TopLikelyCauses { get; set; } = [];
    public List<string> ImmediateActions15Minutes { get; set; } = [];
    public List<string> StabilizationActions60Minutes { get; set; } = [];
    public List<string> EscalationTargets { get; set; } = [];
    public string StakeholderUpdateDraft { get; set; } = string.Empty;
    public List<string> MissingCriticalData { get; set; } = [];
}
