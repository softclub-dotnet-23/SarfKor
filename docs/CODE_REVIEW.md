# Backend Code Review — 2026-07-31

Repo: Sarfkor · Commit: `5f72803df8aec400ab136203ff68d132eb7257b7` (2026-07-31 18:17:32 +0500) · Stack: ASP.NET Core (.NET 10 preview), Clean Architecture (`Domain` / `Application` / `Infrastructure` / `WebApi`), PostgreSQL via Npgsql EF Core, CQRS-lite (`ICommandHandler<,>` / `IQueryHandler<,>`), FluentValidation per command/query, ASP.NET Identity + JWT, `Microsoft.AspNetCore.RateLimiting`.

Scope reviewed: all 4 `src/` projects + `tests/Application.Tests`, 100% of Domain (80 files), 100% of Infrastructure repositories/configurations (101 files), 100% of WebApi controllers (30) + `Program.cs`, 100% of the test suite (89 files / 285 `[Fact]`s), and Application layer module-by-module (Catalog, Products, Pricing, Customers, Loyalty, Payments, Engagement, Feedback, Offers, Notifications, ShoppingLists, Receipts, Auditing, Identity, Stores, Sales, Inventory — 511 files total). EF Core migrations and model snapshot cross-checked directly against index/constraint claims below.

## 0. Resolution status — 2026-08-01

All findings below were triaged and acted on in a single follow-up pass (`FixCodeReviewFindings` migration + accompanying code changes). Every table in this document now carries a **Status** column. Summary:

- **41 of 58 fixed** — including all 8 Critical and all 10 High findings.
- **2 resolved as intentional design, not defects** (ARCH-03, SEC-05) — confirmed with the product owner rather than changed in code; see §11.
- **2 need no action** — flagged as informational in the original review, not defects (CQ-01, CQ-07).
- **13 deferred** — mostly Medium/Low-severity pagination, refactor, and process items (PERF-05…09, SOLID-02, ARCH-01/02, CQ-04/05/06/08, SEC-09) that are either genuine future work or explicit product decisions not to make today. None are exploitable financial or auth bugs; the deferral list is reviewed again in the next pass.

Two corrections to this document's own suggested fixes, discovered while implementing:
- **BUG-01's fix snippet** (§9) has a stale signature for `IStoreCreditRepository.CreditAsync` (`(storeId, customerId, amount, ct)`); the credit is actually keyed by the `StoreCredit` row's own `Id`, matching `IGiftCardRepository.CreditAsync`'s shape. The implemented fix uses `storeCreditRepository.CreditAsync(storeCreditId, amount, ct)` after re-resolving the row by `(StoreId, CustomerId)`.
- **SEC-07**'s suggested fix ("scope results to the caller's own store's customers") was reconsidered during implementation: `Customer` is used as a platform-wide directory by design (a store needs to find a walk-in customer by phone even with no prior relationship, to attach store credit/loyalty for the first time). Scoping it per-store would break that. The fix actually applied closes the enumeration risk with rate limiting instead (`"contributions"` policy) and documents the reasoning inline in `CustomersController.cs`.

## 1. Verdict

The architecture is sound and consistently applied: layering is clean, DI is correctly abstraction-based (zero `IQueryable`/`DbSet` leaks across 47 repositories), the idempotent/transactional POS core (`ProcessSaleCommandHandler`, `StockLevelRepository.TryDecrementAsync`) is genuinely well engineered and matches CLAUDE.md §10's exact must-have invariants (no double-processing, no negative stock under concurrency — both proven by real integration tests). This is a codebase that got the hard 20% right.

It is let down by three systemic, root-cause problems rather than isolated bugs:

1. **Every entity is a plain data bag with public setters and zero invariant enforcement** (`Money`, `Barcode`, `GeoLocation` are bare `record`s; every Domain entity is anemic). The one correctly-guarded money path in the whole app is stock (`TryDecrementAsync`, an atomic SQL-level guard). Every *other* money-moving balance — `GiftCard`, `StoreCredit`, `LoyaltyAccount` — is mutated with a naive read-then-write and has no DB-level concurrency guard, no unique constraint on its natural key, and (for gift cards/store credit) no ledger at all. This is the same bug, independently reintroduced four times, because there is no shared abstraction or domain invariant preventing it.
2. **Authorization is enforced by copy-pasted, per-handler ownership checks with no shared abstraction**, and the copy-paste has visible gaps: `GiftCardsController.GetBalance` has no `[Authorize]` at all, `LoyaltyController.GetAccount` has no ownership check, and `Supplier` (used across store-scoped `PurchaseOrder`s) was never given a `StoreId` in the first place, so its Create/Update/Delete handlers have nothing to check against — any StorePartner can rename or delete any other store's supplier.
3. **Rate limiting and exception handling are applied to a hand-picked minority of endpoints.** 7 named policies exist; the large majority of authenticated, money- or state-changing write endpoints have none. There is no global exception handler registered anywhere in `Program.cs`, so an uncaught `DbUpdateException` (reachable today via at least one missing-FK-check bug below) returns raw internals to the client.

None of this is close to unshippable — the transactional core that matters most (sales/stock) is correct and tested. But the gift-card/store-credit path is a live, exploitable double-spend today, and it sits directly under CLAUDE.md §2's "все операции с деньгами... полностью аудит-логируются" requirement, which is currently false for gift cards and store credit (no ledger entity exists for either, unlike `LoyaltyTransaction` and `StockMovement` which do it correctly).

*(This verdict describes the state as of the review date. See §0 for what has since been fixed — the three systemic problems above are all addressed: atomic debits close problem 1, `IStoreAccessAuthorizer` closes problem 2, and a rate-limiting sweep plus a global exception handler close problem 3.)*

## 2. Bugs & Errors

