# Mounted hosting

Retry Assurance supports its existing independent origin and the application base `/retry-assurance/`. Both endpoints use the existing `retry-assurance` D1 database (`0d144a72-cc9d-47a1-ac1f-9d5d55c0647e`); mounting does not copy, migrate or reset customer records.

## Build and runtime contract

From the repository root, publish the mounted static files with:

```powershell
./scripts/Publish-Portal.ps1 -NoRestore -BasePath '/retry-assurance/' -Destination 'C:\C\RetryAssuranceMount\public\retry-assurance' -ServiceBaseUrl 'https://muhammetozeski-binsite.pages.dev/retry-assurance/'
```

`-NoRestore` uses already restored dependencies; omit it in a clean build environment. The destination is the host's `public/retry-assurance` directory. The publisher emits a matching base element, relative assets, a current example-client ZIP, build metadata and refreshed gzip/Brotli bootstrap files. The ZIP instructions and portal commands contain the complete service base URL, including the mount path and final slash.

Merge the emitted `_headers` and `_redirects` into the host's root metadata. Child metadata files are not applied by Pages. Only the declared SPA routes are rewritten; `_framework`, scripts, styles and downloads retain their actual paths. Merge `_routes.json` so Functions handle `/api/*` and `/retry-assurance/api/*`, rather than all static requests.

Copy these backend files without changing their relative import layout:

- `server/handler.mjs`
- `server/security.mjs`
- `server/accounts.mjs`
- `server/projects.mjs`
- `server/executions.mjs`
- `server/billing.mjs`
- `server/protocol.mjs`

The mount function imports `handleRequest` from the copied `handler.mjs` and calls `handleRequest({request, env}, {basePath:'/retry-assurance'})`. Preserve the actual request URL, origin, headers and body. The existing independent `functions/api/[[path]].js` calls the same handler with its default empty base path.

The handler's environment names are `DB`, `RATE_SALT`, `STRIPE_SECRET_KEY`, `STRIPE_PRICE_ID` and `STRIPE_WEBHOOK_SECRET`. The parent host maps its dedicated `RETRY_DB`, `RETRY_RATE_SALT` and `RETRY_STRIPE_*` bindings to those names. When an explicit retry salt is absent, the parent derives an isolated salt from its existing commerce salt and the mount identity. Unrelated provider keys are never fallback values. Payment remains unavailable unless all three explicit Stripe settings are present; no provider setup is performed by this mount build.

## Existing access and endpoint boundaries

- Recovery keys, sessions and issued project/run capabilities are checked using their existing stored SHA-256 hashes, independently of the rate salt. A user can recover the same account at either host with their existing recovery key. Browser sign-in is separate per host.
- The `ra_session` cookie retains `HttpOnly`, `Secure` and `SameSite=Strict`. Its path is `/api` independently and `/retry-assurance/api` when mounted. Logout clears the matching path.
- The salt also derives the run key for a stable mint request identity. Replaying an old identity at an endpoint with a different salt returns `409 mint_endpoint` if it cannot reproduce the stored key hash. Continue that request at its original endpoint or start a new execution. An already issued run key still authorizes the matching run through either endpoint.
- Browser writes check the actual request origin. Runner/scenario requests retain capability authorization and reject a supplied mismatched origin. No origin rewriting or production proxy is introduced.
- Scenario grants use the actual endpoint prefix. The C# runner accepts only HTTPS roots ending `/` or `/retry-assurance/` and verifies that the granted scenario path belongs to that exact service base before passing a transport to an adapter.
- Existing independent CLI commands and `/api/billing/webhook` keep working. Mounted checkout and portal returns use `/retry-assurance/account/billing`. Enabling a separate mounted webhook later requires its own explicit signing secret; the independent-only billing setup helper is not a mount configuration tool.
- The scoped content policy allows only the application's own scripts, assets and connections, with the WebAssembly execution permission already used by the standalone portal. Appearance assets and theme markers remain the product's own files.

## Verification scope

The mount change passed 74 actual-SQL service assertions and 14 targeted C# runner assertions. These cover prefix routing, origin rejection, scoped cookies, existing recovery/key access, cross-salt mint refusal, prefixed grants, stable retry evidence, revocation and unconfigured billing. A mounted Release publish completed; its route/header files, base element, package instructions and gzip/Brotli bootstrap equality were inspected. The extracted current example client displayed its command help successfully.

Those checks are local SQL, runner-consumer and built-artifact evidence. Actual BinSite HTTPS routing and mounted browser delivery are verified by the integrating host after it pins and publishes this source revision. The standalone deployment remains a separate publication.
