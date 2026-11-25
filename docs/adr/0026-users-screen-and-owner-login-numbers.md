# ADR 0026 — The users screen and the owner's login numbers

- Status: accepted
- Date: 2026-11-26 (1405/09/05)
- Written in: roadmap task 1.6.5 (the fifth part of 1.6)

## Context

ADR 0012 left the users screen to 1.6: the owner lists users, deactivates them and changes the
number of someone who lost a phone. Before building it the developer was asked five questions.
He answered one of them with something new: the owner must not be locked out, or left exposed, for
a day when his phone is stolen. In his words: give the owner more than one number, so that if his
phone is stolen he calls the developer, who switches the stolen number off at once, and the owner
keeps working with his other phone.

Three problems with putting the developer's own number on the owner's account were shown to him:
the audit would record the developer's changes as the owner's; a thief logged in with the stolen
phone is "the owner" and could switch the other numbers off, the developer's included, before
anyone called; and if numbers could be added in the panel, the thief would add his own. He chose
the proposal below on 1405/09/05. He also chose ten minutes for the access token (it was fifteen)
and refused a database check on every request, to keep that load off the store that will be
connected later.

## Decision

### Login numbers

- **A user logs in with one or more numbers** (`user_login_numbers`: user, number, active). A
  number belongs to one user only, switched on or off; the unique index covers all of them, so a
  deactivated user's number is still taken. A manager or an operator always has exactly one.
- **An owner account may have several**, set only in the server's configuration. The workshop
  owner has two numbers on two separate phones (asked on 1405/09/05: two phones, not one phone
  with two SIMs, because one stolen phone would take both).
- **The developer has an owner account of his own**, with one number and his own name, so his
  changes are recorded as his. Whether this account may approve payouts is decided in Phase 15.
- **A login code belongs to the number it was sent to.** The code's hash includes the number, the
  login looks only at the newest code of that number, and the two-minute wait and the five codes
  an hour count per number, as ADR 0012 first said. Why: with codes per user, a thief asking for
  codes on the stolen phone would block the owner's other phone, and a code taken before the
  number was switched off could be typed with the other number.

### Owner accounts come from the configuration, and only add

- `Identity:Owners:{i}:FullName` and `Identity:Owners:{i}:MobileNumbers:{j}` replace
  `Identity:SeedOwner`. The old setting stops the start with the name of the new one, because it
  would otherwise be ignored and a new database would have no owner.
- At every start, an account none of whose numbers exists is created; numbers missing from an
  existing account are added. **Nothing is renamed, removed or switched back on**, because a
  restart must not undo a stolen phone's number being switched off. A number held by a user who is
  not an owner, numbers of one account held by two users, the same number under two accounts
  (they would be joined without a word, and the developer's changes recorded as the owner's), or
  a number that is not an Iranian mobile stop the start. Blank entries are ignored, so
  `docker compose` can pass empty variables.
- **A number is changed next to the old one, never over it.** An account is recognised by its
  numbers, so a number typed over the old one makes a new account and leaves the old one able to
  log in, with no way to switch it off from the panel. The way: the new number goes in the spare
  slot of the same account (`OWNER_MOBILE_3`, `DEVELOPER_MOBILE_2`), a restart, a login with it,
  the old one switched off in «کاربران». The same steps recover from the developer's phone being
  stolen, whose single number nobody can switch off from the panel. An owner account none of
  whose numbers is configured any more is logged as a warning at start, not acted on, because
  acting on it could lock the owner out after a mistyped setting. (Found by the review of 1.6.5.)
- **The panel adds no number to an owner account and changes no owner account's role or active
  flag** (`identity.owner_locked`). A second owner stays a decision outside the panel (ADR 0012).
- Real numbers live only in the server's `.env` (`OWNER_MOBILE`, `OWNER_MOBILE_2`,
  `DEVELOPER_MOBILE`) and in user-secrets, never in the repository, which is on GitHub.

### Switching a number off

