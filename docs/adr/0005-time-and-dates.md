# ADR 0005 — Gregorian UTC storage, localized display, one clock

- Status: accepted
- Date: 2026-11-15 (1405/08/24)
- Written in: roadmap task 0.3

## Context

The workshop thinks in Jalali dates and Tehran time. The store also serves Arabic and English
visitors, who expect Hijri or Gregorian dates. The server may one day run outside Iran. If any
of these leaks into stored data, dates become ambiguous and reports across a year boundary or a
server move go wrong. Attendance and payroll depend on "which day was it" being unambiguous.

## Decision

1. **Stored values are Gregorian and UTC only.**
   - A moment in time (a punch, when a payment was recorded, an audit time) is a
     `DateTimeOffset` in UTC in code and `timestamp with time zone` in PostgreSQL.
   - A calendar day with no time (a due date, an attendance day, the date printed on a work
     order) is a `DateOnly` in code and `date` in PostgreSQL.
   Why two types: a due date is not a moment. Storing it as midnight of some time zone is how
   dates shift by one day when the server or the viewer is elsewhere.

2. **Jalali, Hijri and Gregorian are display choices.** Conversion happens in the frontends.
   The API sends and receives ISO 8601 only. Why: the calendar is a property of the reader, not
   of the data; the same order is shown in Jalali in the panel and in Gregorian to an English
   visitor.

3. **`IClock` is the only source of the current time.**
   - `UtcNow`: the current instant; this is what gets stored.
   - `Today`: the workshop's calendar day in `Asia/Tehran`; use it for anything a person would
     call "today".
   Why a clock interface: tests must be able to set the time (a punch after midnight, a cheque
   due tomorrow, a rate that starts next month), and "today" must mean the workshop's day, not
   the server's.

4. **`DateTime.Now`, `DateTime.UtcNow`, `DateTime.Today`, `DateTimeOffset.Now` and
   `DateTimeOffset.UtcNow` are build errors** (`BannedSymbols.txt`, checked by an analyzer in
   every project). Why: a written rule is forgotten at 2 a.m.; a build error is not.

5. **The Domain never asks for the time.** A use case reads the clock and passes the time into
   the Domain method. Why: the Domain stays free of time zones and its tests need no clock.

6. **The time zone is the named zone `Asia/Tehran`, not a fixed +03:30.** Iran dropped daylight
   saving in 2022, but it has changed the rule before; the named zone follows the operating
   system's time zone data, a fixed offset would not. Checked on 1405/08/24 on Windows and in
   the Linux runtime image: 20:29:59 UTC is still the same Tehran day and 20:30:00 UTC is the
   next one, in both winter and summer.

## Consequences

- A day boundary for attendance, payroll periods and due dates is always the Tehran day,
  wherever the server runs.
- Reports that group by day must convert the instant to the Tehran day first; grouping a UTC
  timestamp by its UTC date would put the hours between 00:00 and 03:30 Tehran time on the
  previous day.
- The servers' time zone data must be kept up to date with the operating system.
- `SystemClock`'s day boundary is tested since task 0.7
  (`tests/Yalfan.Api.IntegrationTests/Time/SystemClockTests.cs`): winter, summer and a past
  daylight-saving summer, which a fixed +03:30 offset fails. CI runs it on Linux in UTC and the
  developer on Windows in Tehran time, so a clock that leaned on the machine's zone would fail
  in one of the two.
