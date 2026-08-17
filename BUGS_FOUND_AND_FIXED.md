# Full-codebase review — bugs found and fixed (2026-08-10/11)

Scope: everything the previous targeted review missed — the full Admin platform-operator feature
(subscriptions, store lifecycle, users, trust score, reference data, diagnostics, audit log —
`ADMIN_PROMPT.md`) plus a re-check of moderation removal, brand merge, and trust-score decay. Not
a literal line-by-line read of all ~700 source files (not feasible in one pass); prioritized by
what CLAUDE.md and ADMIN_PROMPT.md themselves flag as load-bearing: money, access control, data
integrity. Everything below was fixed in this same pass unless marked otherwise, verified by a
clean `dotnet build`, a clean `npm run build`, and 414/414 backend tests passing (408 pre-existing
+ 6 new, added to cover the one previously-untested handler this pass touched most).

---

## 1. [CRITICAL — fixed] Suspended stores could use almost the entire cabinet anyway

**This is the headline finding.** ADMIN_PROMPT.md §2.1 is explicit: *"Suspended: партнёрский кабинет
и касса закрыты — все изменяющие операции магазина возвращают ошибку с явным кодом «подписка
неактивна»."* Acceptance criterion #8 restates it. In practice, only **2 of ~40** store-write
command handlers (`ProcessSaleCommandHandler`, `RecordStockReceiptCommandHandler`) actually checked
`IStoreAccessAuthorizer.IsOperationalAsync`. A store that stopped paying and got auto-`Suspended`
could still: void sales, open/close cashier shifts, process returns, record commissions, hire and
manage cashiers, edit the store profile, run the entire purchasing/supplier/reorder pipeline, issue
and redeem gift cards and store credit, run loyalty programs, create promotions and expiring
offers, and submit prices. Only ringing up a new sale and receiving new stock were actually blocked.
This defeats the entire point of Part 1 of ADMIN_PROMPT — the subscription system exists specifically
to be the platform's leverage over non-paying stores, and it had almost none.

**Fixed**: added the same `IsOperationalAsync` gate (a new `SubscriptionInactive` outcome value →
HTTP 402, matching the existing convention from the 2 handlers that already had it) to 30 more
command handlers:

- **POS/cashier**: `VoidSale`, `OpenCashierShift`, `CloseCashierShift`, `RecordCommission`, `ProcessReturn`
- **Staff/store management**: `CreateCashierAccount`, `CreateStoreEmployeeInvitation`, `RemoveStoreEmployee`, `SetStoreEmployeeActive`, `ResetCashierPassword`, `UpdateStore`, `UpdateStoreEmployee`
- **Inventory/procurement**: `CompleteStockTransfer`, `CreatePurchaseOrder`, `CreateReorderRule`, `CreateSupplier`, `DeleteSupplier`, `InitiateStockTransfer`, `ReceivePurchaseOrder`, `SetCostPrice`, `SubmitPurchaseOrder`, `UpdateSupplier`
- **Loyalty**: `CreateLoyaltyProgram`, `EarnLoyaltyPoints`, `EnrollCustomerInLoyalty`, `RedeemLoyaltyPoints`
- **Marketing/payments/pricing**: `CreatePromotion`, `PublishExpiringOffer`, `IssueGiftCard`, `IssueStoreCredit`, `RedeemGiftCard`, `RedeemStoreCredit`, `SubmitPriceUpdate`
- **Misc**: `CreateProductBundle`, `ReplyToReview`

**Deliberately left out** (not oversights — see reasoning):
- `ApproveStoreCommandHandler` — this is what *creates* the Trial subscription; gating it on itself is nonsensical.
- `ResendStoreEmployeeInvitationCommandHandler` / `RevokeStoreEmployeeInvitationCommandHandler` — both are also callable by a platform Admin (not just the store owner) for a platform-wide invite with no `StoreId` at all; gating them needs a real decision about whether Admin-initiated actions should ever be store-subscription-gated, which is outside a mechanical fix.
- `AskAssistantCommandHandler` — the AI chat itself is read-mostly; any actual mutation it proposes goes through `ConfirmAssistantAction`, which in turn calls the now-gated handlers above.

