# V0.2.2 Quiet Focus

Operate mode, established WPF resources. Preserve warm paper/sage and paired dark tokens, installed Chinese UI fonts and native controls. Create three explicit statistics destinations beneath a shared range/subject toolbar. Overview uses unequal primary/secondary columns at 1200×800, stacking at 900×600. Keep review text and source facts separate.

Redesign scan found one long statistics page, equal-weight secondary sections, millisecond hover details, off-center donut text and outline hover feedback. Adopt editorial hierarchy/asymmetric split from Taste skills, native state/empty affordances from Redesign, integer text and geometric alignment from Make. Reject Tailwind/React, double bezels, giant marketing whitespace, font downloads, page entrances and hover scale: user-pinned WPF Quiet Focus is binding.

Display rule: sum original milliseconds first; floor seconds only for text, never promote 1ms to 1 second. Long values retain seconds. Timer shows total hours and minutes, hours do not wrap. Raw backup/audit and CSV preserve facts. Routine pages hide technical zone IDs; settings and exception evidence retain them.

Average KPI semantics: elapsed natural-day average uses only the elapsed portion of the range; learning-day average uses days with nonzero confirmed effective time. Both show their denominator. If a denominator is zero, the UI displays an em dash and states that the average is not computed. A genuinely zero average over existing natural days still displays 0 seconds. Tiny nonzero days remain learning days even when their display floors to 0 seconds.

Period reviews are independent authored records with range, optional subject, body, timestamps and version. Current weaknesses/tasks are snapshots; dated daily reflections are supporting material. No invented efficiency or historical task completion metric.

## Native audit and Make full review

Framework: WPF/XAML ResourceDictionary, custom DrawingContext charts. Scope: three statistics views, affected today/calendar/detail/weakness/editor paths, light/dark, 900×600 and 1200×800. One context; no browser motion tools or isolated-agent review are claimed.

| Category | Evidence inspected | Result |
|---|---|---|
| Typography | NaturalDuration, converter, DurationText, native KPI/detail/editor captures | Integer seconds, tabular digits, long numeral/unit wrapping |
| Surfaces | Styles/ThemeService, actual light/dark detail and statistic windows | Established paired tokens, restrained surfaces |
| Animations | StudyChart events and native hover/focus | No custom animation; 10% motion replay not applicable |
| Icons | FocusIcon and native navigation/controls | Existing single vector family preserved |
| Performance | 400-row virtualization, bounded startup/environment results | Local sample only; no all-device certification |

| Severity | Location | Before | After | Why |
|---|---|---|---|---|
| HIGH | StudyAnalytics.cs DurationText; NaturalDuration.cs; StudyChart.cs | Fractional seconds/ms and hour compact omission | Floor after raw aggregation, preserve meaningful seconds and canonical precision | Numeric meaning and stable typography |
| HIGH | MainViewModel.cs MarkUncertainAsync; MainViewModel.Presentation.cs DayStatus | Saved uncertain state could leave stale heatmap; rest lost beside study | Refresh after durable write; current pending prefix and independent rest label | Static state must be truthful |
| MEDIUM | MainWindow.xaml statistics; MainWindow.Presentation.cs | Single long page and insufficient small-window stacking | Three shared-scope destinations, unequal overview columns, 900 vertical scroll | Hierarchy and native adaptation |
| MEDIUM | StudyChart.cs donut/text/keyboard | Fixed-left center, ring-shaped selection outline, long axis ellipsis | True text-height center, planned unit line breaks, integer-second axis, separate square focus cue | Optical/geometric alignment and motion restraint |
| MEDIUM | EditorDialog.cs BindChapters; MainViewModel.cs LoadChapters | Empty selector lacked guidance; chapter list did not depend on dialog subject | Clear sentinel, management link, eligible dependent choices and existing archive preservation | Empty state and native affordance |

Considered but rejected: stronger shadows/nested shells (established flat Quiet Focus hierarchy); hover scale/outer halo (explicit request and repeated attention cost); downloaded display font/page entrance (native task surface and installed Chinese font stability). No new styling system or motion library.

Verification: actual Core/TRX, complete native business suite, owned HWND hover/popup capture, bounded batched visual inspection and final confirmation. Final screenshot/packaging verdict is recorded with the frozen native evidence. Unverified: physical IME/Narrator, other physical DPI, actual power/lock changes, clean offline machine. These are not replaced by browser/bitmap tests.
