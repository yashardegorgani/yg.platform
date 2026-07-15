# YG — A Modular Monolith in .NET 10

> A plugin-based, context-driven modular monolith built from scratch as a learning-in-public
> project: PostgreSQL schema-per-module, Wolverine messaging, FastEndpoints, Keycloak
> authentication, and database-driven RBAC — with **zero hard-coded module registrations**.

---

## Why this project exists

Most sample "modular monoliths" are either a folder convention or a microservices
diagram in denial. The goal here was different: build a monolith where **module
boundaries are physically enforced**, not politely suggested — and learn *why* each
rule exists by breaking it first.

The requirements I set at the start:

| Goal | How it ended up |
|---|---|
| Generic, scalable, extendable | Modules are self-contained plugins; adding one touches zero Host code |
| No hard-coded module registrations | Reflection-based `ModuleLoader` scans a `plugins/` folder at startup |
| Plug-in system | Any `YG.Modules.*.dll` implementing `IYGModule` is discovered, config can disable it |
| Module communication without coupling | Wolverine in-process messaging through tiny `*.Contracts` projects |
| PostgreSQL + JSONB, multi-schema | Every module owns exactly one schema; flexible attributes live in `jsonb` |
| Configurable, context-based RBAC | Permission-based: endpoints declare capabilities; role→permission mapping is runtime data with an admin API |
| Concern & boundary separation | Vertical-slice features, contracts-only cross-module references |

## The system at a glance

```
                          ┌──────────────────────────────────────────┐
                          │                 YG.Host                  │
                          │  JWT auth · role claims · FastEndpoints  │
                          │  Wolverine bus · plugin loader           │
                          └────────────────────┬─────────────────────┘
                             loads at startup  │  (no compile-time refs)
              ┌──────────┬──────────┬──────────┴───┬─────────────┐
              ▼          ▼          ▼              ▼             ▼
         ┌─────────┐┌─────────┐┌─────────┐  ┌───────────┐ ┌──────────┐
         │Identity ││ Access  ││ Catalog │  │ Inventory │ │ Purchase │
         │ schema: ││ schema: ││ schema: │  │  schema:  │ │  schema: │
         │identity ││ access  ││ catalog │  │ inventory │ │ purchase │
         └─────────┘└─────────┘└─────────┘  └───────────┘ └──────────┘

         Messages between modules (via *.Contracts projects only):
           Identity ──UserRegistered──────────▶ Access, Catalog
           Catalog  ──ProductCreated──────────▶ Inventory
           Purchase ──GetProductSnapshot?────▶ Catalog   (request/response)
           Purchase ──ReserveStock?──────────▶ Inventory (request/response)
```

### Tech stack