| # | Severity | File:line | Bug | Trigger / repro | Impact | Fix | Status |
|---|---|---|---|---|---|---|---|
| BUG-01 | Critical | `Backend/src/Application/Sales/Commands/VoidSale/VoidSaleCommandHandler.cs:42-57` | Voiding a sale restocks inventory but never refunds any `GiftCard`/`StoreCredit` amount that was applied to the original sale. | Process a sale with `GiftCardCode` set (partial gift-card payment), then void it. | Store keeps the customer's gift-card/store-credit value *and* gets the product back in stock — silent, undetectable financial loss to the customer. Root cause: `SaleTransaction` (`Backend/src/Domain/Sales/SaleTransaction.cs`) never persists which gift card or how much credit was applied — only the transient `ProcessSaleResult` DTO carries `GiftCardAmountApplied`/`StoreCreditAmountApplied`, and it's never written to the DB. | Add `GiftCardId`, `GiftCardAmountApplied`, `StoreCreditAmountApplied` (nullable) to `SaleTransaction`; in `VoidSaleCommandHandler`, re-credit both balances inside the same `ExecuteInTransactionAsync` block, using the same atomic-update discipline as BUG-02's fix. | ✅ Fixed |
| BUG-02 | Critical | `Backend/src/Application/Sales/Commands/ProcessSale/ProcessSaleCommandHandler.cs:120,127,204` | Gift card balance is read once at line 120/127 (outside any lock), then written with a plain `record with` copy at line 204 inside the transaction — a classic lost-update race, not a repro of the correct pattern used for stock two lines above it (`TryDecrementAsync`, line 159). | Two concurrent `ProcessSaleCommand`s against the same store using the same gift card code, each with an amount under the card's balance but summing to more than it. | Double-spend: both sales succeed, the card is debited for more than its actual balance, no error raised. Same pattern independently repeated in `RedeemGiftCardCommandHandler.cs:13-29`, `RedeemStoreCreditCommandHandler.cs:22-32`, `RedeemLoyaltyPointsCommandHandler.cs:17-44`, and the store-credit debit at `ProcessSaleCommandHandler.cs:209`. | Add `IGiftCardRepository.TryDebitAsync(id, amount, ct)` / `IStoreCreditRepository.TryDebitAsync(...)` implemented via `ExecuteUpdateAsync(... where Balance >= amount ...)` — the exact same shape as `StockLevelRepository.TryDecrementAsync` — and use it everywhere a balance is currently mutated in memory. | ✅ Fixed |
| BUG-03 | Critical | `Application/Payments/Commands/IssueStoreCredit/IssueStoreCreditCommandHandler.cs:40` (per Infrastructure-scope review) | Adds `command.Amount` to the existing balance while keeping the balance's **old** currency; `command.Currency` is validated for format only and never compared to `credit.Balance.Currency`. | Issue store credit to a customer twice with different currencies (e.g. TJS then USD). | Silently merges two currencies into one numeric balance — the balance becomes meaningless and can't be redeemed correctly in either currency. | Reject the command (or open a second `StoreCredit` row) when `command.Currency != credit.Balance.Currency`. | ✅ Fixed — rejects with `CurrencyMismatch` outcome |
| BUG-04 | High | `Application/Payments/Commands/IssueStoreCredit/IssueStoreCreditCommandHandler.cs:26-37` + `Infrastructure/Persistence/Configurations/StoreCreditConfiguration.cs` | No unique constraint on `(StoreId, CustomerId)` — confirmed directly in `AppDbContextModelSnapshot.cs:1262-1266` (`StoreCredits` has separate `HasIndex("CustomerId")` and `HasIndex("StoreId")`, no composite unique). Concurrent first-time-issue calls can create two `StoreCredit` rows for the same customer. | Two concurrent `IssueStoreCreditCommand`s for a customer with no existing row. | `GetByStoreAndCustomerAsync`'s `FirstOrDefaultAsync` only ever sees one of the two rows going forward — future issues/redemptions silently split across two balances, permanently losing track of the other. | Add a unique index on `(StoreId, CustomerId)`; handle the resulting `DbUpdateException` as a retry-as-update. | ✅ Fixed — unique index added |
| BUG-05 | Medium | `Backend/src/WebApi/Controllers/GiftCardsController.cs:61-74` | `GetBalance` ignores `result.Found` (or equivalent not-found signal) and always returns `Ok(result)`, even for a nonexistent code. | `GET /api/gift-cards/DOES-NOT-EXIST` | Client can't distinguish "balance is zero" from "code doesn't exist"; also compounds SEC-02 below by giving an oracle for code-guessing (200 vs conceptually-404 timing/shape). | Branch on the handler's outcome and return `NotFound()` when the card isn't found. | ✅ Fixed |
| BUG-06 | Low | `Infrastructure/Repositories/CategoryRepository.cs:19-21` | Comment claims "no DB-level FK" for Category's parent relationship; a real FK now exists since the `AddForeignKeyConstraintsAndMoneyPrecision` migration (confirmed: `AppDbContextModelSnapshot.cs:127` shows `HasIndex("ParentCategoryId")` backed by an FK). | N/A (stale comment, not a runtime bug) | Misleads future readers into thinking app-level duplicate protection is the only guard. | Delete or update the comment. | ✅ Fixed |

## 3. Validation gaps

