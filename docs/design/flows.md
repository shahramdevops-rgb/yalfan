# The three daily flows of Phase 2: prototypes and the owner's choice

Task 2.0 (docs/ROADMAP.md). Before any Phase 2 screen is built, the three daily flows are drawn as
clickable prototypes in two or three variants each and shown to the owner (and to the operator
once there is one), because changing a drawing takes minutes and changing a built screen takes
hours. Built 1405/09/07; **waiting for the owner's choice.**

## The file

`docs/design/prototypes/flows.html`: one self-contained page with all three flows and every
variant.
- **One file, no network request** (the font is embedded): it is sent to the owner's phone in a
  messenger and opens offline, with no server.
- **Outside the panel's code**, because the roadmap says "before any code", and a mock-up inside
  the panel would need tests, screenshots and a review for something that is thrown away.
- **The panel's own look**: the colour roles and values of `web/panel/src/index.css`, the sizes of
  `docs/design/UI.md`, Vazirmatn, right to left, every amount in Rial with its Toman words under
  it. The owner then judges the flow, not a different look.
- **Shared made-up data**: a slip issued in the first flow is received in the second and credited
  in the third, so a whole day of the workshop can be shown. Nothing is saved; «از نو» starts
  over. Every name and number is made up.
- The dark bar at the top belongs to the prototype, not to the panel: it switches the flow, the
  variant, the phone frame («نمای گوشی» on a desktop), and in flow 2c whose eyes the page is seen
  through; in flow 3 it switches the contractor. Its «سؤال‌هایی که این صفحه از صاحب کار
  می‌پرسد» lists the questions below for the flow on screen.

To change it: edit the HTML. The font block at the end is one long line; do not read the whole
file into a session.

## The variants

Each variant differs from the others in one real decision, not in looks.

### 1. Issuing work (the operator, at the desk)

| Variant | How | Good for |
|---|---|---|
| a | From the production order's page: each stage says how many are given out and how many are left; «دادن دوخت» opens a small form under that stage, the counts already filled with what is left | Work given out order by order |
| b | One «کار تازه» form typed from the paper slip: who, which order, which stage, how many of each bag, when back. Enter moves from field to field | The operator copying from paper with the numpad |
| c | Step by step on a phone: who, which job, how many (large + and −), check and issue | The owner or the manager handing out work in the workshop |

In all three: the rate copied from the rate card is shown with the pay if every piece comes back
right; a contractor with pieces out for rework is flagged; a stage with no rate refuses the slip;
an in-house worker is in the list with no rate; moving work to someone else is one step
(«انتقال به دیگری» on a slip: the unfinished pieces come back unpaid and a new slip goes out).

### 2. Receiving and checking (the checker, on a phone)

| Variant | How | Good for |
|---|---|---|
| a | Three counters per bag: good, back for rework, rejected, with + and − or a typed number | Counting carefully, piece by piece |
| b | "All good unless said": the number brought is filled in; one large «همه سالم است»; only if something is wrong, «چندتایش ایراد دارد» opens rework and rejected | The usual case in the fewest taps |
| c | Two steps: the office writes how many came back; the checker's phone lists what waits for a check and marks it. Nothing is credited before the check | Bringing back and checking at different times or by different people |

In all three: the counts can never pass what is still with the contractor; pieces back from
rework are received the same way; the bar at the bottom says what will be saved and what it puts
into the contractor's account. The checker's login sees only this page.

### 3. A contractor's account and settlement (the owner)

| Variant | How |
|---|---|
| a | Like the paper notebook: every row with its running balance, the balance on top, «ثبت پرداخت» and «تسویه» beside it |
| b | Summary first: earned, paid and the balance on top; the unsettled slips ticked below with «تسویه‌ی ۲ برگه»; the full ledger folded at the bottom |

In both: a payment, an advance or a deduction (with its reason); the summary dialog before money
moves («به … مبلغ … ریال بابت …، به شبای …»); the warning while pieces are out for rework, with
«پرداخت در هر حال»; no payment for a contractor with no IBAN; an advance larger than the work
shown as his debt; a row reversed by a second row, both kept; a slip with pieces still out
cannot be settled. Try «حسین نوری» in the top bar for the no-IBAN case and the debt.

## Taken as proposals (the developer did not answer, 1405/09/07)

- **One slip carries several bags of one model**, a row per bag (fabric × colour × size), because
  one handover is one paper slip with one number. If the owner says each colour is its own
  slip, receiving becomes simpler (one set of counts per slip).
- **No reviewer run for 2.0**, because nothing in it is product code; the prototype was checked
  against `docs/design/UI.md` and the money rules by hand and clicked through in Chrome.

## Questions for the owner

Ask them while showing the page; they are also in its top bar.

Issuing:
1. Does one slip carry several colours of a model (as drawn), or does each colour get its own?
2. How many days does work usually take to come back? The «۳ روز، یک هفته، دو هفته» buttons are
   only an example.
3. A model with no rate for a stage (the city backpack for packing): refuse the slip (as drawn),
   or type the rate there? (ADR 0021 leaves this to 2.2.)
4. Moving work to another contractor: one step (as drawn), or two (take back, then issue)?
5. Should an in-house worker also get slips, with no rate, so that progress shows all sewing?
   (The open question of Phase 2.)

Receiving:
1. Is the work checked the moment it arrives (a, b), or received at the office and checked later
   by someone else (c)?
2. Who checks? If neither the manager nor the operator, his login sees only this page.
3. A piece that cannot be fixed is simply not paid (as drawn); is anything also deducted?
4. Must each colour be counted on its own (as drawn), or is the slip's total enough?

Account:
1. Which variant is closer to the paper notebook?
2. How often is a contractor settled: all finished slips at once, or slip by slip?
3. Is the bank's reference required on every payment?

## Found while building

- **The Toman words under every amount** (ADR 0024) make a long statement on a phone about twice
  as tall: each ledger row has two amounts, each with its words. Worth showing the owner on the
  phone before 2.10; the rule itself is not changed here.
- **Counting per colour** makes receiving a block per bag; with three colours on a slip, variant
  2a is long on a phone. This is tied to issuing question 1.
- **A slip cannot be settled while any of its pieces are out**, rework included, because a settled
  slip cannot change and the repaired pieces would still have to be paid on it. So a contractor
  with one piece out for rework waits for that whole slip.

## The choice

To be filled once the owner has looked: for each flow, the chosen variant and why, and the
answers to the questions above, each moved to where it belongs (ROADMAP, ADR, ARCHITECTURE). Then
the rejected variants are deleted from `flows.html` (git keeps them), so that 2.1, 2.9 and 2.10
see only one.

| Flow | Chosen | Why |
|---|---|---|
| Issuing work | | |
| Receiving and checking | | |
| Account and settlement | | |
