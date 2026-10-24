# Execution and first-job acceptance contract

This document defines the initial implementation contract. It is not a claim that the service has already passed these checks.

## Ownership and transport

The Blazor portal uses account sessions and project membership. A project owns a versioned behavior contract and explicitly pinned maintained scenario pack. Owner-created runner capabilities are revocable, scoped to that project and stored only as hashes. A minted execution receives a short-lived per-run capability bound to its immutable logical operation ID, contract, pack and destination namespace. Revoking the parent capability also rejects subsequent descendant-run requests. Existing evidence remains readable through authorized account membership.

Customer adapters use the runner-supplied HttpClient. Its handler rejects requests outside the issued HTTPS scenario origin, port and path; redirects are disabled. It removes production-style Authorization, Cookie and Proxy-Authorization values and supplies generated dummy credentials. First-party examples use only explicitly synthetic input. The runner is a constrained transport for cooperating adapters, not a code sandbox for arbitrary customer programs. A run with no observed authorized scenario request cannot pass.

## One operation and durable effects

The runner creates one logical operation ID outside the client's retry loop and invokes the real client callback once. Every actual HTTP attempt receives a distinct attempt ID while retaining that logical ID. Two separately declared logical operations are not collapsed merely because their payloads match.

In the first scenario, a D1 transaction records the attempt, idempotency-key digest, payload digest and resource effect. Only after that commit does the server deliberately delay acknowledgement before response headers. A same-key/same-payload retry returns the same resource; a fresh key can create another. Same-key/different-payload reuse is rejected. The delay is the deliberately tested fault, not a generic recovery mechanism.

Finalization atomically closes admission of new attempts before deriving its verdict from durable evidence. One resource and an exercised scenario can pass the declared cardinality invariant; multiple resources for this one logical operation fail. No traffic, unexercised policy or contradictory evidence is incomplete, not a green result. The caller's success, failure or indeterminate terminal observation is separately labelled and must fit the declared allowed outcomes; it cannot override a duplicate effect.

## Evidence and recurrence

Evidence is an allowlist: server sequence, method, path label, attempt ID, key digest, payload digest and byte count, effect/resource ID, response policy/status and server timestamps. Raw headers, capabilities, Authorization, request bodies and arbitrary customer data are excluded. No cross-computer timestamp ordering is presented as proof. An optional runner-side observation of the durable journal before its own HTTP cancellation can support a clearly labelled local observation.

Every execution pins client/package/commit declarations, pack version/hash and contract version/hash. Client declarations are not independent attestations of arbitrary customer code. Repetition creates a new execution; it never reopens an old verified run. Comparison checks compatible pinned contracts/packs and shows actual effect and request differences. Pack upgrades require explicit adoption.

## First runnable acceptance

1. Two actual accounts cannot read each other's project or run.
2. A provider creates a project, declares semantics, pins a pack and creates a scoped capability.
3. The broken first-party C# client invokes one operation, times out through actual HttpClient cancellation, retries under a new key and leaves two durable jobs; server result fails.
4. A new run with the correct client reuses the key, leaves one job and passes the declared invariant. Safe indeterminate terminal behavior remains distinct from server effects.
5. The portal reads the real request/effect evidence and compares the two new runs.
6. Revoked capabilities fail actual requests, including issued descendant runs; account evidence access remains authorized.
7. Billing success can follow only a valid provider notification. Without owner configuration, checkout is unavailable and no paid entitlement is fabricated.