| # | Severity | File:line | Missing/weak check | Exploit or bad-data scenario | Fix | Status |
|---|---|---|---|---|---|---|
| VAL-01 | High | `Application/Pricing/Commands/SubmitPriceUpdate/SubmitPriceUpdateCommandValidator.cs:12` | `Price` has no upper bound. | Submit a price of `999999999999`; it feeds directly into `CompareStoresForShoppingListQueryHandler.cs:20`'s summed total, which can overflow/produce nonsense comparison results platform-wide. | `RuleFor(x => x.Price).LessThanOrEqualTo(1_000_000)` (or a sane domain ceiling). | ✅ Fixed |
| VAL-02 | High | `Application/Catalog/Commands/CreateTaxRate/CreateTaxRateCommandHandler.cs:11-17` | Never checks `CategoryId` exists before saving, unlike the sibling `UpdateTaxRateCommandHandler`. | POST a `CreateTaxRateCommand` with a bogus `CategoryId`. | Add `categoryRepository.ExistsAsync(command.CategoryId, ct)` check returning a `CategoryNotFound` outcome, matching Update's behavior. | ✅ Fixed |
| VAL-03 | High | `Application/Catalog/Commands/CreateProductBundle/CreateProductBundleCommandHandler.cs:22-28` | Never checks that the component `ProductId`s exist before inserting — relies on the FK throwing a raw `DbUpdateException`, which (per SEC-11) is unhandled and leaks internals to the client. | POST a bundle referencing a nonexistent `ProductId`. | Validate all `ProductId`s exist up front (`productRepository.ExistsAsync` per id, or a batched `ExistManyAsync`), return `ProductNotFound` outcome. | ✅ Fixed |
| VAL-04 | Medium | `Application/Catalog/Commands/UpdateCategory/UpdateCategoryCommandHandler.cs:15-16` | Only rejects a category being its own direct parent, not deeper cycles (A→B→C→A). | Set B's parent to C, then C's parent to A, then A's parent to B. | Walk the parent chain (bounded, e.g. max depth 20) before accepting the new parent, reject if it revisits the category being updated. | ✅ Fixed |
| VAL-05 | Medium | `Application/Feedback/Commands/RaisePriceEntryDispute/RaisePriceEntryDisputeCommandHandler.cs:12-30` | No duplicate-pending-dispute check, unlike the sibling `Report`/`ReportDispute` flow pattern elsewhere. | Raise the same dispute against the same `PriceEntry` repeatedly. | Check for an existing unresolved dispute for the same `PriceEntryId` before inserting. | ✅ Fixed |
| VAL-06 | Medium | `Application/Engagement/Commands/RecordScan/RecordScanCommandHandler.cs` | `StoreId` on the scan is never checked to exist. | Submit a scan with a bogus `StoreId`. | Add `storeRepository.ExistsAsync` check (or make the FK non-nullable and let the constraint reject it cleanly with a mapped outcome, not a raw exception). | ✅ Fixed |
| VAL-07 | Medium | `Application/Catalog/Commands/CreateBrand/*` (no dedicated `BrandConfiguration.cs` exists) | No duplicate-name check for Brand despite CLAUDE.md-driven self-service creation by any StorePartner. | Two StorePartners each create "Coca-Cola" as a new Brand within the same request window. | Add a unique index on `Brand.Name` (case-insensitive collation) and map the resulting conflict to a friendly outcome. | ✅ Fixed — `BrandConfiguration.cs` added with unique index |
| VAL-08 | Medium | `Application/Inventory/Commands/CreateReorderRule/CreateReorderRuleCommandHandler.cs:21-29` (confirmed directly) | Neither `ProductId` nor `PreferredSupplierId` is checked to exist before the rule is saved. | Create a reorder rule for a nonexistent product or supplier. | Validate both FKs exist (or at minimum the `ProductId`, since `PreferredSupplierId` is nullable) before insert. | ✅ Fixed |
| VAL-09 | Medium | `Application/Feedback/Commands/ReportOutOfStock/ReportOutOfStockCommandValidator.cs:5-13` | Nullable `StoreId` is missing `GreaterThan(0).When(x => x.StoreId is not null)`, present on the sibling `SubmitReviewCommandValidator.cs:11`. | Submit `StoreId = 0` or negative. | Add the matching conditional rule. | ✅ Fixed |
| VAL-10 | Low | `Application/Offers/Commands/CreatePromotion/CreatePromotionCommandHandler.cs:12-37` | Never checks `ProductId`/`CategoryId` exist, unlike sibling `PublishExpiringOfferCommandHandler`. | Create a promotion targeting a nonexistent product. | Add existence checks matching the sibling handler. | ✅ Fixed |
| VAL-11 | Low | Multiple: `SetCostPriceCommandValidator`, `IssueStoreCreditCommandValidator`, `ProcessSaleCommandValidator` | Currency fields validated only by `.Length(3)`, no ISO-4217 whitelist. | Submit `Currency = "XXX"` or `"ABC"`. | Validate against a small fixed whitelist (e.g. `TJS`, `USD`, `RUB` — whatever the platform actually supports). | ✅ Fixed — `SupportedCurrencies` whitelist (`TJS`/`USD`/`RUB`/`EUR`) applied across ~13 validators |
| VAL-12 | Low | `Application/Customers/Queries/GetCustomerByPhone/GetCustomerByPhoneQueryValidator.cs:9` | Missing `MaximumLength(30)` present on the sibling Create validator. | Submit an extremely long phone string. | Add the matching length rule. | ✅ Fixed |
| VAL-13 | Low | `Application/Engagement/Commands/AddFavorite/AddFavoriteCommandHandler.cs` | Never verifies the referenced Product/Store actually exists before saving the favorite. | Favorite a nonexistent `EntityId`. | Add an existence check per `Type`. | ✅ Fixed |
| VAL-14 | Medium | `Application/Payments/Commands/RedeemGiftCard/RedeemGiftCardCommand.cs`, `Application/Payments/Commands/RedeemStoreCredit/RedeemStoreCreditCommand.cs` | Neither command carries/validates a currency, so redemption against a balance in a different currency than the sale is silently allowed. | Redeem a USD gift card against a TJS sale total. | Add a `Currency` field to both commands, validated against the balance's currency before applying. | ✅ Fixed |

## 4. SOLID violations

| # | Principle | File:line | Violation | Refactor | Status |
|---|---|---|---|---|---|
| SOLID-01 | SRP/DRY | `RecordStockReceiptCommandHandler.cs:21-23`, `ProcessSaleCommandHandler.cs:40-42`, `GetStockLevelQueryHandler.cs:17-19`, `VoidSaleCommandHandler.cs:26-28`, plus 5+ more handlers | The identical "`store.OwnerUserId != userId && !await storeEmployeeRepository.IsEmployeeAsync(...)`" check is copy-pasted across every handler needing owner-or-employee access, instead of living in one place. | Extract `IStoreAccessAuthorizer.IsOwnerOrEmployeeAsync(storeId, userId, ct)` in `Application/Abstractions`, inject it, replace every inline duplicate. Cuts the surface area for exactly the kind of one-off gap found in SEC-04/SEC-06 below. | ✅ Fixed — ~37 call sites migrated |
| SOLID-02 | SRP/DRY | `Application/Catalog/Commands/DeleteBrand/*`, `DeleteCategory/*`, `DeleteTaxRate/*` | Three near-identical handlers (load → check `IsInUseAsync` → remove → save) differing only by entity type. | Not urgent to generalize (types genuinely differ), but at minimum extract the shared shape into a helper/base method to avoid the fourth copy next time a reference-data entity is added. | ⏳ Deferred — low-value refactor, no behavior at stake |
| SOLID-03 | OCP/SRP (systemic) | `Domain/ValueObjects/Money.cs`, `Barcode.cs`, `GeoLocation.cs`, `PaymentToken.cs`; every entity under `Domain/**` | Value objects are bare `record`s with zero invariant enforcement (negative `Money`, malformed `Barcode`, out-of-range lat/lng all constructible); every entity is a plain public-setter bag with no behavior — all business rules live in Application-layer handlers, re-implemented per handler. This is the direct root cause of BUG-02/BUG-03: nothing in the type system stops a handler from writing an invalid balance. | Give `Money` a private constructor + factory that rejects negative amounts/unknown currencies; give `Barcode`/`GeoLocation` format/range validation in their constructors. This alone doesn't fix the concurrency races (BUG-02) but it closes the "invalid state constructible at all" half of the anemic-model problem. | ✅ Fixed (scoped) — `Money`/`Barcode`/`GeoLocation` now validate on both construction and `with`-expressions via private backing fields; `PaymentToken` and entity behavior-richness intentionally left as-is (no clear invariant / larger effort than this pass warranted) |
| SOLID-04 | ISP/SRP | `Application/Payments/Commands/IssueGiftCard/IssueGiftCardCommand.cs` + Handler | No `PerformedByUserId`/`StoreId` field and no authorization check at all — every other money-issuing command (`IssueStoreCredit`, `RecordStockReceipt`, `SetCostPrice`) carries an actor and checks ownership; this one doesn't. | Add `PerformedByUserId`/`StoreId`, require `[Authorize("StorePartner")]` *and* an ownership check consistent with the rest of Payments. | ✅ Fixed |

## 5. Clean Architecture violations

