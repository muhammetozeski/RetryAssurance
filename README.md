# Retry Assurance

[![Service build](https://github.com/muhammetozeski/RetryAssurance/actions/workflows/deploy.yml/badge.svg)](https://github.com/muhammetozeski/RetryAssurance/actions/workflows/deploy.yml)

[Open the actual service](https://muhammetozeski-retry-assurance.pages.dev) · [Source](https://github.com/muhammetozeski/RetryAssurance) · [Portable releases](https://github.com/muhammetozeski/RetryAssurance/releases) · [Runner boundaries](docs/Runner.md)

Execute one real client operation against a durable, version-pinned failure scenario. Customer code stays in its local or CI environment. The service records synthetic remote effects and request evidence; it does not host arbitrary builds or proxy production APIs.

The first scenario commits a job before withholding its acknowledgement for three seconds. An internal retry under a fresh key can create another job. The server derives its verdict from durable effects and request relationships, keeping the runner's final observation separate. A documented indeterminate outcome can be allowed without assuming eventual success.

## Complete the first job

1. [Create or recover an actual account](https://muhammetozeski-retry-assurance.pages.dev/account). Save the one-time private recovery key.
2. Create a project, declare key ownership/allowed terminal outcomes and pin `ra-core 1.0.0`.
3. Create a scoped project runner capability. Keep it in a private process/CI environment variable named `RA_PROJECT_TOKEN`.
4. Download the portable examples from a release, or build from source with .NET 10:

   ```powershell
   dotnet build examples/BrokenClient/BrokenClient.csproj -c Release
   dotnet build examples/CorrectClient/CorrectClient.csproj -c Release
   $env:RA_PROJECT_TOKEN = Read-Host -MaskInput 'Project runner capability'
   dotnet examples/BrokenClient/bin/Release/net10.0/BrokenClient.dll --server https://muhammetozeski-retry-assurance.pages.dev --commit YOUR_ACTUAL_COMMIT
   dotnet examples/CorrectClient/bin/Release/net10.0/CorrectClient.dll --server https://muhammetozeski-retry-assurance.pages.dev --commit YOUR_ACTUAL_COMMIT
   ```

5. Open the actual run IDs in the project. The first-party broken client leaves two remote jobs and exits 1 for server fail; the correct client reuses the key, leaves one remote job and exits 0 for server pass. Incomplete/action failures exit 2.
6. Create a separate new execution with `--baseline ACTUAL_RUN_ID --revision N`, then compare the pinned records in the portal. No old result is reopened or overwritten.

These are runnable synthetic first-party clients, not fabricated customer activity. A real client without this fault can pass; unknown-bug discovery is not guaranteed. Adapt your SDK by injecting the supplied HttpClient, invoking the actual operation once and mapping its actual typed result. Only dummy credentials and explicitly synthetic input belong in this initial JSON POST/jobs pack.

## Boundaries and actual state

- Real recovery-key accounts, owner/editor/viewer project roles, one-use invitations and revocable project/run capabilities.
- Immutable behavior revisions and pack hashes; actual request journal/resource evidence; separate runner declarations.
- A destination-bound .NET transport, outgoing header allowlist and no raw request-body/header/capability storage in evidence. It is not a sandbox for arbitrary code and does not sanitize arbitrary payloads.
- Free allowance: two owned projects and fifty executions per UTC calendar month. Planned Team allowance: twenty projects and one thousand executions for USD 99 monthly, only after a verified live provider entitlement.
- **Live billing remains unconfigured.** The free workflow is usable. Real Stripe integration and owner setup are implemented; absent merchant credentials are not replaced by simulated payment success.
- English-only independent Blazor portal with a dark default, optional light preference and live text-contrast guard. No advertising trackers in private workflows.

Initial demand and price are hypotheses. Existing mocking/contract/replay tools remain alternatives; the proposed paid value is maintained semantic failure cases and repeated real-release verification.

## Source map

- `src/RetryAssurance.Portal`: independent English Blazor application.
- `src/RetryAssurance.Runner`: portable .NET runner and destination-bound HTTP transport.
- `src/RetryAssurance.Contracts`: typed service and runner contracts.
- `examples`: first-party broken and correct clients, using real HTTP requests.
- `functions/api` and `server`: account, project, scoped capability, execution and billing services.
- `schema`: D1 migrations; `scripts`: build, deployment and owner configuration.
- [Project knowledge](docs/INDEX.md) and [handoff method](Walkthrough/Guide.md).

## Build, publish and operate

The portal requires the .NET 10 SDK to build; released portable example DLLs require the .NET 10 runtime. No Windows executable or bundled runtime is implied. `scripts/Publish-Portal.ps1` builds the independent browser application; `scripts/Package-Runner.ps1` creates fresh release payloads without user settings or credentials. Generated output lives under ignored `publish`/`public` paths; authored source is preserved.

GitHub Actions builds and verifies the service/runner, then deploys to the independent Cloudflare Pages project using repository secrets. D1 migrations are applied explicitly. See [operator documentation](docs/Operations.md) before changing schema, secrets, plan or webhook configuration. Never copy a redacted hosting response over live environment values.

Actual HTTPS acceptance exercised both real C# clients, tenant isolation, pinned comparison and revocation. Narrow provider tests use explicitly controlled replies and do not claim a real Stripe charge. Verification scope and executed results are recorded in [project knowledge](docs/INDEX.md).
