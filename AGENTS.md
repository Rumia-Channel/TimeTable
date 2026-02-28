# Repository Guidelines

## Project Structure & Module Organization
This repository is a single Windows Forms app targeting .NET Framework 4.8 and built from source files in the repo root.
- `Program.cs`: application entry point.
- `MainForm.cs`: editor UI, grid input handling, and export flow.
- `TimetableRenderer.cs`: drawing engine plus timetable data models.
- `Fonts/`: bundled `.ttf` files loaded at runtime and copied during build.
- `bin/`: compiled output (`TimeTableApp.exe`), ignored by Git.

Reference images may exist locally for validation, but generated image files are ignored.

## Build, Test, and Development Commands
- `.\build.bat`: compiles with `csc.exe`, outputs `.\bin\TimeTableApp.exe`, and copies fonts.
- `.\bin\TimeTableApp.exe`: runs the app after a successful build.
- `Remove-Item .\bin -Recurse -Force`: optional clean before rebuilding.

No package restore step is required.

## Coding Style & Naming Conventions
Follow the style already used in the codebase:
- 4-space indentation, Allman braces, and readable method-sized blocks.
- `PascalCase` for types, methods, and public fields.
- private UI fields use `_camelCase` (for example, `_stationBox`).
- event handlers follow `<Control>_<Event>` naming (for example, `ResetButton_Click`).

Keep compatibility with .NET Framework 4.8; avoid APIs that require newer runtimes.

## Testing Guidelines
There is no automated test project yet. Validate changes with a manual smoke pass:
1. Run `.\build.bat` and confirm success.
2. Launch `.\bin\TimeTableApp.exe`.
3. Edit header fields and grid cells across tabs.
4. Export `PNG/JPG/BMP` and verify the image opens correctly.
5. Confirm text and symbols render with bundled fonts.

For bug fixes, include clear reproduce/verify steps in the PR.

## Commit & Pull Request Guidelines
Current history uses concise subject lines with a prefix and colon (example: `初期設定: MainForm、Program、ビルドスクリプトを追加`).
- Prefer `scope: summary` (or equivalent Japanese format), imperative and specific.
- Keep the subject line short (target <= 72 characters).
- One logical change per commit.

PRs should include purpose, key file changes, manual test steps, and screenshots when UI/rendered output changes. Link related issues when available.
