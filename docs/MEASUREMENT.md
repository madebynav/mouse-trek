# Measurement notes - Trek edition

## Distance and counts

Mouse travel sums Euclidean distances between observed screen cursor positions, in pixels. The first point after startup/resume is a baseline, not a distance. Injected mouse events are excluded and update/reset the baseline. Physical key-down transitions count once until release; device and extended scan-code state distinguish keyboards/keys. Modifier presses count. Click-down events count left, middle, right and extra buttons; releases do not add clicks.

Pointer acceleration settings, programmatic jumps and pointer-lock games can change observed cursor behavior. This is not a sensor for physical mouse/trackpad travel. Secure desktops and elevated contexts can limit event coverage.

## Periods and saving

Today uses local calendar dates. Last hour is the rolling set of one-second buckets whose timestamps are within the preceding 3,600 seconds; exact boundary buckets expire. Lifetime totals stay independent. Daily history retains 366 active dates; second history retains at most 3,600 buckets. Old peak fields default to zero without losing counts.

Every ten seconds and on orderly quit/session transitions the tracker snapshot is written atomically with a previous-save backup. A valid backup can recover a corrupt primary; both unreadable files are protected from automatic overwrite. Unsaved activity since the latest successful checkpoint can be lost on sudden power loss. Restart expires stale rolling-hour buckets, preserving lifetime totals. Basic save version remains 1; new city preference is a string, with New York as the default when absent or invalid.

## Screen equivalents

`km = pixels * 0.0254 / validPPI / 1000`. Default PPI is 96. The optional monitor calculator uses sqrt(widthPx^2 + heightPx^2) / diagonalInches. The one selected scale applies to all history and monitors, so mixed physical pixel densities are not individually calibrated. Changing PPI recalculates equivalent labels without changing canonical pixels.

The route consumes lifetime km, while Today/Last hour/Lifetime selects the three count cards. Every city uses the same lifetime value; each trail has its own length and virtual laps. See TREKS.md for the 20 m checkpoint, approximate geographic anchors and schematic curves.

## Motion

The existing native input/motion engine samples accumulated travel and two-dimensional cursor deltas at approximately 100 ms while active. Path speed comes from travelled distance per elapsed time; velocity comes from displacement per elapsed time. Exponential smoothing reduces event jitter. Acceleration is the smoothed magnitude of a velocity difference divided by elapsed time, so braking and direction changes contribute. This is descriptive cursor physics, not the operating system's mouse-acceleration setting.

A stationary cursor settles to zero; pause/resume and long gaps reset stale motion. Today/hour/lifetime peak rates use maxima, not sums. The pulse/bloom display is decorative and bounded; its saturated visual scale does not cap the numeric rates. Rendering uses bounded sampled polylines and fixed-radius shapes; no small-angle dynamic GDI+ arc is used.

## Privacy and resource bounds

There is no app/context/calendar/focus/workflow/strain engine in this edition. Old analytics flags are ignored by preferences deserialization; old insight files are never opened. No historical positions or ordered keys are retained. Only the previous cursor point, motion accumulators and held-key suppression state are transient inputs.

Five offline route definitions and five precomputed 41-point curve legs are fixed. Store/render work is outside native input callbacks. Idle animation stops; the visible widget refreshes its clock-based display at most once per second when idle. Compact/tray modes retain tracking. There are no network or browser dependencies.

## Validation

The source self-test covers native event routing, injected events, held-key repeats, pause, date/hour boundaries, peaks, old saves, atomic recovery, bounded history, the 96-PPI formula, steady velocity, braking/direction changes and rendering at five display scales. Trek checks cover all five routes, exact 20 m thresholds, repeated laps, length-based curve progress, actual native selector persistence, unchanged counters, ignored v0.3.0 opt-ins and absence of analytics types from the executable. See test-results.txt and VERIFICATION.txt.

Native previews are illustrative. Resource samples are short-run observations, not guarantees. Physical devices, real mixed-DPI desktops, secure contexts, power-loss recovery and long-term resource use require further testing on the user's hardware. GitHub CI has not run on GitHub yet.
