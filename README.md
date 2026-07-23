# YG.Platform — a modular monolith workflow platform

**License:** source-available, all rights reserved — see [LICENSE](LICENSE). Contact for licensing inquiries.

> One deployable. Six modules. Zero shortcuts across boundaries.

**.NET 10 · Wolverine · FastEndpoints · PostgreSQL (multi-schema + JSONB) · EF Core · Keycloak · Preact**

YG.Platform is a modular monolith hosting a generic, definition-driven **workflow engine** next to real business modules (catalog, inventory, purchasing) — with plug-in module loading, message-only module boundaries, runtime-configurable RBAC, and a single-file operator console that includes a visual workflow designer.

Built as a deliberate architecture study: every decision below was made on purpose, and the commit history reads as a narrative — **one commit, one concept**.

## Architecture at a glance

```
src/
  YG.Host/            ← the only executable; discovers modules at startup
  Modules/
    Identity/         ← user registry (mirrors Keycloak identities)
    Access/           ← RBAC: roles, permissions, grants — runtime-configurable
    Catalog/          ← products
    Inventory/        ← stock levels
    Purchase/         ← orders
    Workflow/         ← the engine: definitions, instances, tasks, history
plugins/              ← drop a module DLL here; the Host finds it
```

- **One process, hard boundaries.** Modules never reference each other's internals. All cross-module communication is Wolverine messages against `*.Contracts` — and architecture tests fail the build if anyone cheats.
- **No hard-coded registration.** The Host scans `plugins\`; each module registers its own services, endpoints, schema and workflow activities. Adding a module = dropping a DLL.
- **A schema per module.** One PostgreSQL database, one schema per module, no cross-schema joins. The database enforces the same boundary the code does.
- **RBAC as data.** Keycloak authenticates; the Access module authorizes. Permissions like `workflow.instances.cancel` are granted to roles at runtime through the API — no redeploy.

## The workflow engine

Processes are **data, not code** — versioned JSON definitions with a governance lifecycle (Draft → Pending approval → Published). Instances pin their definition version forever.

- **Human steps** — role-addressed task inboxes: claim, complete, reassign, notes.
- **Automatic steps** — pluggable activities contributed by business modules (`inventory.check-stock`, `purchase.place-order`, `catalog.snapshot-product`, …) discovered at startup. Failure is data: retry, pend for human rescue, or continue — chosen per step.
- **Routing** — condition-guarded transitions over the accumulated JSONB context, first match wins; parallel fan-out; join-all convergence; legal cycles.
- **Sub-workflows** — instances spawn child instances; results bubble home under a result key; recursion is depth-guarded; cancellation cascades down the family and pends the parent upward.
- **History** — append-only audit of everything, with actor snapshots frozen at write time.

## The console

A single static file (`wwwroot/index.html`) — no SPA build server, by constraint and by choice. Dark operator UI: task inboxes with dynamic forms, definition governance, a **visual workflow builder** with live lint (no JSON required), instance inspector with parallel-aware breadcrumbs (⑂ ⨝ 🪺 ×N), parent↔child lineage navigation, stuck-work rescue, and cancellation with audited reasons.

## Proven, not promised

Every feature was closed with a drill against the running system: requester bounce-backs, approval money-gates, restock cycles, parallel fan-out + join, child workflow spawn and merge, cascade cancellation, and RBAC refusals (403s on purpose).

## Status

A learning artifact with production-shaped decisions — pre-hardening by choice. Known road to production: join race guard, publish-time graph validator rules with unit tests, Auth Code + PKCE, secrets hygiene, observability, a clean-plugins build step. The full story — every choice and its reason — lives in the tutorial document.
