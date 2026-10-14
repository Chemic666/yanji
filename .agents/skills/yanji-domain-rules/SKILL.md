---
name: yanji-domain-rules
description: Preserve YanJi study-time, record revision, progress, weak-point and backup invariants when changing its Core, Data or WPF workflows. Use for YanJi implementation and consistency review; not for unrelated .NET projects.
---

# YanJi domain rules

Read owning source and approved scope before changes; verify these schema-1 anchors as code evolves. User scope wins.

- Core/Models.cs, TimerMachine.cs, TimeParts.cs: manual and timer share StudyRecord/StudyPart. One active timer. Only Running contributes. Awake/boot/wall/process discontinuities require review; lock alone does not imply stopping. DurationOnly never fabricates timestamps. Intervals split at actual local midnight. Sum raw milliseconds before formatting. Unknown gaps never receive default credit.
- Data/YanJiDatabase.cs: sessions/parts are statistic facts. SaveRecord and SaveTimer commit facts, audit and checkpoints in transactions with version checks. Known intervals cannot overlap. Preserve deduplication and durable-snapshot fallback on write failure. No second mutable total store.
- Whole-record deletion is reversible soft deletion. ExcludePart removes an actual part with revision evidence; RestorePart revalidates overlaps. Final part exclusion is refused: use whole-record delete. Preserve IDs and cross-date impact, not an adjusted total pretending to remove a part.
- Chapter Progress and Mastery are independent. Referenced subjects/chapters archive. Task Done and weak point Open/Reviewing/Verified are explicit facts. Finishing a timer never automatically completes a task or masters a point.
- Stats derive from effective records and correct dates/filters. Rest markers do not erase study. Pending, rest and no-record differ. Preserve natural-day/study-day denominators and current-snapshot/period-history distinction. Existing tasks have no completion timestamp.
- Full backup replacement, not merge: keep WAL-safe snapshot, integrity/identity/schema/semantic validation, rollback copy and active-timer restore guard. No raw open-WAL file copy. Migrations require backup before user-data mutation and restorable isolated validation.
- docs/V03-PLAN.md owns focus semantics: modes/goals/task links are metadata, breaks never study. Metadata and active snapshot must match the same committed session. No V0.4+ or account/cloud scope in V0.3.

Use synthetic artifacts directories, never the real LocalAppData/YanJi database. Select checks for changed invariants; no old full UI matrix without a new concern. Real power changes/restarts need human authorization. Distinguish simulation, real HWND, real input and NOT TESTED. Retain failed and interrupted evidence.

For review report affected invariants, owning transaction/state path, minimum meaningful checks and uncertainty. Do not introduce a new timer, database or command framework to follow this skill. Source anchors are relative to src/ unless docs is specified.
