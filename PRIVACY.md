# Privacy - Trek edition

Pixel Trek 0.3.1 is a local cursor odometer. Its executable contains no workflow, focus, strain, app-context or calendar analytics engine. Old opt-in flags do not enable those removed features.

## Input and storage

The input thread uses observed cursor positions to add segment lengths, and physical key-down/up state to suppress held-key repeats. Positions, scan codes and key identities are transient. Only aggregate pixel distances, key-tap/click counts, motion peaks and bounded second/day buckets are stored. The keyboard's typed text is never assembled or saved. Exact cursor trails, window titles, app/process names, screenshots and calendar data are not collected.

Preferences include city, period, scale, window position, compact/topmost/animation settings and pause state. User-requested recap/CSV/backup exports are written only when a destination is chosen. The app makes no network requests and has no account or cloud backend.

## Persistence

`%LOCALAPPDATA%\PixelTrek\counters.json` stores counts and preferences, with a prior-save `.bak` and temporary atomic checkpoint. At most 366 active dates and 3,600 rolling-hour buckets are kept; lifetime totals are independent. Saved files and exports are ordinary local files and are not encrypted by this app.

Older `insights.json` and its backup, if present, are left untouched and never opened by this edition. To erase old advanced history, quit the app and remove only those old insight files manually. Odometer data is in `counters.json`.

## Controls

Pause suspends counting; Quit saves and ends tracking. Hiding or compacting keeps counting. Optional Start with Windows changes only the current user's Run entry and can be disabled in the same menu. No administrator elevation is requested. The app's measurement scope is the normal desktop; secure/elevated contexts may be unavailable.