**Also fixed as part of this**: 53 existing unit tests broke because their `Mock<IStoreAccessAuthorizer>`
fakes had no `IsOperationalAsync` setup and defaulted to `false`. Patched all of them to grant
`IsOperationalAsync(...) → true` alongside the existing ownership grant. All 408 pre-existing tests
pass again.

---

## 2. [HIGH — fixed] "Изменить" on a cashier silently wiped their salary

Carried over from the previous review pass and fixed now. `Frontend/src/lib/api/stores.ts`'s
`updateStoreEmployee()` hardcoded `monthlySalaryAmount`/`Currency` to `null` on every call — the
backend treats null there as "clear," not "leave unchanged" (unlike the FirstName/LastName/PhoneNumber
fields, which do mean "leave unchanged"). Reproduced live before fixing: set a salary via SQL,
called the exact request the "Изменить" button sends, salary came back null.

**Fixed**: `updateStoreEmployee()` now takes explicit optional salary fields instead of hardcoding
them; `EditCashierModal` passes the employee's own current `monthlySalaryAmount`/`Currency` straight
through (added to the `StoreEmployee` TS type, which never exposed them before), so an edit that
has nothing to do with salary can no longer touch it.

---

## 3. [HIGH — fixed] Camera picker restarted the wrong device

Carried over and fixed. `useBarcodeScanner.ts`'s `selectDevice` called `start()` through a stale
closure — `selectDevice` was memoized on `[stop]` only and never recreated, so it permanently
called the *first* render's `start` (and therefore the *first* render's `selectedDeviceId`),
regardless of which camera was actually just picked. The dropdown label updated correctly; the
video feed didn't follow it.

**Fixed**: split the actual "open this device" logic into `startWithDevice(deviceId)`, which takes
the device id as an explicit parameter instead of reading it from a closure. Both `start()` and
`selectDevice()` now call it with the device id they actually mean.

---

## 4. [MODERATE-HIGH — fixed] Blanket 15s frontend timeout could kill legitimate 60s AI replies

Carried over and fixed. The backend's `AnthropicAssistantChatClient` deliberately allows 60s for a
real LLM round-trip; every `apiFetch` call (including assistant chat) inherited the same 15s
default with no override.

**Fixed**: added an optional `timeoutMs` to `ApiFetchOptions`, threaded through to
`fetchWithTimeout`; `assistant.ts`'s `chat()` now passes 65s.

---

## 5. [MODERATE — fixed] No way to remove a co-owner anywhere in the UI