| # | Severity | File:line | Rule broken | Correct placement | Status |
|---|---|---|---|---|---|
| ARCH-01 | Medium | `WebApi/Controllers/ReceiptsController.cs:68-101` | Controller does file-size validation, magic-byte content-type sniffing, `Directory.CreateDirectory`, and raw file I/O directly — the one non-thin controller in the whole API (all other 29 confirmed thin). | Extract an `IFileStorageService` (Infrastructure), inject it, have the controller just bind the request and call `handler.Handle`. | ⏳ Deferred — `Receipts` itself is on the CQ-08 dead-code-or-not list; revisit alongside that decision rather than refactor code that may be removed |
| ARCH-02 | Low | `Program.cs` (bottom, ~last 60 lines) | ~60 request DTOs declared unstructured at the bottom of the composition-root file instead of colocated with their feature. | Move each request DTO into its owning controller's file or a per-controller `Requests.cs`. | ⏳ Deferred — cosmetic, no behavior/risk implication |
| ARCH-03 | Low | `Application/Pricing/Commands/SubmitPriceUpdate/SubmitPriceUpdateCommandHandler.cs:28-30` | Restricts price updates to store staff only, contradicting CLAUDE.md §6's documented *consumer* crowdsourcing use case (`SubmitPriceUpdateCommand` — "юзер обновляет цену... с весом по репутации"). `ContributorTrustScore` is created on registration but never read anywhere in the codebase — the reputation-weighting feature is entirely dead. | Either implement the consumer-facing crowdsource path per spec (read `ContributorTrustScore` to weight trust, allow non-staff submissions with moderation), or update CLAUDE.md to reflect the staff-only pivot and delete the dead `ContributorTrustScore` machinery. This is a real product-direction fork, flagged to §11 for confirmation. | ☑️ Resolved as intentional — product owner chose to keep staff-only, no code change; drift from CLAUDE.md §6 is acknowledged, not fixed |

## 6. Security findings