- **Each number of an owner account has its own switch**, for every owner-role user.
- **An account always keeps one number switched on** (`identity.last_login_number`). This one rule
  replaces a special lock: the developer's account has a single number, so nobody, a thief
  included, can switch it off from the panel. If a thief has switched off the owner's other
  number, the developer switches it back on and then switches the stolen one off. The rule is
  also a constraint trigger in the database, checked at commit with a lock on the user's row:
  two requests switching off an account's two numbers at once would each see the other still on.
- **A number of one's own account is switched back on only by another owner account**
  (`identity.own_number_switch_on`). Why: the stolen phone's open page keeps its access token for
  up to ten minutes after its number is switched off; were an account allowed to switch its own
  numbers on, the thief would switch the stolen number on and ask for a new code. So the owner who
  switches a number off by mistake, or gets his phone back, asks the developer, as in the
  developer's own picture of a theft. (Found by the review of 1.6.5.)
- **Switching a number off ends every session of that account, on every device**, not only those
  opened with that number, because nobody knows which number's code the stolen phone used. The
  owner logs in again with his other number. When the owner switches off a number of his own
  account, his page goes to the login at once.
- A switched-off number gets the same answer as an unknown one and no SMS.

### What ends a session, and when

- Changing a manager's or operator's number, deactivating a user, and switching a number off
  revoke every refresh token of that user in the same save, so no device can renew.
- **The access token lives ten minutes** (was fifteen): an open page on a stolen phone works at
  most that long after its number is switched off. Nothing is checked in the database on each
  request; the developer accepted the ten minutes to keep that load off the future store.
- A changed role does not end the user's sessions; it is in his next token, within ten minutes.

### The users screen

- Answers of five questions (1405/09/05): the owner's own number is not changed in the panel (it
  became the configuration above); a user's name and role can be corrected, the role only between
  manager and operator, because otherwise a role chosen by mistake could never be fixed, the
  number staying taken by the user himself; ten minutes is accepted; only the owner manages users,
  the manager adds no operators (ADR 0012 had left this open); the change history waits for 1.6.6.
- API, owner only: `GET /panel/users` (owner accounts first, then Persian alphabetical order; each account's
  numbers in number order, because ids made in the same millisecond have no order),
  `PUT …/{id}/name`, `PUT …/{id}/role`, `PUT …/{id}/mobile`, `POST …/{id}/deactivate|activate`,
  `POST …/{id}/numbers/{numberId}/deactivate|activate`; every change answers the whole user.
  `POST /panel/users` answers the whole user too. `/panel/auth/me` and the session no longer carry
  a mobile number: an owner account has several and the panel needs none.
- Panel: «کاربران» (`/users`), in the sidebar for an owner only; anyone else gets «not found», and
  the API refuses them as well. Every user a card with no save button: the name edited where it
  stands, the role as two chips, a manager's or operator's number edited where it stands with a
  line saying he will be logged out everywhere, a switch for active; an owner account shows
  «مالک», a note that it comes from the configuration, and a switch per number, the last one on,
  and on one's own account a number that is off, locked with a line saying why. The new-user form starts at «اپراتور دفتر», the smaller power.

## Consequences

- **After a phone theft, also look at the users screen.** Until its number is switched off, and
  for up to ten minutes after on a page already open, a thief logged in as the owner can change a
  manager's or operator's number to his own and keep an account after the owner's is safe, or
  switch off the owner's other number while the stolen one is still on. The changes are audited,
  and the change history (1.6.6) will show them.
- The users table has its Persian collation now, like contractors (ADR 0019).
- The migration copies each user's number into the new table in SQL, so it writes no audit entry;
  each number is already in the audit of its user's creation, under `users`. Old login codes are
  dropped (they live two minutes).
- The developer must replace `Identity:SeedOwner` in his user-secrets and `.env` with the new
  settings before the API starts again.
- Changing the owner's numbers means editing the server's configuration and restarting; removing
  one is switching it off in the panel.

## Not decided here

- Whether the developer's account may approve payouts (Phase 15).
- Confirming a new number by a code sent to it: not needed while only the owner types numbers, and
  testable only with the real SMS provider (task 3.0).
