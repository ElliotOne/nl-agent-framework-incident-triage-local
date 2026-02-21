# nl-agent-framework-incident-triage-local

An educational **local-first incident triage assistant** built with C# and the **Microsoft Agent Framework**.

This project now uses `AIAgent` + `AgentWorkflowBuilder` for orchestration, while keeping deterministic policy enforcement in code for severity and domain safety.

## Overview

The runtime flow:

1. Operator enters incident details.
2. `TriageAuthoringAgent` generates structured JSON triage output.
3. `TriageReviewAgent` reviews and normalizes that JSON in a sequential workflow.
4. App parses the typed `IncidentTriageReport`.
5. Deterministic `TriagePolicyEnforcer` applies hard severity/domain guardrails.
6. Final report + SLA hint is printed.

If structured parsing fails, the app falls back to plain text triage mode.  
`/stream` uses agent streaming output for real-time console rendering.

## What This Project Demonstrates

- Microsoft Agent Framework agent creation with `ChatClientAgent`
- Sequential workflow composition with `AgentWorkflowBuilder.BuildSequential(...)`
- Local model integration through OpenAI-compatible endpoint (`Ollama`)
- Structured JSON-first triage contract (`IncidentTriageReport`)
- Deterministic post-processing guardrails in C#

## Prerequisites

- .NET 10 SDK or later
- Ollama installed and running locally
- A local chat model pulled (example):

```bash
ollama pull mistral:7b
```

## Quick Start

From the project root:

```bash
dotnet run --project IncidentTriageLocalAgent
```

Commands:

- `/stream <incident>`
- `/sample`
- `/exit`

## Test Inputs

Use these sample incident prompts to validate severity classification, domain selection, and action quality:

- `Checkout API p95 latency jumped from 220ms to 4.8s after deploy 2026-02-20 14:05 UTC. Error rate is 18% on POST /checkout. DB connections are 98% saturated. US-East customers report payment timeouts.`
- `Login failures started at 2026-02-21 09:12 UTC. 42% of auth requests fail with upstream 503 from identity provider. No deploy today. All regions affected.`
- `Order processing delay increased from 1 minute to 47 minutes since 2026-02-21 03:00 UTC. Queue depth rose from 2,000 to 480,000. Consumer lag keeps increasing.`
- `Nightly report job failed at 2026-02-20 01:00 UTC with low disk warning on analytics node. Customer-facing APIs are unaffected.`
- `One internal dashboard widget shows stale cache since 2026-02-20. No customer impact. Workaround exists.`

Streaming example:

- `/stream Login failures started at 2026-02-21 09:12 UTC. 42% of auth requests fail with upstream 503 from identity provider. No deploy today. All regions affected.`

## Configuration

`IncidentTriageLocalAgent/appsettings.json`:

```json
{
  "Agent": {
    "Provider": "ollama",
    "BaseUrl": "http://localhost:11434/v1",
    "ApiKey": "ollama",
    "ModelId": "mistral:7b",
    "Temperature": 0.1,
    "MaxOutputTokens": 800
  }
}
```

Environment overrides (`ITA_` prefix):

- `ITA_Agent__Provider`
- `ITA_Agent__BaseUrl`
- `ITA_Agent__ApiKey`
- `ITA_Agent__ModelId`
- `ITA_Agent__Temperature`
- `ITA_Agent__MaxOutputTokens`

## Project Structure

```text
.
+-- IncidentTriageLocalAgent.slnx
+-- IncidentTriageLocalAgent/
|   +-- IncidentTriageLocalAgent.csproj
|   +-- Program.cs
|   +-- appsettings.json
|   +-- App/
|   |   +-- AgentAppConfig.cs
|   +-- Domain/
|   |   +-- IncidentTriageReport.cs
|   +-- Services/
|   |   +-- IncidentTriageService.cs
|   |   +-- TriagePolicyEnforcer.cs
+-- LICENSE
```

## License

See the [LICENSE](LICENSE) file.

## Contributing

Contributions are welcome for improvements within the current project scope.

Suggested contribution areas:

- Triage instruction quality and prompt robustness
- Deterministic severity/domain policy rules
- Structured response validation and error handling
- Test coverage for policy enforcement and parsing paths
- Console UX improvements for on-call workflows

Typical contribution workflow:

1. Fork the repo and create a feature branch.
2. Make focused changes with clear commit messages.
3. Run `dotnet build IncidentTriageLocalAgent` locally.
4. Open a pull request describing the problem, approach, and verification.