- **.NET 10 / ASP.NET Core** — Host + class-library plugin modules
- **[FastEndpoints](https://fast-endpoints.com/)** — REPR-style HTTP endpoints, secure by default
- **[Wolverine](https://wolverinefx.net/)** — in-process command/event bus (static handlers, codegen)
- **EF Core + Npgsql** — PostgreSQL, one schema per module, `jsonb` + GIN for flexible attributes
- **Keycloak** — OIDC authentication (credential custody, token issuance)
- **PostgreSQL** — single database, five schemas, per-schema migration history

## Core architectural ideas

### 1. Modules are plugins, not references

The Host has **no project references to any module**. Each module sets
`<IsPluginModule>true</IsPluginModule>`; an MSBuild target copies its self-contained
output to `plugins/<ModuleName>/` after every build. At startup the `ModuleLoader`
scans that folder, instantiates every `IYGModule`, and asks each one to register its
own services. Configuration can switch any module off:

```json
{ "Modules": { "Catalog": { "Enabled": false } } }
```

Delete a module's folder and the system still boots — endpoints 404, nothing else
notices. That is the boundary test a folder convention can't pass.

### 2. Contracts are the only doorway between modules

A module's internals (entities, DbContext, handlers) are invisible to every other
module. When module B needs something from module A, A publishes a tiny
`YG.Modules.A.Contracts` project containing only records. Two patterns:

- **Events** (fire-and-forget, 0..n listeners): `UserRegistered`, `ProductCreated`
- **Requests with replies** (exactly one answerer): `GetProductSnapshot → ProductSnapshot`,
  `ReserveStock → ReservationResult`

Contracts are *earned, not scaffolded*: they exist only when a real consumer arrives.
Purchase has no Contracts project — nothing consumes Purchase yet.

### 3. One schema per module, no cross-schema joins

Every module's `DbContext` derives from `ModuleDbContext`, declares its schema via a
`static abstract` member (`ISchemaOwner`), and keeps its own `__ef_migrations`
history table in that schema. Rules that make it matter:

- **Reference-by-ID**: Inventory stores Catalog's `ProductId` as a plain `Guid` — no
  foreign key across schemas, ever.
- **Snapshot-at-boundary**: Purchase copies the product's name and price into the
  order row at purchase time. Catalog can rename or reprice tomorrow; order history
  doesn't rewrite itself.
- Migrations are applied automatically at startup (`Database:AutoMigrate`), generated
  at design time with a factory that never touches a real database.

### 4. Identity is rented, authorization is owned

Keycloak answers exactly one question: *who is this?* (credential custody, OIDC
tokens). Everything after that is owned by the application:

1. JWT arrives → standard bearer validation (issuer, signature).
2. An `IClaimsTransformation` asks the **Access module** (through an
   `IUserRolesProvider` port — dependency inversion, the Host never references Access)
   for the user's roles **and the permissions those roles grant**, from the database.
3. FastEndpoints enforces a declared capability per endpoint —
   `Permissions("catalog.products.create")` — never a role name.

Roles *and* permissions live in PostgreSQL, not in the token — granting or revoking
takes effect on the **next request**, no re-login, and policy data stays
queryable/reportable like any other data. Ownership is split three ways: each module
owns its permission *names* (`inventory.stock.set` is Inventory's vocabulary), the
Access module owns *which roles hold them* (editable at runtime via its own API), and
role names appear nowhere in feature code — inventing a `stock-clerk` role that can
restock but not buy is pure data, zero recompiles. Modules never see `HttpContext` or
claims; they consume a three-member `IUserContext` abstraction.

### 5. Vertical slices, one concern per file

No `Controllers/`, `Services/`, `Repositories/` layers. Each feature folder holds its
message, handler, and endpoint as separate files: `PlaceOrder.cs`,
`PlaceOrderHandler.cs`, `PlaceOrderEndpoint.cs`. Cohesion over categorization.

## A request, end to end

`POST /api/purchase/orders { productId, quantity }` with a member token:

1. **Host**: validates the JWT, loads roles from the Access schema, builds `IUserContext`.
2. **PlaceOrderEndpoint** (Purchase): checks the `purchase.orders.place` permission, resolves *who* from
   `IUserContext`, sends a `PlaceOrder` command — HTTP stops here.
3. **PlaceOrderHandler**: asks Catalog `GetProductSnapshot` (name + price), asks
   Inventory `ReserveStock` — an **atomic conditional decrement** (`UPDATE … WHERE
   quantity >= @qty`), so two concurrent buyers can never both take the last unit.
4. On success: the order row is written to `purchase.orders` with snapshot values.
   On failure: `409` with a machine-readable reason (`insufficient-stock`, `unknown-product`).

Meanwhile, every `ProductCreated` event fans out to Inventory, which opens a stock row
at 0 — Catalog has no idea Inventory exists.

## Repository layout

```
src/
├── YG.Host/                          # composition root: auth, bus, plugin loading
├── BuildingBlocks/
│   ├── YG.BuildingBlocks.Modules/    # IYGModule + ModuleLoader
│   ├── YG.BuildingBlocks.Persistence/# ModuleDbContext, ISchemaOwner, auto-migrate
│   └── YG.BuildingBlocks.Auth/       # IUserContext, IUserRolesProvider (zero deps)
└── Modules/
    ├── Identity/    (+ .Contracts)   # Keycloak registration, /me, UserRegistered
    ├── Access/                       # roles, grants, role→permission mapping, bootstrap admins
    ├── Catalog/     (+ .Contracts)   # products with jsonb attributes, snapshots
    ├── Inventory/   (+ .Contracts)   # stock, ProductCreated subscriber, reservations
    └── Purchase/                     # orders with snapshot-at-boundary
plugins/                              # build output the Host actually loads
```

## Running it locally

**Prerequisites:** .NET 10 SDK, Docker.

1. **Infrastructure** — `docker compose up -d` (PostgreSQL 16 + Keycloak 26).
2. **Keycloak** (one-time): create realm `yg`; a public client `yg-api` with direct
   access grants (login); a confidential client `yg-admin` with a service account
   holding `realm-management/manage-users` (used by the registration API).
3. **Configuration** — set the connection string, `Auth:Authority`, and the
   `Keycloak:*` section in `appsettings.Development.json` (secrets belong in user
   secrets / environment variables, not in the repo).
4. **Run the Host** (F5 or `dotnet run`). Migrations for all five schemas apply
   automatically. `GET /` lists the loaded modules.

### Try the flow

```bash
# register a user (creates it in Keycloak AND in identity.users)
curl -X POST http://localhost:5000/api/identity/register \
  -H "Content-Type: application/json" \
  -d '{"username":"omid","email":"omid@test.local","password":"Omid123!"}'

# login
curl -X POST http://localhost:8080/realms/yg/protocol/openid-connect/token \
  --data-urlencode "grant_type=password" \
  --data-urlencode "client_id=yg-api" \
  --data-urlencode "username=omid" \
  --data-urlencode "password=Omid123!"

# create a product (permission catalog.products.create — held via the auto-granted member role)
curl -X POST http://localhost:5000/api/catalog/products \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"Keyboard","price":49.9,"attributes":{"brand":"YG","tags":["peripheral"]}}'

# stock it (needs inventory.stock.set — admin), then buy it (needs purchase.orders.place — member)
curl -X PUT http://localhost:5000/api/inventory/stock/$PRODUCT_ID \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{"quantity":10}'

curl -X POST http://localhost:5000/api/purchase/orders \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"productId":"'$PRODUCT_ID'","quantity":2}'
```

Order 11 units and you get `409 insufficient-stock` — the failure crosses the module
boundary as data, not as an exception.

## Decisions worth reading (the journal highlights)

| Decision | Choice | Why |
|---|---|---|
| Roles in tokens vs database | **Database** (Keycloak = authN only) | Instant revocation, queryable/reportable role data, future workflow control |
| Endpoint guards | **Permissions**, not role names | Endpoints declare capabilities they own; who holds them is runtime data |
| Permission storage | Table, not `jsonb` array on Role | Policy data must stay relationally queryable; per-item grants, audit-ready |
| Contracts placement | Sibling project per module | Owned by the publisher, referenced by consumers, invisible internals |
| Cross-schema foreign keys | **Forbidden** | Schemas must be independently evolvable; reference-by-ID instead |
| Purchase snapshots product data | Copy name + price at purchase | History must not rewrite itself when Catalog changes |
| Stock reservation | Single conditional `UPDATE` | Correct under concurrency without locks or retries |
| Layer folders (`Handlers/`, `Commands/`) | **Rejected** | Kills feature cohesion; vertical slices + one concern per file |
| `Update-Database` in dev | **Never** | Startup auto-migration is the single write path to the schema |

## Honest limitations & roadmap

- **In-memory bus**: events die with the process, and reserve-then-save in Purchase is
  not atomic across schemas. Next step: Wolverine's EF Core **durable outbox**.
- **Architecture tests**: NetArchTest rules (contracts-only references, no
  `HttpContext` inside modules, no module→Host references) to make the boundaries
  fail the build instead of the code review.
- **Secrets hygiene**: move the Keycloak admin secret and DB password to environment
  variables / user secrets.
- Per-request role/permission caching, admin-token caching for the Keycloak client.
- Guard rail against revoking `access.roles.manage` from the last steward role
  (currently recoverable only by SQL — a deliberate, documented gap).

---

*Built step by step — every rule in this repo was adopted because skipping it hurt
first. The commit history is the real documentation.*
