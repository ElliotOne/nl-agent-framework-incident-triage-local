# nl-agent-framework-incident-triage-local

An educational example demonstrating a **local-first incident triage assistant** in a C# console app for production-style on-call workflows.

This project uses **OllamaSharp** directly and focuses on one core idea: let the model draft triage output, then enforce severity and domain policy deterministically in code.

## Overview

Live incidents often fail in the first 15 minutes because teams lose time deciding:

- How severe is this incident?
- What should we do immediately?
- Who should we escalate to?
- What should we communicate to stakeholders?

This project demonstrates a practical pattern:

1. Collect incident context from the operator
2. Ask the local model for structured triage output
3. Parse into a typed report contract
4. Apply deterministic severity and domain policy enforcement
5. Return actionable triage guidance and SLA hints

## What This Project Demonstrates

- Single-agent incident triage flow (no multi-agent orchestration)
- Local-first model usage with **OllamaSharp** (`OllamaApiClient`)
- Structured JSON-first output mapped to `IncidentTriageReport`
- Automatic fallback to plain-text mode if structured parsing fails
- Deterministic severity override rules for reliability
- Domain allowlist enforcement to prevent out-of-policy values
- Console streaming mode for real-time response output

## Prerequisites

- .NET 10 SDK or later
  https://dotnet.microsoft.com/

- Ollama installed and running locally
  https://ollama.ai/

- A local chat model pulled in Ollama (example):
  ```bash
  ollama pull mistral:7b
  ```

## Quick Start

Run from the project root:

```bash
dotnet run --project IncidentTriageLocalAgent
```

Type `/exit` to quit.

## Configuration

Default config is in `appsettings.json`:

```json
{
  "Agent": {
    "Provider": "ollama",
    "BaseUrl": "http://localhost:11434",
    "ModelId": "mistral:7b",
    "Temperature": 0.1,
    "MaxOutputTokens": 800
  }
}
```

Environment variable overrides are supported with prefix `ITA_`:

- `ITA_Agent__Provider`
- `ITA_Agent__BaseUrl`
- `ITA_Agent__ModelId`
- `ITA_Agent__Temperature`
- `ITA_Agent__MaxOutputTokens`

Example (Windows):

```bash
set ITA_Agent__BaseUrl=http://localhost:11434
set ITA_Agent__ModelId=mistral:7b
set ITA_Agent__Temperature=0.1
dotnet run --project IncidentTriageLocalAgent
```

## How It Works

1. Incident Input (`Program.cs`)
- Reads incident text from the console.
- Supports:
  - normal mode: structured triage path
  - `/stream <incident text>`: streaming response path

2. Structured Triage (`Services/IncidentTriageService.cs`)
- Requests JSON output for a typed `IncidentTriageReport`.
- If parsing fails, falls back to plain text so interaction is resilient.

3. Deterministic Policy Enforcement (`Services/TriagePolicyEnforcer.cs`)
- Overrides severity where high-confidence impact rules apply.
- Enforces domain allowlist:
  - `API`, `Database`, `Queue`, `Networking`, `Compute`, `Storage`, `Identity`, `ThirdPartyDependency`, `Unknown`
- Normalizes out-of-policy model domain values.

4. Output + SLA Hint (`Program.cs`)
- Prints triage report sections:
  - summary
  - severity
  - domain
  - immediate and stabilization actions
  - escalation targets
  - stakeholder update draft
  - missing critical data
- Adds SLA hint by final severity (`P1` to `P4`).

## Example Prompts

- `Checkout API p95 latency jumped from 220ms to 4.8s after deploy 2026-02-20 14:05 UTC. Error rate is 18% on POST /checkout. DB connections are 98% saturated. US-East customers report payment timeouts.`
- `Login failures started at 2026-02-21 09:12 UTC. 42% of auth requests fail with upstream 503 from identity provider. No deploy today. All regions affected.`
- `Order processing delay increased from 1 minute to 47 minutes since 2026-02-21 03:00 UTC. Queue depth rose from 2,000 to 480,000. Consumer lag keeps increasing.`
- `Nightly report job failed at 2026-02-20 01:00 UTC with low disk warning on analytics node. Customer-facing APIs are unaffected.`
- `One internal dashboard widget shows stale cache since 2026-02-20. No customer impact. Workaround exists.`

Streaming example:

- `/stream Login failures started at 2026-02-21 09:12 UTC. 42% of auth requests fail with upstream 503 from identity provider. No deploy today. All regions affected.`

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
|   +-- Llm/
|   |   +-- OllamaChatClient.cs
|   +-- Services/
|   |   +-- IncidentTriageService.cs
|   |   +-- TriagePolicyEnforcer.cs
+-- LICENSE
+-- README.md
```

## Guardrails Checklist

- Structured output first, with fallback path
- Deterministic severity enforcement
- Domain allowlist normalization
- Local model operation only (no cloud dependency)
- Action-oriented triage output contract

## Notes

- This is intentionally a small, local-first demo.
- Streaming mode currently shows raw model output (policy enforcement is applied in the structured path).

## License

See the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome for improvements to this project's current scope:

- Deterministic severity and routing rule quality
- Schema validation and response auditing
- Test coverage for policy enforcement behavior
