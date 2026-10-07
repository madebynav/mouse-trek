PIXEL TREK 0.3.2

Native Windows cursor odometer: Dashboard, Treks and Graph.

RUN
Extract all files and open PixelTrek.exe on Windows 10/11 with .NET Framework 4.8.
Quit the previous copy before replacing its app files; saved totals carry over.

CONTROLS
Treks: five city trails, next stop, lap progress and complete achievements.
Graph: 30 local calendar dates; Daily/Cumulative; series toggles; exact values.
Back/Escape: Dashboard. Today/Last hour/Lifetime: lower count cards.
Drag header: move. Minus: compact. Arrow: hide to tray. Double-click tray: restore.
Three dots/right-click: pause, scale, history, peaks, exports and settings.
Quit & save: save totals and stop tracking.

UNITS
Pixels are canonical cursor path distance. Km, m/s and m/s2 are screen equivalents
at the selected PPI, not physical mouse travel. The chart normalizes each series
against its own window maximum. Cumulative totals begin at zero for this window.
City trails are schematic, with a playful 20 m checkpoint and approximate legs.

LOCAL DATA
No typed text, ordered keys, historical cursor trails, app names, calendar data,
screen contents or telemetry. No network requests or browser runtime.
Counts, peaks and preferences: %LOCALAPPDATA%\PixelTrek\counters.json.
Atomic saves every 10 seconds and on orderly quit, with previous-save backup.
366 active dates retained; lifetime separate. Unsaved activity can be lost on interruption.

BUILD
Source package: run .\Build.ps1 in PowerShell. Output is dist.
No package restore needed. Read README.md and CONTRIBUTING.md for details.

Created by Nav Medikonda with AI assistance from OpenAI Codex. MIT license.
