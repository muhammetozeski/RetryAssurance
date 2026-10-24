# Operator and release operations

The independent public repository is https://github.com/muhammetozeski/RetryAssurance and the verified service origin is https://muhammetozeski-retry-assurance.pages.dev. Cloudflare Pages project `muhammetozeski-retry-assurance` serves the app and Functions; D1 database `retry-assurance` stores the service records. Source remains portable: server SQL is in `schema`, API logic in `server`, the portal and runner in separate .NET projects.

## Deployment and database

The GitHub workflow builds the standalone .NET 10 Blazor app with `scripts/Publish-Portal.ps1`, parses service/browser scripts and publishes the `public` output with the existing D1 binding. `CLOUDFLARE_API_TOKEN` and `CLOUDFLARE_ACCOUNT_ID` are repository secrets, not source values. `RATE_SALT` is a secret hosting binding; it salts rate identities and derives recoverable child capabilities. Never replay a redacted hosting configuration over real secrets.

Apply each new SQL migration intentionally to the configured D1 database before deploying code that requires it. The initial schema was applied as 23 successful statements. Migration `002-checkout-intents.sql` adds a single pending checkout identity per account. Do not assume deploying source applies SQL automatically. Database backups and deployment rollback are separate operations: old code must still be compatible with the current schema, and superseded deployment endpoints need review when a security boundary changes. No old source is discarded; Git records the implemented versions.

`/build-info.json` identifies the actual static source commit/build time. `/api/health` checks configured service availability. These are not evidence that a customer SDK was exercised. User verification follows the complete actual runner path in Runner.md.

## Billing readiness

No merchant key or connected Stripe tool was available in the implementation environment. Live billing is therefore deliberately unconfigured. This is an operational boundary, not a simulated subscription. The complete account/project/run workflow remains usable under the free allowance.

The operator runs `scripts/Configure-Billing.ps1` after privately setting `STRIPE_SECRET_KEY`, `CLOUDFLARE_API_TOKEN` and `CLOUDFLARE_ACCOUNT_ID`. The script is an owner-operated setup action, not something a customer or application timer runs. It creates or reuses the fixed USD 99 monthly plan and this app's webhook endpoint, then sends key/price/signing secret together to production environment bindings. It does not create a subscription or charge. If the endpoint already exists, its saved signing secret must be supplied privately as `STRIPE_WEBHOOK_SECRET`; Stripe does not return that secret on later reads. Values are never printed or written to tracked files. The owner must retain them in a private secret store.

Checkout readiness requires all three pieces. An account with an active live subscription is directed to provider management. Each account retains one pending checkout intent; repeated and concurrent starts use the same provider Idempotency-Key and recover the same pending session. The actual provider session is given a bounded one-hour expiry. A checkout return URL does not grant entitlement.

The notification handler validates the raw-body HMAC and a five-minute timestamp window, then reads the actual subscription from Stripe's fixed API origin. The subscription must name this app/account and the exact monthly USD plan. Notifications are recorded by provider identity/hash, and stale subscription events cannot restore an already canceled allowance. Test-mode notifications are recorded distinctly but never grant live paid limits. Payment/customer identifiers remain private to the owning account. The service does not collect card numbers.

## Verification scopes

`verification/service.spec.mjs` consumes the actual route, SQL and SQLite in memory. It contains deterministic paused-body authorization races and narrowly controlled Stripe responses for signed/unsigned/stale/test-mode and checkout-idempotency checks. Those provider fixtures are not a real charge or live Stripe verification.

`verification/RunnerChecks` exercises optional observer refusal, final-close refusal and outgoing transport allowlists against actual runner source. Its controlled transport replies do not claim remote effects. `verification/live-client.ps1` is the separate owner-invoked real HTTPS/.NET acceptance action; it creates a labelled synthetic account, executes both actual clients, checks the real server comparison, scans local evidence for the scoped token and deletes the synthetic account in a finally block. Supply a disposable evidence directory outside the source tree. Its explicit account creation consumes the normal per-IP hourly allowance; do not repeat unchanged live fixtures merely to regenerate logs.

The actual HTTPS .NET check passed with source `8f9e68d`: one broken callback produced two durable resources and server fail/exit 1; one correct callback produced one resource and pass/exit 0; both used real HttpClient cancellation and referenced actual server resources. Exported evidence contained no project capability. The synthetic account and project/run rows were deleted through the real API. These are first-party verification examples, not customer adoption or revenue.

The independent native-browser read of portal deployment `40117660590` rendered five distinct public views and measured 211 visible text samples with zero contrast failures. It observed English and the dark default. It did not click account actions or claim authenticated native-browser execution; actual authentication/tenant/revocation was independently exercised through HTTPS requests.

## Portable distribution

`scripts/Package-Runner.ps1` publishes two portable example payloads to a fresh generated staging directory, packs the typed contracts and runner, and creates a versioned ZIP. Fresh staging prevents inclusion of any settings or logs from an earlier user invocation. It does not contact the scenario API or repeat unchanged client behavior verification. Distributions require .NET 10 and contain DLL commands, not a Windows EXE or bundled runtime. Runtime settings/logs are resolved from the executing payload directory, including when invoked from another working directory; the live acceptance used copied payloads in a disposable directory.

The release assets are `RetryAssurance.Examples.NET10-v1.0.0.zip`, `RetryAssurance.Contracts.1.0.0.nupkg` and `RetryAssurance.Runner.1.0.0.nupkg`. A local NuGet feed needs both packages. Publishing GitHub assets does not claim a NuGet registry publication. No user data, credentials or actual private execution logs belong in an archive. The release source note is [Release 1.0.0](Release-1.0.0.md).

[The initial release](https://github.com/muhammetozeski/RetryAssurance/releases/tag/v1.0.0) was actually published from source `91310ab0c0878bd57826b8c352be6bab80a70e9a`. The runner package's RepositoryCommit matches that source. GitHub acknowledged all three assets: examples ZIP 199,263 bytes, contracts package 18,374 bytes and runner package 38,700 bytes. The ZIP has 21 entries and no runtime-data folders. Workflow `40119074154` passed actual SQL checks, narrow runner checks, optimized portal publication and Pages deployment. `/build-info.json` returned that deployed source commit. Billing remained `configured: false`, `mode: unconfigured`, `premium: false`.

The first pre-fix immutable function deployment was retired after an independent inventory exposed the alternate endpoint; see [Capability authority](Capability-Authority.md). The canonical current deployment and its healthy main API were retained. Final documentation-only updates do not require re-executing unchanged live fixtures or replacing this accepted deployment.
