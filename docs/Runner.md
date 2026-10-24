# Portable .NET runner

The runner requires .NET 10. Its public API calls the supplied client-operation callback exactly once. The real SDK owns any internal retry. Every actual HTTP send through the supplied transport receives a new attempt ID and the same execution logical ID. The two first-party examples use real HttpClient requests and differ in whether their retry key is generated once per logical operation or again inside the retry loop.

## Build and invoke a first-party example

From the repository root:

```powershell
dotnet build examples/CorrectClient/CorrectClient.csproj -c Release
$env:RA_PROJECT_TOKEN = Read-Host 'Project runner capability'
dotnet examples/CorrectClient/bin/Release/net10.0/CorrectClient.dll --server https://muhammetozeski-retry-assurance.pages.dev --commit YOUR_ACTUAL_COMMIT
```

The capability is intentionally not an argument, URL, settings entry or output field. Set it through your CI secret store in an actual build environment. The command above is an interactive example; do not paste a production API key in its place. The project capability authorizes only this service's synthetic project executions.

Use `examples/BrokenClient` to exercise the same failure with a fresh retry key. A server `pass` returns process code 0; an invariant `fail` returns 1; incomplete evidence, configuration or action failure returns 2. `--baseline RUN_ID --revision N` creates a new run against the selected immutable contract, suitable for comparison with that earlier run. `--json OUTPUT_FILE` writes sanitized evidence, not the grant or capability. The owner sees run evidence at `/runs/RUN_ID` through an actual account session.

## Adapt a real SDK

Reference `src/RetryAssurance.Runner/RetryAssurance.Runner.csproj` or the compiled runner library. Construct the actual SDK with `OperationContext.HttpClient`; supply only dummy SDK credentials and explicitly synthetic request data. Use `RetryAssuranceClient.RunAsync` with an adapter that invokes the real SDK method once and maps its actual typed result to `ClientObservation`. `RunnerCommand.ExecuteAsync` provides the same command boundary used by the working examples.

The callback boundary is one declared logical operation, not one low-level HTTP request. Separate intended operations must use separate executions. The initial pack supports one JSON POST to `jobs`; a client that polls other paths, uploads a real file or fetches a source URL is outside this initial supported adapter contract. Do not claim that merely naming a package independently attests which arbitrary code was executed.

## Destination and privacy boundary

The supplied transport checks the exact HTTPS origin, port and scenario path before inner dispatch; automatic redirects are disabled. It clears all caller general and content headers, preserves only the synthetic Idempotency-Key, then supplies fixed JSON negotiation, generated scenario authorization and dummy SDK authorization. A caller Host override is cleared. Unknown SDK credential headers such as X-API-Key are not forwarded.

The guard does not sanitize arbitrary payload contents or sandbox arbitrary customer code. An adapter must use the supplied client and synthetic body. A program that creates a separate HttpClient bypasses this guard; such code is not claimed to be isolated. The server does not fetch URLs inside a request body and stores no raw body or raw HTTP headers. Its explicit evidence allowlist contains digests, byte count, sequence, effect IDs and response policy.

The real HttpClient timeout is shorter than the pack's deliberate three-second acknowledgement delay. The service commits effects and request records in one transaction before that delay. The optional local journal observer may see that effect while the logical callback is pending. `DurableEffectObservedBeforeClientCompleted` describes only that local monotonic observation; it does not itself establish ordering before the first individual timeout. Server-recorded request/effect timestamps are journal metadata, not measurements of exact database commit completion across computers.

Optional journal failures, including HTTP 429 or 503, are recorded as observation unavailable and do not bypass the final close. The final close remains independently authorized and a real refusal is an error, not a fabricated passing verdict. The runner retries only a control request with an existing stable mint/close identity after one transport failure; it never retries the customer callback itself. Caller cancellation is distinguished from an individual HttpClient timeout.

## Executed narrow verification

`verification/RunnerChecks` is an upper-layer consumer test. It compiles against actual production runner source and substitutes only its own control transport or a handler behind the public destination guard. Eight assertions passed: optional observer 503/429 continues to one close without a fabricated effect, final close refusal still fails, arbitrary credential headers and Host override are stripped, and an off-origin request is refused before inner dispatch. Controlled replies in those tests do not claim service integration or remote effects. Actual HTTPS example execution is verified separately.
