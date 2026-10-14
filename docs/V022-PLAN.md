# V0.2.2 implementation plan

Execution: superpowers brainstorming/refine → writing-plans → executing-plans/TDD → verification. Native inline execution is already authorized; ordinary review gates do not pause this task.

Goal: deliver a runnable offline WPF release with honest statistics, integer-second presentation and persistent period reviews.

Facts: baseline d9a50ed, clean; .NET 10/WPF/SQLite schema 1; canonical StudyPart values are milliseconds. StudyAnalytics and MainViewModel.Presentation already retain dates and subjects for drill-down. Settings stores JSON strings and shares the existing revision transaction and full backup. Tasks have no completion date; weakness history is not an analytics time series.

Decisions: preserve Quiet Focus tokens and native controls; overview primary stacked chart/secondary flat donut; separate subject/chapter and review views with shared range. Reviews use a reserved `period-review:v1:` namespace with independently validated entities, version checks, soft deletion and recovery. Do not migrate schema. Integer seconds floor only at presentation, after aggregation; timer total HH:mm. CSV remains the original all-record raw export; range-specific display CSV is not promised.

Assumptions: existing ten-year limit remains; long chart ranges aggregate by week/month with explicit bucket range. Heatmap scrolls horizontally with all dates reachable by keys, no clipping of source points. Review materials show current weakness/task snapshots and genuine daily reflections without claiming historical completion or efficiency.

## Tasks
- [x] 1. Duration boundary: update DisplayDurationTests/AnalyticsTests; observe red, implement DurationText and native presentation; retain 1ms facts and 600+600ms aggregate test.
- [x] 2. Period reviews: add Core entity and Data API; tests for validation, optimistic version, atomic revision, restart, delete/restore, backup and corrupt namespace rejection. Use existing settings/revisions transaction.
- [x] 3. Statistics and chapters: MainViewModel.Presentation and MainWindow XAML; native assertions for subject/date/chapter drill, selector empty state and dependent refresh, pending/rest semantics, long range aggregation, three views and review CRUD.
- [ ] 4. Charts/polish: StudyChart mathematical center, no hover outline, rectangular keyboard cue, integer axes and tooltips; preserve nonzero geometry. Single bounded native capture inspection across themes/sizes/states, then one correction/confirmation pass.
- [ ] 5. Verification/delivery: run new Core TRX and native business suite; version 0.2.2; commit final source, publish self-contained win-x64, launch final EXE, capture fresh native batch (all images carry metadata), freeze provenance and manifests. Build source/runtime/review/test ZIPs and independently re-read every entry size/SHA. Preserve old releases.

Review focus: 1ms/huge values; archive reference preservation; cross-day effective parts only; pending prefix and independent rest label; long ranges and keyboard/date semantics. Every class receives regression assertions, plus existing timer/record/backup tests.

Limits: physical IME/Narrator, unmodified DPI classes, real power/lock/clock changes and clean offline machine are NOT TESTED. Only owned synthetic data directories, never the real user database. No public push or system changes.

