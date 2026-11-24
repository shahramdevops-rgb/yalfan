# ADR 0002 — Money: Rial, whole numbers, one rounding rule

- Status: accepted
- Date: 2026-11-15 (1405/08/24)
- Written in: roadmap task 0.3. The ledger part (append-only entries, reversal, calculated
  balances) is extended in task 2.4.
- Amended: 2026-11-23 (1405/09/02), before task 1.5: every amount shown carries a helper line
  in Toman and in words (point 9). Storage, the API and typed amounts stay Rial.
- Amended: 2026-11-25 (1405/09/04), in task 1.6.3: the helper line keeps only the words in
  Toman; the Toman figure is dropped (point 9, ADR 0024).

## Context

Every later module moves money: contractor pay, payroll, purchases, cheques, sales. The way an
amount is represented cannot be changed cheaply once real entries exist, because every stored
number would have to be converted. So it is fixed before the first amount is stored.

In Iran people speak in Toman and the banks, cheques and official papers are in Rial
(1 Toman = 10 Rial). A system that mixes the two will sooner or later be wrong by a factor of ten.

## Decision

1. **The unit is Rial, everywhere.** Decided by the developer on 1405/08/24 ("Rial everywhere"):
   amounts are stored in Rial, the API sends and receives Rial, amounts are typed in Rial, and
   the panels, the contractor app, the store, print-outs and SMS show the amount in Rial. Toman
   exists only as the display-only helper line of point 9, in words: it is never stored, sent by
   the API or typed.
   Why: one unit means no conversion step that can be forgotten, and it is the unit of the
   bank transfers, cheques and payout files the system must match.

2. **An amount is a whole number of the currency's smallest unit**, held as a 64-bit integer
   (`long`): Rial for IRR, cents for USD. Never `float`, `double`, or a fraction.
   Why: floating point cannot hold most decimal fractions exactly (0.1 + 0.2 is not 0.3), and
   a ledger must add up to the last Rial. A 64-bit integer holds about 9.2 × 10^18 Rial, far
   beyond any total the workshop can reach.

3. **Money is an amount plus a currency code** (`Money(Amount, Currency)`), never a bare
   number. Only currencies listed in `Currency` exist: IRR, and USD for proformas to foreign
   buyers. Which currencies the price lists really use is decided in Phase 8.
   Why: a bare number cannot say whether it is Rial or dollars, and a closed list means a typo,
   or the Toman pseudo-code "IRT", is refused instead of stored.

4. **Mixing currencies in arithmetic throws.** It is a bug in the calling code, not something a
   user can cause, so it is an exception and not a `Result`. A use case that receives a currency
   from outside validates it first (`Currency.FromCode` returns `currency.unknown`).

5. **One rounding rule: a half rounds away from zero** (166.5 → 167, −166.5 → −167). Rounding
   happens only when an amount is multiplied by a number with a fraction (2.5 metres, a 1.4
   overtime factor). Adding, subtracting and multiplying by a whole count are always exact.
   Why this rule: it is what a person does on paper, so hand-checked figures match; and the
   reversal of an amount is the exact mirror of the amount, so a reversing entry always cancels
   the original.

6. **Overflow throws** instead of wrapping around silently.

7. **A negative amount is valid.** Debts, reversing entries and balances need it. Whether a
   particular field may be negative is that document's rule, not Money's.

8. **In the database** an amount is a `bigint` column and its currency a 3-letter code column.
   The mapping is added with the first entity that holds money.

9. **Every Rial amount shown also says the amount in words, in Toman**, on a smaller, quieter
   line under it (added by the developer on 1405/09/02: "wherever an amount is written, write it
   in Toman and in words under it, so the person is not confused"; the Toman figure was dropped
   from the line on 1405/09/04, see below):

   ```
   ۱۲۰٬۰۰۰ ریال
   دوازده هزار تومان
   ```

   - The Rial figure stays first and is the only figure; the line under it is a reading aid.
   - The words are the Toman amount, because that is how people say amounts aloud.
   - **Only the words, no Toman figure** (changed by the developer on 1405/09/04, when he saw
     the first screen: «اون رو پاک کن و همون حروف به تومن بمونه»). Why: a second figure next to
     the Rial one gave the eye two numbers to choose from; the words still make a factor-of-ten
     mistake visible. A Rial amount that is not a multiple of ten is not rounded: 12,345 Rial
     reads «هزار و دویست و سی و چهار تومان و پنج ریال» (ADR 0024).
   - Under a field where an amount is typed, the line follows the typing, because a Toman
     typed by habit into a Rial field is the usual mistake and must show at that moment.
   - It applies to screens and print-outs: the panel, the contractor app and the Persian store.
     **SMS is decided in 10.5**, because every extra character can split a message into
     two paid parts. A dollar amount has no Toman line.
   - Why this changes point 1 ("no Toman anywhere"): the operator and the owner think in Toman,
     so a Rial figure alone is read one way and meant another; the Toman words under it turn a
     factor-of-ten mistake into something visible. Storage, the API and typed input stay Rial,
     so the data still has one unit.
   - Built in the panel with the first screen that shows an amount (task 1.6.3, ADR 0024).

## Not decided here

- **Rounding a payslip or a settlement to a coarser step** (for example to the nearest 1,000
  Rial). That is a payroll and settlement rule, asked in Phases 2 and 5. Money only guarantees
  whole Rials.
- **Splitting an amount into shares** without losing a Rial (overhead per model, task 9.1). Added
  when it is needed.
- **The ledger rules** (no edit, no delete, reversal, calculated balances): task 2.4.

## Consequences

- The operator types and reads Rial. The panel must make the unit impossible to miss: every
  amount field and column is labelled «ریال», large numbers are grouped in threes, and the
  Toman words of point 9 sit under every amount, because an amount typed in Toman by
  habit is wrong by a factor of ten and looks plausible.
- Roadmap wording that said "to the last Toman" now reads Rial (task 5.4).
- Reports never accumulate rounding error: totals are sums of integers.
- Adding a currency is one line in `Currency`, plus the rule for how its smallest unit is shown.
