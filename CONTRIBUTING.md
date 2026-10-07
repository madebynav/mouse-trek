# Contributing

Thank you for checking Pixel Trek. Small, reproducible improvements are welcome.

## Build

Use Windows 10/11 with .NET Framework 4.8. From PowerShell, run `./Build.ps1`. The native executable and configuration are written to `dist`.

## Verify

Run checks in a scratch folder, not your real saved-data folder:

```powershell
./dist/PixelTrek.exe --self-test C:\Temp\PixelTrek-tests
./dist/PixelTrek.exe --preview C:\Temp\PixelTrek-preview
./dist/PixelTrek.exe --smoke C:\Temp\PixelTrek-smoke 30
./dist/PixelTrek.exe --benchmark C:\Temp\PixelTrek-benchmark
```

Self-test reports `test-results.txt`; preview generates illustrative UI images, rendering its own native widgets briefly offscreen; smoke uses an isolated data directory, installs the actual input listeners, renders its own widget, saves and exits. Smoke does not inject physical input. Physical-device checks must be performed separately.

Before suggesting a change, verify diagonal distance, held-key repeats, rolling-hour expiry, pause, save/reload and older-save migration where relevant. For the Trek edition, check city selection persistence, all named stops, the 20 m checkpoint, route completion/repeated laps and older preferences with removed analytics enabled. For physics changes, test steady velocity, stopping and direction reversal. For UI changes, inspect normal/compact modes and display scaling.

## Design constraints

- Keep the UI native. Preserve the small-widget workflow.
- Keep input callbacks short; no disk/network operations on the input thread's callbacks.
- Preserve raw pixel counts as the canonical data. Label any converted units explicitly.
- No stored text, ordered key sequences, exact trails, window titles or screen contents. App/process and calendar collection are outside this edition.
- No telemetry or uploads without a separately discussed, explicit opt-in design.
- Bound history and avoid logging every input event.
- Preserve existing saves; document format changes and recovery behavior.
- Report measurements with the environment and limits of verification.

## Pull requests

Explain the concrete issue, new behavior and relevant checks. Note AI assistance and verify the resulting code; it is welcome, but it does not replace review. Preserve appropriate license notices and attribution. Do not claim tests that were not run.

## Good first contributions

- Test a trackpad, high-polling-rate mouse or multiple keyboard devices.
- Verify the normal and compact widgets on mixed-DPI displays.
- Improve keyboard access and screen-reader descriptions.
- Measure CPU use during active movement with a documented setup.
- Review the conversion and smoothing explanations for clarity.

These are suggested work areas, not fabricated open issues or completed tests.


For 0.3.2 UI changes, verify all three native views, zero-filled chart dates, local midnight, daily/window cumulative totals, independent normalization, keyboard date selection, series toggles, milestone replay suppression and paused/hidden animation. The opt-in benchmark renders the actual native paint path into a reused offscreen bitmap with synthetic aggregate motion; its output includes fixture overhead.
