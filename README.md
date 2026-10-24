# Retry Assurance

A service for testing one real client operation against durable, version-pinned failure scenarios. Customer code executes in the customer's environment. The service records synthetic remote effects and request evidence; it does not host arbitrary builds or proxy production APIs.

The first scenario creates a job before delaying its acknowledgement. Retrying under a fresh idempotency key can create a second job. The verdict uses the server's durable resource count, not a caller's declared pass flag. A documented indeterminate client outcome can be allowed and is labelled separately from server-observed effects.

## Source map

- `src/RetryAssurance.Portal`: independent English Blazor application.
- `src/RetryAssurance.Runner`: portable .NET runner and destination-bound HTTP transport.
- `src/RetryAssurance.Contracts`: typed service and runner contracts.
- `examples`: first-party broken and correct clients, using real HTTP requests.
- `functions/api` and `server`: account, project, scoped capability, execution and billing services.
- `schema`: D1 migrations; `scripts`: build, deployment and owner configuration.
- [Project knowledge](docs/INDEX.md) and [handoff method](Walkthrough/Guide.md).

## Development state

Implementation has begun. Repository, deployment and verified usage instructions will be added after those operations actually complete. Pricing and market demand remain hypotheses; no customer adoption or payment is represented by examples.
