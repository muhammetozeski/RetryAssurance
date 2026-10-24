# Independent service portal

The English Blazor application owns the complete account-to-evidence workflow. Its layout is specific to Retry Assurance: a hard-color dark workbench, execution steps, durable-effect counters, request journal and explicit role/capability controls. It does not embed a BinSite layout. The independently reusable audited appearance algorithm is adapted only as a functional guard.

`/account` creates or recovers an actual secret-backed account. Recovery keys and newly created project capabilities exist only in their explicit view's memory until the visitor clears the view or navigates away. They are not written to local storage. Private key downloads and clipboard actions require explicit user actions. Invitations use a fragment and require an actual recipient account.

`/projects/new` declares key ownership, allowed runner terminal observations and the selected pack before creating an immutable first contract revision. `/projects/:id` reads actual roles, contract history, capabilities and executions. Contract changes use the original expected revision; refreshing data does not silently replace an unsaved contract editor. Owner membership controls and revocation are real service actions. A new runner key is not fabricated from a local form.

`/runs/:id` reads server-owned allowlisted request/resource evidence and distinctly labels runner declarations. `/compare/:current/:baseline` reads two real records from the same project, preserving their separate effects and checking compatible pins. The replay command starts a new actual execution and includes a contract revision, not a secret. Empty projects contain no inserted demonstration runs.

`/runner` provides the actual compiled-example build/invocation path. `/account/billing` shows actual provider configuration, mode and server entitlement. An unconfigured checkout is unavailable and returning from a checkout URL is not treated as payment. `/privacy` explains actual data storage, transport and observation limits.

## Appearance and publication markers

The document starts with `data-ra-theme="dark"`. An explicit light preference can be stored under `retry-assurance:appearance:v1`. Runtime contrast correction and the theme control use `window.RetryAssuranceAppearance`; `RetryAssuranceAppearance.audit()` reports the current theme, checked text elements, repairs and remaining contrast failures. Minimum text contrast is 4.5. Dynamic content and focus/hover changes are observed.

The functional asset delivery markers are `data-ra-appearance="style"` on `/appearance.css` and `data-ra-appearance="script"` on `/appearance.js`. The control is placed in the product's own `[data-ra-theme-control]` region. The unchanged audited algorithm was renamed/adapted from the existing appearance guard; no additional contrast result is claimed before reading the deployed native DOM.

`scripts/Publish-Portal.ps1` compiles the standalone application into the static `public` root. Source lives under `src/RetryAssurance.Portal`; generated runtime/assets are ignored in Git and recreated in the GitHub workflow. Existing API, redirect and response-header configuration is retained. `/build-info.json` records the actual source commit and build time. Backend verification and actual C# HTTPS examples were completed separately from portal rendering; a successful Razor build alone is not a browser acceptance claim.
