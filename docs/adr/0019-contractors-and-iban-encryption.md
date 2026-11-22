# ADR 0019 — Contractors, and IBANs stored encrypted

- Status: accepted
- Date: 2026-11-23 (1405/09/02)
- Written in: roadmap task 1.3

## Context

About 40 outside contractors are paid by the piece. Work orders and their accounts (Phase 2)
will point at them, they will log into their own panel with their mobile (Phase 10), and the
payouts (Phase 15) will pay to their IBAN. Until Phase 15 the owner pays by hand.

The developer accepted these proposals on 1405/09/02 (roadmap Phase 1, "Answered … for 1.3"):

- A contractor does **one or more stages**, at least one, ticked from the shared list (ADR 0018).
- The profile is **full name, mobile, stages, optional IBAN, optional notes**. No national ID.
  A contractor with helpers of his own is one record: the person who is paid.
- The **mobile is required and unique** among contractors; **names may repeat**.
- **Every staff role** may enter or change the IBAN; screens show only its **last four digits**;
  only the **owner** can see the whole number.
- Only **Iranian** IBANs. **No delete**, only deactivate and reactivate.

## Decision

### The Parties module

`Contractor` lives in the Parties module (ARCHITECTURE.md 6), which will also hold workers
(1.4), customers and suppliers. Its stages are a separate table, `contractor_stages`
(contractor id, stage id), with a foreign key to `stages`. Why a table and not an array column:
rate cards and work orders point at stages through real foreign keys too, so the database refuses
a link to a stage that does not exist, and "which contractors sew" is an ordinary join.

- The name is normalized like catalog names (`PersianText.Normalize`), so a search finds it
  whichever keyboard typed it.
- The name column has PostgreSQL's ICU collation `fa-x-icu`, so the list is in Persian
  alphabetical order. Code-point order puts «پ چ ژ گ» after the Arabic letters, and a glibc
  collation depends on the server's locale data, which differs between images and changes with
  operating system upgrades. The collation is deterministic, so the unique index and substring
  search behave as usual.
- Whether giving a contractor a stage he has not ticked is refused or warned, and whether cutting
  may be ticked at all, is decided in 2.2, where a work order is issued.

### The IBAN

- **`Iban` is a value object in `Domain/Common`**, shared by contractors, workers (1.4) and the
  payees of Phase 15. It accepts the forms people type (with or without `IR`, spaces, dashes,
  Persian or Arabic digits), checks the ISO 13616 check digits, and keeps `IR` + 24 digits. Its
  `ToString()` is the masked form, so printing one by mistake in a log does not leak it.
- **Stored encrypted with AES-256-GCM** (`IbanCipher`), with a key from configuration
  (`Encryption:IbanKey`: user-secrets on the host, `IBAN_ENCRYPTION_KEY` in `.env` for compose,
  the server's environment later). Why: the key lives outside the database like the JWT key, so a
  database dump or a backup alone does not reveal an IBAN, and AES-GCM is in .NET itself, with no
  new package. Each write uses a random nonce, so the same IBAN is stored differently every time;
  the stored value starts with `v1:`, which names the key, so a key can be rotated later by adding
  `v2` without rewriting old rows in one go. ASP.NET Data Protection was not used: it is built for
  short-lived payloads with a key ring that expires and rotates on its own schedule, and its keys
  would need their own protection and backup; one fixed, explicit key is simpler to keep safe.
- **Applied to the whole model by type.** `AppDbContext` gives every `Iban` property of every
  entity the encrypting converter and the audit's "redacted" mark (`IbanMapping`), so an entity
  that gets an IBAN later cannot forget either; its configuration only names the property.
- **The API never returns the whole IBAN**, except on the owner-only request below. Every
  response carries `maskedIban` (`IR** **** **** **** **** **12 34`) or null.
- **Its own requests.** `PUT` and `DELETE /panel/parties/contractors/{id}/iban` set, replace and
  remove it; the profile `PUT` has no IBAN. Why: the profile form never receives the whole number,
  so it could not send it back, and saving a name must never clear an IBAN. Phase 15 puts
  verification and a holding period on exactly this request.
- **The owner reads the whole number** with `GET …/{id}/iban` (owner only). Each read writes a
  log line with the user and the contractor, because the audit records changes, not reads.
- **Audit:** an IBAN change is recorded as `{"iban": {"old": null, "new": null, "redacted":
  true}}` (ADR 0013). The ciphertext is never written to the audit either.

### The key

- The API refuses to start without a key that is exactly 32 bytes in base64. Design-time tools
  (`dotnet ef`) build the model without it, because the key is checked only when it is used.
- **Losing the key loses every stored IBAN.** It is kept outside the server too, apart from the
  database backups (ARCHITECTURE.md 12). A backup and its key must not be stored together, or the
  encryption protects nothing.
- One process uses one key: the cipher lives inside the EF model, which EF Core may build once and
  reuse. The integration tests therefore share one key for the whole run.

### The panel API

`/panel/parties/contractors`, for every staff role: list and search (one box for part of the name
or part of the mobile, Persian digits and `+98` accepted; `isActive` filter; pages), read, add,
save the profile, deactivate, activate, and the IBAN requests above. Error codes:
`parties.full_name_required`, `parties.full_name_too_long`, `parties.notes_too_long`,
`parties.stage_required`, `parties.stage_not_found`, `parties.mobile_taken`,
`parties.contractor_not_found`, `parties.iban_not_set`, `iban.invalid`.

## Consequences

- Workers (1.4) reuse `Iban`, `IbanRules`, the encryption and the redaction with no new code: an
  `Iban` property on `Worker` is enough.
- The audit records a link row (a row that is only its key, such as a contractor's stage) with its
  key columns as fields, so adding and removing a stage is readable (ADR 0013, updated).
- Searching IBANs by value is impossible by design (random nonce). If Phase 15 needs "is this IBAN
  already someone else's", it adds a keyed hash (HMAC) column next to the ciphertext.
- Phase 10 links the contractor's login to this record through the unique mobile.