Carried over and fixed. The old per-row "remove" button was dropped in the cashier-lifecycle
redesign and never replaced for `Owner` rows (only `Cashier` rows got the new edit/reset/disable
actions, matching that redesign's own explicit spec — that part stays as-is). Co-owners were left
with zero lifecycle actions, including no way to undo a mistaken invite.

**Fixed**: added a "Удалить" action back for `Owner` rows specifically (not self), reusing the
existing, now-gated `RemoveStoreEmployeeCommand`/endpoint.

---

## 6. [MODERATE — fixed] Co-owner rows showed a raw user-id fragment instead of a name

Carried over and fixed. `StoreEmployee.FirstName`/`LastName` are only ever populated for a
directly-created Cashier; a co-owner's real name lives in `UserProfile.DisplayName` instead, which
`GetStoreEmployeesQueryHandler` never consulted — so the redesigned list (whose whole point was
"never show raw ids") still showed something like `5e8313b8…` for every co-owner.

**Fixed**: the handler now falls back to `UserProfile.DisplayName` when `FirstName` is null (one
extra lookup per name-less row — a store realistically has one or two owners, not worth a batched
repository method); the frontend's `displayName` computation no longer requires both `firstName`
*and* `lastName` to be present.

---

## 7. [LOW-MODERATE — fixed] Disabling a cashier didn't revoke their refresh token

Carried over and fixed. Every other password/security-sensitive mutation in this codebase
(`ChangePasswordAsync`, `ResetPasswordAsync`, `AdminResetPasswordAsync`) explicitly revokes refresh
tokens "the moment an existing session should die." `SetStoreEmployeeActiveCommandHandler` (the
"Отключить" action) didn't — in practice likely masked by `IsActive` being re-checked live on every
store-scoped request, but inconsistent defense-in-depth.

**Fixed**: now calls `IRefreshTokenRepository.RevokeAllForUserAsync` when disabling (not when
re-enabling).

---

## 8. [LOW — fixed] Nothing stopped an owner from disabling their own account via a direct API call

Carried over and fixed. The frontend hid the disable button for the caller's own row; the backend
never checked it. `SetStoreEmployeeActiveCommandHandler` now returns a new `CannotDisableSelf`
outcome when the target `StoreEmployee.UserId` equals the caller's own id.

---

## 9. [LOW / accepted tradeoff, unchanged] EmailJobQueue is in-memory only

Noted in the previous pass, not changed here — a process restart silently drops any email still
queued (password resets, invitations), with no retry. Explicitly called out as an accepted tradeoff
in the code's own comment ("strictly worse before was a request that could 500/timeout the caller
outright"). Listed here again only for completeness of this document.

---

## 10. [LOW / edge case, unconfirmed, unchanged] "Evening" shift preset crosses midnight

Noted in the previous pass, not investigated further — `{ start: '18:00:00', end: '00:00:00' }` has
no ordering validation and I didn't find (or check) any downstream duration calculation this could
break. Flagged as a "look before relying on it" item, not a confirmed bug.

---

## New findings from this pass (Admin platform-operator feature)

Reviewed and found **no defects** in: `SubscriptionLifecycleJob` (the daily Trial→PastDue→Suspended
job, and the trust-score decay riding along with it), `MergeBrandsCommandHandler` (transactional,
audited, correctly rolls back partway through a multi-source merge), `TrustScoreFormula` (the
decay/corroboration math checks out), `GetStoreDiagnosticsQueryHandler`/`Result` (deliberately and
correctly excludes every commercial figure — revenue, cost, margin, receipt amounts — with a comment
explicitly warning against ever adding one back; the recent-errors/sync-conflict/client-version
fields ADMIN_PROMPT.md §2.6 asks for are absent on purpose too, since no error-logging or offline-sync
infrastructure exists anywhere in this codebase yet to source them from — inventing empty
placeholders would be worse than omitting the fields), and moderation removal (grepped for every
leftover moderation/dispute route and endpoint name — nothing found beyond explanatory comments in
migrations and doc-comments describing what replaced it).

**Out-of-scope observation, not fixed**: `SubmitPriceUpdateCommandHandler` (the crowdsourced price
endpoint) requires `IsOwnerOrEmployeeAsync` — meaning only store staff, not an ordinary consumer, can
currently submit a price update, even though `[Authorize]` on the controller allows any logged-in
role to call it. This contradicts CLAUDE.md §6's own description of the feature ("`SubmitPriceUpdateCommand`
— юзер обновляет цену"). This predates this session's work entirely and is a much larger, unrelated
change (redesigning who's allowed to submit a price) — flagging it here rather than touching it
unprompted.

---

## What this pass did not cover

A literal file-by-file read of the entire ~700-file backend + frontend tree. Prioritized by risk per
CLAUDE.md's own stated priorities (money, auth, data integrity) rather than exhaustive coverage —
areas *not* specifically re-examined this pass: the consumer-facing B2C screens (scan, shopping
list, reviews) beyond what the subscription-gating sweep touched, the notifications/push pipeline,
and the receipt-verification/crowdsourcing flow beyond the one observation above.
