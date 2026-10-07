# Upload this source to GitHub

1. Extract PixelTrek-v0.3.2-GitHub-Ready-Source.zip.
2. Create an empty GitHub repository named PixelTrek (or use your chosen name).
3. Upload the extracted contents into the repository root. Include .github and .gitignore.
4. Keep README.md, LICENSE, assets and docs alongside Build.ps1 and the .cs files.
5. Check the Windows build and checks workflow after the first push.
6. For a release, tag v0.3.2 and attach the separately supplied portable ZIP.

The source ZIP is the complete buildable project. The portable ZIP is the runnable app.
Generated build output and private counters are excluded from the source package.

Suggested release title: **Pixel Trek 0.3.2 — Dashboard, Treks and Graph**.

This release adds smooth native speed/acceleration instruments, separate Treks and
Graph views, a normalized 30-day Daily/Cumulative chart, fixed selected-date values,
milestone celebrations and shorter header tooltips. Existing counts remain compatible.

Screenshots contain illustrative data. Report only the supplied local verification;
GitHub CI has not run until it runs in your repository. The executable is unsigned.
Retain the MIT license and credits to Nav Medikonda and AI assistance from OpenAI Codex.
No GitHub repository or release was published by this packaging task.
