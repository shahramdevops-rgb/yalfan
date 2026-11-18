# ADR 0002 — Money: Rial, whole numbers, one rounding rule

- Status: accepted
- Date: 2026-11-15 (1405/08/24)
- Written in: roadmap task 0.3. The ledger part (append-only entries, reversal, calculated
  balances) is extended in task 2.4.

## Context

Every later module moves money: contractor pay, payroll, purchases, cheques, sales. The way an
amount is represented cannot be changed cheaply once real entries exist, because every stored
number would have to be converted. So it is fixed before the first amount is stored.

In Iran people speak in Toman and the banks, cheques and official papers are in Rial
(1 Toman = 10 Rial). A system that mixes the two will sooner or later be wrong by a factor of ten.

## Decision

1. **The unit is Rial, everywhere.** Decided by the developer on 1405/08/24 ("Rial everywhere"):
   amounts are stored in Rial, the API sends and receives Rial, and the panels, the contractor
   app, the store, print-outs and SMS show Rial. There is no Toman anywhere in the system and no
   conversion between the two.
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

## Not decided here

- **Rounding a payslip or a settlement to a coarser step** (for example to the nearest 1,000
  Rial). That is a payroll and settlement rule, asked in Phases 2 and 5. Money only guarantees
  whole Rials.
- **Splitting an amount into shares** without losing a Rial (overhead per model, task 9.1). Added
  when it is needed.
- **The ledger rules** (no edit, no delete, reversal, calculated balances): task 2.4.

## Consequences

- The operator types and reads Rial. The panel must make the unit impossible to miss: every
  amount field and column is labelled «ریال» and large numbers are grouped in threes, because
  an amount typed in Toman by habit is wrong by a factor of ten and looks plausible.
- Roadmap wording that said "to the last Toman" now reads Rial (task 5.4).
- Reports never accumulate rounding error: totals are sums of integers.
- Adding a currency is one line in `Currency`, plus the rule for how its smallest unit is shown.
