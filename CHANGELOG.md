# Changelog

## 0.3.2 — Dashboard, Treks and Graph

- Native Dashboard startup with matching speed and acceleration gauges.
- Monotonic display interpolation and measured-sample wake-up; bounded active/idle refresh.
- Treks destination with five city routes, lap percentage, next stop and all badges.
- Thirty-date local Daily/Cumulative Graph with independent normalization, series toggles and fixed exact values.
- Missing-date and empty-history handling; bounded chart arrays and midnight rollover.
- New milestone strip with launch/scale replay suppression; short header tooltips.
- Preserved input formulas, compatible totals, compact/tray modes, history and exports.
- Expanded checks and refreshed native screenshots/documentation.

## 0.3.1 - Trek edition

- Returned to one playful Trek home. Removed the analytics engine, its controls and the Flow/Loops/Signals/Context/Privacy tabs from the executable and current source.
- Added a native persisted dropdown with New York (default), Sydney, London, Moscow and Paris. Replaced both previous route choices.
- Added six named stops per city, including a playful 20 m street checkpoint, winding S-like trails, a traveller dot and repeated virtual laps.
- Added a city-themed badge card with progress and changed the home comparisons to match the selected city. Retained all ten km thresholds and expanded input achievements.
- Enlarged the normal window by approximately 15% per dimension to 575 x 472; retained compact/tray modes. Placed acceleration centrally and counts underneath.
- Kept the original input/motion engine and odometer save format. Old analytics files are never loaded or modified; older preferences migrate to the New York default.
- Passed 109 checks, including actual native city selection, migration, curved-path progress and the existing motion/render regressions.
## 0.3.0

- Added six native sidebar tabs: Trek, Flow, Loops, Signals, Context and Privacy; a central motion bloom; and a reactive mouse mascot.
- Replaced pixel milestones with ten screen-kilometre tiers and referenced landmark comparisons. Added two offline journey sketches and expanded key/click achievements.
- Added optional local focus candidates, repeated click/movement patterns, reviewable shortcut advice, path straightness, reversal proxies, personal baselines and gentle break nudges.
- Added independently opted-in process context, Writing/Design categories, time-of-day comparisons and local timed calendar imports with supported daily/weekly recurrence.
- Added a separate known-target task for Fitts-style throughput; passive input does not invent target widths.
- Added bounded, independent insight persistence, deletion and exports. All new collection starts off. Existing odometer totals and the v0.2 motion engine carry over.
- Passed 116 automated checks, inspected all six native tab renders, and completed isolated native startup/save smoke runs with insights off and on.

## 0.2.1

- Fixed the acceleration gauge crash at small nonzero readings. Replaced the GDI+ arc call with a bounded curve drawn from line segments.
- Reproduced the v0.2.0 OutOfMemoryException with a 0.1-degree sweep before applying the fix.
- Added gauge rendering regression checks for tiny, zero, normal and over-range readings at 100%, 125%, 150%, 200% and 300% display scales.
- Retained the UI, measurement formulas and existing saved counters.

## 0.2.0

- Added live smoothed cursor speed and two-dimensional acceleration gauge.
- Added peak speed/acceleration for today, rolling hour and lifetime.
- Added kilometre-equivalent distance alongside pixels.
- Added configurable screen scale and monitor-PPI calculator.
- Added daily CSV export with counts, scale and motion peaks.
- Extended recap images with kilometre equivalents and peak rates.
- Preserved v0.1 totals and widget preferences.
- Added GitHub documentation, contribution guidance and measurement notes.

## 0.1.0

- Initial native portable widget with cursor pixels, key presses and clicks.
- Daily, rolling-hour and lifetime statistics; local save recovery.
- Compact/tray modes, explorer milestones, daily history and recap PNG export.

