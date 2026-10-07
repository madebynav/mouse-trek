# Measurement notes — 0.3.2

## Counts and saved data

Mouse travel sums Euclidean distances between observed cursor positions in pixels.
The first position after startup/reset is a baseline. Injected mouse events are
excluded and update/reset the baseline. Fresh physical key-down transitions count
once until release, with per-device and extended scan-code held-state suppression.
Click-down events count left, right, middle and extra buttons; releases do not count.

Today uses local calendar dates. Last hour uses one-second buckets strictly newer
than now minus 3600 seconds. Daily history retains 366 active dates; lifetime totals
remain independent. Atomic saves run every ten seconds and on orderly quit/session
transitions. A previous-save backup supports recovery. Unreadable saves are protected.
The save format stays version 1, with compatible defaults for older preferences.

## Screen equivalents

km = pixels × 0.0254 / validPPI / 1000. Speed and acceleration use the same conversion
without dividing by 1000. The default scale is 96 PPI. One scale applies to all saved
history and monitors; changing it does not change canonical pixels. This describes
cursor travel on screen, not physical mouse or trackpad travel.

## Measurement latency

The native input thread samples accumulated path distance and displacement around
every 100 ms while moving or settling. Kinematics uses monotonic elapsed time and a
120 ms exponential filter. Path speed uses travel per elapsed time; velocity uses
displacement per elapsed time. Acceleration is the filtered magnitude of a velocity
difference per elapsed time, including turns and braking. The verified formulas
are preserved. Long gaps and resets discard stale motion; stationary values settle
to exact zero. Peaks use maxima and do not inflate input counts.

## Display latency and resources

The earlier 250 ms display poll could add delay after measurement. Measured samples
now post a bounded UI wake-up while the dashboard is visible. The display requests
30 ms ticks during motion or settling; actual cadence depends on Windows scheduling.
Display interpolation uses monotonic elapsed time, with a 35 ms attack and 75 ms
decay. It is independent of input event frequency. Numeral and needle share the
same interpolated value; the numeric rate is not capped by the dial range.

Idle timer ticks return to 250 ms, with quiet paints at most once per second.
Hidden/paused/minimized states suppress visual animation. Historical aggregates are
not scanned per paint: a current snapshot is read at most about 10 times per second
while active, and graph history is snapshotted only on entry.

Ticks and dial arc geometry are precomputed. Changing arcs use bounded sampled
polylines, retaining the small-angle GDI+ crash fix. Temporary pens/brushes are
disposed and scaled control fonts are reused. Source includes an opt-in isolated
resource fixture; results are observations, not a memory ceiling.

## Graph

The chart holds exactly 30 consecutive local dates, zero-filled from existing daily
buckets. Today is partial and updates at most once per second. Date rollover shifts
the bounded window, retaining yesterday and dropping expired points.

Daily values are bucket totals. Cumulative values start at zero before the first
displayed date and sum only that 30-day window. Each series uses its own window
maximum for a shared labelled 0–100% relative axis. Zero maxima map to zero safely.
Straight segments and rounded joins preserve sample values without overshoot.
Distinct colours, line styles and markers identify the series. Exact selected
counts/equivalents and the pixel amount appear in a fixed strip.

## Milestones and privacy

The milestone observer starts from loaded totals. Only thresholds crossed since
the previous observation produce a brief celebration. Both distance observations
use the current PPI, so changing scale alone crosses nothing. City changes do not
reset counts or replay badges. Large jumps summarize the highest crossed tier per
category, avoiding an unbounded celebration queue.

No raw event histories, ordered keys, historical positions, app/context/calendar
information, analytics or network dependencies are introduced. The source checks
cover input routing, persistence, motion formulas, dates, normalization, crossings,
navigation and rendering scales. See test-results.txt and VERIFICATION.txt.