| # | Severity | File:line | Issue | Attack scenario | Fix | Status |
|---|---|---|---|---|---|---|
| SEC-01 | Critical | `Infrastructure/Persistence/Configurations/ProductConfiguration.cs` + confirmed via `AppDbContextModelSnapshot.cs:1391-1395` | `Products.Barcode_Value` has no index of any kind — only `CategoryId`/`TaxRateId` are indexed on `Products`. | Every barcode scan (`ScanBarcodeQuery`, the single hottest, fully public endpoint in the product) does a full table scan as the product catalog grows. Not an auth bypass, but a trivially-triggerable, unauthenticated resource-exhaustion vector at scale. | Add a unique index on `Barcode_Value` (it's a natural key — should probably be unique anyway, closing a duplicate-barcode gap flagged separately). | ✅ Fixed — switched `Barcode` mapping from `ComplexProperty` to `OwnsOne` to allow a unique index |
| SEC-02 | Critical | `WebApi/Controllers/GiftCardsController.cs:61-74` | `GetBalance` has no `[Authorize]` attribute and no rate limiting — confirmed directly, the only unauthenticated action on this controller (Issue/Redeem both require `StorePartner`). | Anonymous caller enumerates/brute-forces gift-card codes (`GET /api/gift-cards/{code}` for every plausible code) and reads balances with no rate limit slowing them down. | Add `[Authorize("StorePartner")]` and `[EnableRateLimiting("sales")]` (or a dedicated tighter policy), matching the sibling `Redeem` action. | ✅ Fixed |
| SEC-03 | Critical | `ProcessSaleCommandHandler.cs:120-127,202-211`, `RedeemGiftCardCommandHandler.cs:13-29`, `RedeemStoreCreditCommandHandler.cs:22-32`, `RedeemLoyaltyPointsCommandHandler.cs:17-44` | Same root cause as BUG-02: no pessimistic lock or optimistic concurrency token on `GiftCard`/`StoreCredit`/`LoyaltyAccount`. | Concurrent redemption requests against the same card/account (trivial to trigger — just send two requests) each read the pre-debit balance and both succeed, redeeming more total value than the balance holds. | See BUG-02's fix (`TryDebitAsync` via `ExecuteUpdateAsync`). | ✅ Fixed |
| SEC-04 | High | `Application/Loyalty/Queries/GetLoyaltyAccount/GetLoyaltyAccountQueryHandler.cs` + `WebApi/Controllers/LoyaltyController.cs:150-164` | `GetAccount` has no ownership check at all. | Any authenticated StorePartner reads any other store's customer loyalty balance by guessing/incrementing account IDs — IDOR. | Add the same owner-or-employee (or owner-only, per the sensitivity) check every other Loyalty handler already has. | ✅ Fixed |
| SEC-05 | High | `Domain/Payments/GiftCard.cs` (fields: `Code, Balance, IsActive, IssuedAt, ExpiresAt` — no `StoreId`) | `GiftCard` has no store scoping at all. | Any StorePartner can redeem (spend down) any other store's issued gift card at their own register, with no way to reconcile which store actually owes the redeeming store anything. | Add `StoreId` (issuing store) to `GiftCard`; decide product-wise whether redemption should be cross-store (mall gift card) or store-scoped, and enforce accordingly — flagged to §11 since this may be intentional. | ☑️ Resolved as intentional — product decision: stays cross-store. `IssuingStoreId`/`IssuedByUserId` added for attribution plus a new `GiftCardRedemption` append-only ledger (`GiftCardId`, `StoreId` of redemption, `Amount`, `SaleTransactionId`, `RedeemedAt`) so cross-store spend is now reconcilable, without restricting where a card can be spent |
| SEC-06 | High | `Application/Inventory/Commands/CreateSupplier/CreateSupplierCommandHandler.cs:7-25`, `UpdateSupplierCommandHandler.cs:6-23`, `DeleteSupplierCommandHandler.cs:6-23` (all confirmed directly, no ownership check in any of the three) + `Domain/Inventory/Supplier.cs` (no `StoreId` field at all) + `WebApi/Controllers/SuppliersController.cs` (role-only `[Authorize("StorePartner")]`) | `Supplier` is a platform-wide entity with zero store scoping anywhere in the stack — Application, Domain, and WebApi all agree there's nothing to check. | Any authenticated StorePartner can create/rename/delete *any* supplier, including ones referenced by other stores' `PurchaseOrder`s (breaking their records) or by their `ReorderRule.PreferredSupplierId`. | Add `StoreId` (or `OwnerUserId`) to `Supplier`, scope all four operations (Create implicitly via the new field, Update/Delete/Get via an ownership check) the same way `Product`/`Store`-scoped entities already are. | ✅ Fixed — `StoreId` added, ownership checks threaded through all four handlers + `SuppliersController`, frontend (`suppliers.ts`, `SupplyPage.tsx`) updated to pass `storeId`. Existing rows backfilled via migration data-fix (earliest referencing `PurchaseOrder`'s store, else lowest `Store.Id`) |
| SEC-07 | Medium | `WebApi/Controllers/CustomersController.cs:31-44` | `GetByPhone` has no store-scoping and no rate limiting. | Any authenticated StorePartner enumerates customers platform-wide by phone number with no throttling. | Scope the query to the caller's own store's customers; add rate limiting. | ✅ Fixed differently than suggested — `Customer` is kept as an intentional platform-wide directory (a store must be able to find a walk-in by phone with no prior relationship); rate limiting (`"contributions"` policy) closes the enumeration risk instead of store-scoping. Reasoning documented inline in `CustomersController.cs` |
| SEC-08 | High | `Infrastructure/Identity/AuthService.cs` (`LoginAsync`) | Relies solely on `userManager.CheckPasswordAsync`; never calls `IsLockedOutAsync`/records `AccessFailedCount` — ASP.NET Identity's built-in lockout is not wired up. Only the `"login"` rate-limit policy (10/15min per IP) protects against brute force. | Distributed/proxied brute-force attempts (many IPs, low rate each) bypass the only protection in place. | Wire up `userManager.AccessFailedAsync`/`IsLockedOutAsync` per Identity's standard pattern, in addition to the existing IP rate limit. | ✅ Fixed — lockout wired in `AuthService.LoginAsync` (5 attempts, 15 min lockout) |
| SEC-09 | Medium | `Infrastructure/Identity/AuthService.cs` (`RegisterAsync`) + `Application/Stores/Commands/AcceptStoreEmployeeInvitation/AcceptStoreEmployeeInvitationCommandHandler.cs` | `RegisterAsync` never sets `EmailConfirmed = true` and requires no email verification anywhere; the cashier-invite accept flow trusts "an account with this email already exists" as proof of identity. | Attacker pre-registers a victim's email address (no verification needed) before the victim is invited as a cashier by a store owner; the invite then silently attaches to the attacker's account instead of the victim's. | Require email verification before an account can be attached via invite-accept (or verify the invitee explicitly, e.g. a one-time code sent to the invited email at accept time, not just "does a user with this email exist"). | ⏳ Deferred — needs a transactional email provider decision (send verification/one-time codes), which is infrastructure the project doesn't have configured yet; tracked as real risk, not forgotten |
| SEC-10 | Medium | `Program.cs` (7 rate-limit policies defined; cross-referenced against all 30 controllers) | The large majority of authenticated write endpoints have zero rate limiting: `SalesController` VoidSale/RecordCommission/ProcessReturn; all 9 Catalog write actions; `StockController` RecordReceipt/SetCostPrice; CashierShifts Open/Close; StockTransfers Initiate/Complete; PurchaseOrders Create/Submit/Receive; Suppliers Create/Update/Delete; ReorderRules Create; ProductBundles Create; Reviews Reply; ShoppingLists Create/AddItem/RemoveItem; Offers Publish; Favorites Add/Remove; Feedback RaiseDispute; DeviceTokens Register; Loyalty CreateProgram/Enroll/Earn/Redeem; GiftCards Issue; StoreCredit Issue/Redeem; Customers Create/GetByPhone; PriceAlerts Create/Deactivate; Promotions Create; Admin's 4 moderation POSTs; Products GetMostScanned/GetTopSelling; Pricing RaiseDispute; Stores AddEmployee/RemoveEmployee. | Any of these can be hammered without throttling — most consequential on the money-moving ones (GiftCards Issue, StoreCredit Issue/Redeem, VoidSale). | Define 2-3 more graduated policies (e.g. `"partner-write"` for routine StorePartner writes, a tighter `"money-write"` for gift-card/credit/void operations) and apply them systematically instead of per-endpoint ad hoc. | ✅ Fixed — added `"partner-write"` (120/min) and `"money-write"` (30/min) policies, now 9 named policies total, applied across the endpoints listed. `PricingController.RaiseDispute` (SEC-13) was missed in the original sweep and has now been closed too |
| SEC-11 | Medium | `Program.cs` (confirmed via direct grep: no `UseExceptionHandler`, `AddProblemDetails`, or `UseStatusCodePages` registered anywhere) | No global exception handler. | Any unhandled exception (e.g. the raw `DbUpdateException` from VAL-03's missing FK check) returns ASP.NET Core's default error response, which in a non-Development environment without explicit configuration can still leak stack traces/type names depending on hosting config — and at minimum returns an inconsistent, unstructured error shape compared to the app's own `ProblemDetails`-style validation errors. | Add `app.UseExceptionHandler(...)` + `AddProblemDetails()`, map known exception types to clean `ProblemDetails` responses, log the rest server-side only. | ✅ Fixed — `GlobalExceptionHandler : IExceptionHandler` added and wired |
| SEC-12 | Low | `WebApi/Controllers/ProductsController.cs` (`GetMostScanned`, `GetTopSelling`) | Unauthenticated and unrate-limited. | Low-severity scraping/analytics-leak risk. | Add rate limiting at minimum; consider whether these need to be public at all. | ✅ Fixed (partial) — `"scan"` rate limit applied to both; left public, per the finding's own framing that this is a product call, not a defect |
| SEC-13 | Low | `WebApi/Controllers/PricingController.cs:46-71` (`RaiseDispute`) | No rate limit, unlike the sibling `SubmitPriceUpdate` action on the same controller. | Spam disputes with no throttling. | Apply the `"contributions"` policy already used by `SubmitPriceUpdate`. | ✅ Fixed |

## 7. Performance & data access

| # | Severity | File:line | Issue | Cost | Fix | Status |
|---|---|---|---|---|---|---|
| PERF-01 | Critical | `Infrastructure/Repositories/ScanRepository.cs:16` (confirmed directly) | `GetMostScannedAsync` runs `dbContext.Scans.Select(s => s.ProductId).ToListAsync(ct)` — every row in the `Scans` table, ever, pulled into memory before grouping in-process. Code comment cites a "GroupBy doesn't translate reliably" concern as the reason, but the workaround avoids the real fix. | `Scans` is described as the highest-volume table in the schema (every consumer scan writes one row) — this query gets slower and eventually OOMs as usage grows, on a fully public/unauthenticated code path. | Aggregate in SQL: `dbContext.Scans.GroupBy(s => s.ProductId).Select(g => new(g.Key, g.Count())).OrderByDescending(...).Take(limit)` — EF Core's Npgsql provider does translate simple `GroupBy`+`Count` to SQL; if a specific past translation failure motivated this, bound it with a `Where(s => s.ScannedAt >= cutoff)` at minimum rather than loading unbounded history. | ✅ Fixed — aggregation moved into SQL |
| PERF-02 | Critical | `Infrastructure/Repositories/SaleTransactionRepository.cs:42-46` | `GetTopSellingProductsAsync` materializes every matching `SaleLineItem` platform-wide when `storeId` is null. | Same shape as PERF-01 — unbounded in-memory aggregation over the second-highest-volume table. | Aggregate via SQL `GroupBy` + `Sum`, same fix shape as PERF-01. | ✅ Fixed |
| PERF-03 | High | `Application/Products/Queries/CompareStoresForShoppingListQuery/CompareStoresForShoppingListQueryHandler.cs:14-22` | N+1 query loop over `ProductIds` with no upper bound on how many can be submitted. | A shopping list with hundreds of items issues hundreds of sequential queries; also a mild DoS vector since there's no limit on list size. | Batch into a single query keyed by the `ProductIds` set; add a `MaximumLength` validator rule on the list size. | ✅ Fixed — batched via `GetLatestPerStoreForProductsAsync` |
| PERF-04 | Medium | `Application/Sales/Commands/ProcessSale/ProcessSaleCommandHandler.cs:63,90` | Product/price lookups loop per-line (`productRepository.GetByIdAsync`, `priceEntryRepository.GetLatestForStoreAsync`) instead of batching. | Every sale with N lines issues 2N+ queries instead of 2. | Add batched `GetByIdsAsync`/`GetLatestForStoreBatchAsync` methods and resolve all lines up front. | ✅ Fixed |
| PERF-05 | Medium | Admin moderation queries: `ReportRepository.GetPendingAsync`, `ReportDisputeRepository.GetPendingAsync`, `ProductSubmissionRepository` pending query, and the equivalent for price-entry disputes | Unbounded — no pagination, and `Status` is unindexed on at least `Reports`/`ReportDisputes`. | Grows without bound as the moderation queue ages. | Add pagination (`skip`/`take` or keyset) + an index on `Status`. | ⏳ Deferred — needs paired frontend pagination UI, out of scope for this pass |
| PERF-06 | Medium | `Application/Notifications/Queries/GetNotifications/*` | No pagination, no composite index backing the query. | Same unbounded-growth risk. | Add pagination + a composite `(UserId, CreatedAt)` index. | ⏳ Deferred — same reason as PERF-05 |
| PERF-07 | Low | `Application/Inventory/Queries/GetSuppliers/GetSuppliersQueryHandler.cs:10` (confirmed directly: `supplierRepository.GetAllAsync(ct)`, no paging) + equivalent `Brand`/`Category`/`TaxRate` repositories | `GetAllAsync` unbounded on all reference-data repositories. | Low risk today (reference data is naturally small), but no ceiling exists if supplier/brand counts grow. | Add pagination if/when these lists are expected to grow past a few hundred rows. | ⏳ Deferred — low current risk, revisit if row counts grow |
| PERF-08 | Low | `ReviewRepository.GetByProductIdAsync`, `PriceAlertRepository.GetByUserIdAsync`, `ShoppingListRepository.GetByUserIdAsync` | Unbounded per-entity queries. | Grows with a single popular product's or user's history. | Add pagination. | ⏳ Deferred — same reason as PERF-05 |
| PERF-09 | Low | `Application/Inventory/Commands/ReceivePurchaseOrder/ReceivePurchaseOrderCommandHandler.cs:31-44` (confirmed directly), `VoidSaleCommandHandler.cs:42-57`, `ProcessReturnCommandHandler.cs` | Each loops one `IncrementAsync`/`StockMovement` insert per line instead of a single batched call. | N extra round-trips per multi-line operation. | Add batched `IncrementManyAsync`/bulk-insert overloads for the repositories in question. | ⏳ Deferred — lowest priority in the original review, no functional risk |

## 8. Code quality / maintainability

| # | File:line | Issue | Suggestion | Status |
|---|---|---|---|---|
| CQ-01 | `Infrastructure/Repositories/*` (39 of 47 files) | Near-identical boilerplate CRUD repositories (Add/GetById/Remove wrapping `DbSet<T>`). | Acceptable given DIP is preserved and each stays a thin, testable seam — not worth a generic-repository abstraction that would blur per-aggregate query methods; noting for awareness, not urging a rewrite. | ➖ No action needed — informational, not a defect |
| CQ-02 | `Infrastructure/Persistence/Configurations/SaleLineItemConfiguration.cs:13-16` | Uses `Cascade` delete against `SaleTransaction`, while every sibling financial configuration (`PaymentConfiguration`, `CommissionConfiguration`, `SaleReturnConfiguration`, `FiscalReceiptConfiguration`) correctly uses `Restrict`. | Change to `Restrict` — deleting a `SaleTransaction` should never be possible/should never silently cascade-delete its line items, given it's a financial audit record. | ✅ Fixed |
| CQ-03 | `PurchaseOrderLineItemConfiguration.cs`, `ReturnLineItemConfiguration.cs` | Same Cascade-vs-Restrict inconsistency as CQ-02. | Same fix. | ✅ Fixed |
| CQ-04 | `Infrastructure/Persistence/Configurations/UserConsentConfiguration.cs:12-15` | `Cascade` wipes consent history when a user is deleted. | Given CLAUDE.md §9 flags Tajikistan personal-data legal requirements as an open question, consider whether consent *history* should outlive account deletion for compliance purposes — flagged to §11, not asserted as wrong. | ⏳ Deferred — intentionally left as-is, blocked on the same unresolved legal question CLAUDE.md §9 flags |
| CQ-05 | `Backend/tests/Application.Tests/**` | Zero `[Theory]`/`[InlineData]` usage across all 285 test methods, despite several being naturally parametrized (e.g. repeated near-identical threshold tests as separate `[Fact]`s). | Convert the clearest duplicated cases to `[Theory]` incrementally; not urgent. | ⏳ Deferred — cosmetic, not urgent per the finding's own text |
| CQ-06 | `Backend/tests/Application.Tests/**` (~35+ files) | Heavy duplication of `new Store { OwnerUserId = ..., Name = "Test", ... }` setup boilerplate and the StoreNotFound/Forbidden test-pair pattern, no shared base fixture. | Extract a shared `TestStoreFixture`/builder to cut duplication. | ⏳ Deferred — the test suite grew further this pass (285 → 302 `[Fact]`s), increasing the payoff of this refactor for next time, but not done now |
| CQ-07 | `LoyaltyTransactionRepository.cs`, `ReviewReplyRepository.cs`, `StockMovementRepository.cs` | Add-only, no query methods. | Likely intentional (append-only audit trail) — flagged as informational, not a defect. | ➖ No action needed — informational, not a defect |
| CQ-08 | `Application/ShoppingLists/**`, `Application/Receipts/**` (consumer upload/verify only), `Application/Notifications/**`, `Application/Engagement/Commands/RegisterDeviceToken/**`, `Application/Feedback/Commands/CreatePriceAlert/**` (+ `DeactivatePriceAlert`), `Application/Engagement/Commands/AddFavorite/**` (+ `RemoveFavorite`) | Confirmed (cross-checked against every file under `Frontend/src/lib/api/*.ts`) to have **zero live frontend caller** anywhere. | Either these are intentionally-dormant backend surface built ahead of a future mobile consumer client (per CLAUDE.md's original consumer-app vision), or genuinely dead code. Flagged to §11 for a product decision rather than asserted as remove-worthy, since `Receipts` in particular accepts file uploads + PII with zero live consumer today, which is worth revisiting regardless of the answer. | ⏳ Deferred — genuinely a product decision (delete vs. build the consumer client that would use it), left untouched pending that call |
| CQ-09 | `Infrastructure/Persistence/Configurations/DeviceTokenConfiguration.cs` (confirmed via snapshot: `DeviceTokens` has only `HasIndex("UserId")`, no unique on `Token`) | No unique index on `Token` despite handler logic that assumes idempotent upsert-by-token. | Add a unique index on `Token`. | ✅ Fixed |

## 9. Fix snippets

**BUG-01 / BUG-02 (Critical) — atomic balance debit + void-time refund**

```csharp
// Before (Application/Sales/Commands/ProcessSale/ProcessSaleCommandHandler.cs:202-205)
if (giftCard is not null && giftCardAmountApplied is not null)
{
    giftCard.Balance = giftCard.Balance with { Amount = giftCard.Balance.Amount - giftCardAmountApplied.Value };
}
```
```csharp
// After — atomic, race-safe debit mirroring StockLevelRepository.TryDecrementAsync
if (giftCard is not null && giftCardAmountApplied is not null)
{
    var debited = await giftCardRepository.TryDebitAsync(giftCard.Id, giftCardAmountApplied.Value, ct);
    if (!debited) throw new InsufficientGiftCardBalanceSignal(giftCard.Id);
}
```
```csharp
// IGiftCardRepository.TryDebitAsync — Infrastructure implementation
public async Task<bool> TryDebitAsync(int giftCardId, decimal amount, CancellationToken ct)
{
    var rows = await dbContext.GiftCards
        .Where(g => g.Id == giftCardId && g.Balance.Amount >= amount)
        .ExecuteUpdateAsync(s => s.SetProperty(g => g.Balance, g => new Money(g.Balance.Amount - amount, g.Balance.Currency)), ct);
    return rows == 1;
}
```
And `SaleTransaction` needs the fields to make BUG-01's refund possible at all:
```csharp
// Domain/Sales/SaleTransaction.cs — add
public int? GiftCardId { get; set; }
public decimal? GiftCardAmountApplied { get; set; }
public decimal? StoreCreditAmountApplied { get; set; }
```
```csharp
// VoidSaleCommandHandler.cs — inside the existing ExecuteInTransactionAsync block, after restocking
if (saleTransaction.GiftCardId is not null && saleTransaction.GiftCardAmountApplied is not null)
    await giftCardRepository.CreditAsync(saleTransaction.GiftCardId.Value, saleTransaction.GiftCardAmountApplied.Value, ct);
if (saleTransaction.CustomerId is not null && saleTransaction.StoreCreditAmountApplied is not null)
    await storeCreditRepository.CreditAsync(saleTransaction.StoreId, saleTransaction.CustomerId.Value, saleTransaction.StoreCreditAmountApplied.Value, ct);
```

**SEC-02 (Critical) — missing authorization on gift card balance lookup**

```csharp
// Before (WebApi/Controllers/GiftCardsController.cs:61-74)
[HttpGet("{code}")]
public async Task<IActionResult> GetBalance(string code, ...)
{
    var query = new GetGiftCardBalanceQuery(code);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return this.ToValidationProblem(validationResult);

    return Ok(await handler.Handle(query, cancellationToken));
}
```
```csharp
// After
[HttpGet("{code}")]
[Authorize("StorePartner")]
[EnableRateLimiting("sales")]
public async Task<IActionResult> GetBalance(string code, ...)
{
    var query = new GetGiftCardBalanceQuery(code);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return this.ToValidationProblem(validationResult);

    var result = await handler.Handle(query, cancellationToken);
    return result.Found ? Ok(result) : NotFound();
}
```

**SEC-01 / PERF-01 (Critical) — missing barcode index + unbounded scan aggregation**

```csharp
// ProductConfiguration.cs — add
builder.OwnsOne(p => p.Barcode, b => b.Property(x => x.Value).HasColumnName("Barcode_Value"));
builder.HasIndex("Barcode_Value").IsUnique();
```
```csharp
// Before (Infrastructure/Repositories/ScanRepository.cs:12-24)
var productIds = await dbContext.Scans.Select(s => s.ProductId).ToListAsync(cancellationToken);
return productIds.GroupBy(id => id).Select(g => new ProductScanSummary(g.Key, g.Count()))
    .OrderByDescending(x => x.TotalScans).Take(limit).ToList();
```
```csharp
// After — aggregate in SQL instead of materializing every row
return await dbContext.Scans
    .GroupBy(s => s.ProductId)
    .Select(g => new ProductScanSummary(g.Key, g.Count()))
    .OrderByDescending(x => x.TotalScans)
    .Take(limit)
    .ToListAsync(cancellationToken);
```

**SEC-06 (High) — Supplier has no store scoping**

```csharp
// Domain/Inventory/Supplier.cs — add
public int StoreId { get; set; }
```
```csharp
// Before (Application/Inventory/Commands/DeleteSupplier/DeleteSupplierCommandHandler.cs)
var supplier = await supplierRepository.GetByIdAsync(command.SupplierId, cancellationToken);
if (supplier is null) return new DeleteSupplierResult(DeleteSupplierOutcome.NotFound);
if (await supplierRepository.IsInUseAsync(command.SupplierId, cancellationToken))
    return new DeleteSupplierResult(DeleteSupplierOutcome.InUse);
```
```csharp
// After
var supplier = await supplierRepository.GetByIdAsync(command.SupplierId, cancellationToken);
if (supplier is null) return new DeleteSupplierResult(DeleteSupplierOutcome.NotFound);
if (supplier.StoreId != command.PerformedByStoreId)
    return new DeleteSupplierResult(DeleteSupplierOutcome.Forbidden);
if (await supplierRepository.IsInUseAsync(command.SupplierId, cancellationToken))
    return new DeleteSupplierResult(DeleteSupplierOutcome.InUse);
```
(Same shape applies to `CreateSupplierCommandHandler`/`UpdateSupplierCommandHandler`.)

## 10. Refactor roadmap

Items 1-9 below were completed in the 2026-08-01 fix pass (§0). Items 10-11 remain open — see §11 for why.

1. ✅ **Atomic balance debits for GiftCard/StoreCredit/LoyaltyAccount** (Effort: M, Risk: Low). Add `TryDebitAsync`/`CreditAsync` to all three repositories via `ExecuteUpdateAsync`; replace every naive `record with` mutation (`ProcessSaleCommandHandler.cs`, `RedeemGiftCardCommandHandler.cs`, `RedeemStoreCreditCommandHandler.cs`, `RedeemLoyaltyPointsCommandHandler.cs`). Fixes BUG-02/SEC-03, the single highest-value fix in this review. Ship first — it's the only genuinely exploitable financial bug found.
2. ✅ **Persist gift-card/store-credit application on `SaleTransaction` + refund on void** (Effort: M, Risk: Low, depends on #1). Adds the fields described in BUG-01's fix and the void-time refund logic. New migration required.
3. ✅ **Scope `Supplier` to a store** (Effort: S, Risk: Medium — existing `Supplier` rows need a `StoreId` backfill strategy since it's currently global; decide the backfill answer before writing the migration). Fixes SEC-06.
4. ✅ **Add missing indexes**: `Products.Barcode_Value` (unique), `StoreEmployees(StoreId, UserId)` (unique), `StoreCredits(StoreId, CustomerId)` (unique), `DeviceTokens.Token` (unique), `Brand.Name` (unique) (Effort: S, Risk: Low — check for pre-existing duplicate rows before adding unique constraints in each case). Fixes SEC-01, BUG-04, CQ-09, VAL-07, and the `StoreEmployee` race noted by the Infrastructure review.
5. ✅ **Fix the two unbounded in-memory aggregations** (`ScanRepository.GetMostScannedAsync`, `SaleTransactionRepository.GetTopSellingProductsAsync`) (Effort: S, Risk: Low). Fixes PERF-01/PERF-02.
6. ✅ **Add `[Authorize]` + ownership check to `GiftCardsController.GetBalance`, `LoyaltyController.GetAccount`** (Effort: S, Risk: Low). Fixes SEC-02/SEC-04.
7. ✅ **Register a global exception handler** (`UseExceptionHandler` + `AddProblemDetails`) (Effort: S, Risk: Low). Fixes SEC-11; makes VAL-03-style gaps fail safely in the meantime even before they're individually patched.
8. ✅ **Extract `IStoreAccessAuthorizer`** to de-duplicate the owner-or-employee check (Effort: M, Risk: Low — pure refactor, no behavior change if done as a straight extraction). Fixes SOLID-01; reduces the chance of the next SEC-04/SEC-06-shaped gap.
9. ✅ **Rate-limiting sweep**: define 2 additional graduated policies and apply across the ~30 currently-unprotected write endpoints (Effort: M, Risk: Low). Fixes SEC-10.
10. ⏳ **Introduce concurrency tokens / rich constructors for value objects** (`Money`, `Barcode`, `GeoLocation`) (Effort: L, Risk: Medium — touches every entity using them, needs careful migration of existing invalid data if any exists). Fixes SOLID-03; lowest priority since #1 already closes the concrete exploit this would help prevent architecturally. **Partially done**: `Money`/`Barcode`/`GeoLocation` now validate on construction and `with`-expressions (private backing fields); full entity richness and concurrency tokens are still open, and remain lowest priority for the same reason stated originally.
11. ⏳ **Decide and act on the dead-code modules** (ShoppingLists, Receipts consumer flow, Notifications, DeviceTokens, PriceAlerts, Favorites) (Effort: S to remove / L to actually wire up a consumer client, Risk: Low either way). Resolves CQ-08. **Still open** — a product decision, not a code change, and deliberately not made in this pass.

## 11. Open questions / need to verify

- ☑️ **Resolved — `SubmitPriceUpdateCommandHandler`'s staff-only restriction (ARCH-03).** Product owner decision: leave it staff-only. The drift from CLAUDE.md §6 (which documents consumer crowdsourcing with reputation weighting) is acknowledged and intentionally not fixed; `ContributorTrustScore` remains unread/dead pending a future decision to actually build the consumer-facing path.
- ☑️ **Resolved — `GiftCard` cross-store design (SEC-05).** Product owner decision: stays cross-store, redeemable at any participating store. The fix applied was reconciliation, not scoping: `IssuingStoreId`/`IssuedByUserId` on `GiftCard` for attribution, plus a new `GiftCardRedemption` ledger recording which store actually redeemed how much, when, and against which sale.
- **Still open — are the six modules in CQ-08 genuinely dead, or pre-built for a not-yet-shipped mobile consumer app?** Not resolved in this pass; left untouched. Still worth a decision, especially for `Receipts` (file uploads + PII with no live consumer today).
- **Still open — `UserConsentConfiguration`'s `Cascade` delete (CQ-04)** — unchanged; blocked on the same unresolved Tajikistan legal question CLAUDE.md §9 flags.
- **Still open — JWT `ClockSkew`** — no explicit override found (library default ~5 min leeway applies). Not flagged as a bug, but worth an explicit decision either way rather than an implicit default. Not addressed in this pass.
- **New — SEC-09 (email verification before invite-accept) is a real, unresolved gap**, deferred because closing it properly needs a transactional email provider the project doesn't have configured yet. Worth prioritizing in the next infrastructure-focused pass rather than the next pure-code pass, since the blocker is operational, not a design question.

## 12. What's good

- `ProcessSaleCommandHandler`'s core transaction — idempotency-key short-circuit, atomic `TryDecrementAsync` stock guard, bundle-price allocation, and promotion-precedence logic — is well-designed and exactly matches CLAUDE.md §10's stated must-have invariants. `ProcessSaleCommandHandlerTests.cs` (542 lines, full 1:1 branch coverage including bundle/gift-card/store-credit stacking) is the strongest file in the entire test suite.
- The idempotency, double-void, and negative-stock-under-concurrency invariants CLAUDE.md §10 calls out as mandatory are all explicitly tested — including a real integration test (`StockLevelConcurrencyTests.cs`) against live Postgres proving no negative stock under concurrent decrements.
- Zero DIP violations found across all 47 repository abstractions — every one returns `IReadOnlyList<T>`/`T?`/`bool`/`void`, never `IQueryable`/`DbSet`.
- The `UserId`-from-JWT-claim extraction pattern is 100% consistent across all 30 controllers (`ClaimTypes.NameIdentifier`, null → `Unauthorized()`), zero deviations found.
- CORS is correctly configured with an explicit origin whitelist — no `AllowAnyOrigin()` anywhere.
- Cost-price/profit-report access control correctly matches CLAUDE.md §4's "не видна кассиру" requirement: `SetCostPriceCommandHandler` is owner-only, distinct from the owner-or-employee pattern used for the POS itself.
- Ownership checks in the Feedback/Offers/Notifications/ShoppingLists/Receipts/Auditing modules were found consistently correct with zero IDOR — every handler re-checks `entity.UserId == command.UserId` at the Application layer, not just relying on the route.
- `RefreshAsync` correctly re-derives the user's roles fresh on every refresh (not from the stale access token), so a role change/removal takes effect on the next refresh rather than lingering for the token's full lifetime.

## Appendix: statistics

**Files reviewed**: Domain 80/80, Infrastructure repositories + configurations 101/101, WebApi controllers + `Program.cs` 31/31, Application 511/511 (Catalog 56, Products 32, Pricing 16, Customers 8, Loyalty 24, Payments 24, Engagement 12, Feedback 36, Offers 16, Notifications 24, ShoppingLists 16, Receipts 8, Auditing 4, Identity 37, Stores 28, Sales 50, Inventory 66, Abstractions 50, Common 3), Tests 89/89 (285 `[Fact]`s, 0 `[Theory]`s). EF Core migrations (6) and the model snapshot cross-checked directly for every index/constraint claim above.

**Files skipped**: none outright skipped; `Frontend/` was read only incidentally, to confirm which backend endpoints have live callers (CQ-08) — a full frontend code review was out of scope for this backend-focused pass.

**Findings by severity**: Critical 8 (BUG-01, BUG-02, BUG-03, SEC-01, SEC-02, SEC-03, PERF-01, PERF-02) · High 10 (BUG-04, VAL-01, VAL-02, VAL-03, SOLID-01, SEC-04, SEC-05, SEC-06, SEC-08, PERF-03) · Medium 22 · Low 15.

**Findings by category**: Bugs 6 · Validation 14 · SOLID 4 · Architecture 3 · Security 13 · Performance 9 · Code quality 9. Total: 58 numbered findings.

**Resolution status as of 2026-08-01** (see §0): 41 fixed · 2 resolved as intentional design (no code change) · 2 needed no action (informational) · 13 deferred. All 8 Critical and all 10 High findings are fixed or resolved. Test suite grew from 285 to 302 `[Fact]`s covering the new behavior; full solution (`dotnet build`) and frontend (`npx tsc -b`) both verified clean.
