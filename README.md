# Study Tracker

Windows desktop app to track college coursework: status of every assignment by term and course, deadlines, grades feedback, and submission files organized right in the course folders.

It works directly on the folder where the coursework lives (a Google Drive for desktop folder, in my case). There is no database: the folder and its `status-dados.js` file are the source of truth, so the app, the old static panel and an AI assistant can all keep working on the same data.

The interface is in Portuguese; the code is in English.

## Features

- **Dashboard**: counters by status, next steps by priority, deadlines, assignments still missing their brief or rubrics, progress per course and recent activity.
- **Assignments**: every assignment of the current block, filterable by status.
- **Courses**: one page per course with each assignment's stages, next step, feedback and notes.
- **Course history**: all blocks taken so far, with what was submitted.
- **Folders**: reads each assignment folder and suggests updates when the files disagree with the data (brief and rubrics present, folder created, deliverable named with the wrong course code). Changes on disk show up on their own.
- **Editing**: status, deadline, submission date, stages, next step, feedback and notes, with a history line generated from what changed.
- **Files**: drag files onto an assignment and they are saved in its folder with the expected names (`Enunciado TP2.docx`, `Rubricas TP2.txt`); rubrics can be pasted as text.
- **Explorer**: opens course and assignment folders in Windows Explorer.

## How it works with the data file

`status-dados.js` is a small script (`window.DADOS = { ... };`) that the original static panel loads from disk. The app reads and writes the same file and keeps it byte-for-byte compatible: same key order, same JSON layout, unknown fields preserved. Editing it by hand or with an assistant keeps working.

Every save goes through a version check. An edit is applied on top of the latest file on disk and is only refused when that same assignment changed in the edited fields; then the edit dialog keeps what was typed and shows what is in the file now. Writes go to a temp file first and then replace the original, so a sync never sees a half-written file.

Only folders inside the configured study folder can be read, written or opened, whatever folder names the data file contains.

## Install (Windows)

Requires the .NET 10 SDK and the WebView2 runtime (included in Windows 11).

```powershell
.\scripts\install.ps1
```

This publishes the app to `%LOCALAPPDATA%\StudyTracker\app` and creates **Study Tracker** shortcuts on the desktop and in the Start menu. Opening it again while it is open brings the existing window to the front. Run the script again to update: it closes the app and keeps the settings.

On the first install, settings are copied to `%LOCALAPPDATA%\StudyTracker\appsettings.Local.json`. Set the folder path there. If the path is missing, the app says so when it starts.

### From a release

Each [release](../../releases) has a self-contained `StudyTracker-<version>-win-x64.zip` (no .NET install needed). Extract it anywhere, create `%LOCALAPPDATA%\StudyTracker\appsettings.Local.json` from the `appsettings.Local.example.json` inside the zip, and run `StudyTracker.exe`. The build is not code-signed, so Windows SmartScreen asks for confirmation the first time.

## Releases

- **CI** (`.github/workflows/ci.yml`) builds and runs the tests on every push and pull request to `main`.
- **Release** (`.github/workflows/release.yml`) runs when a version tag is pushed: it tests, publishes a self-contained win-x64 build with that version and attaches the zip to a GitHub release with generated notes.

```
git tag v1.1.0
git push origin v1.1.0
```

The version shows at the bottom of the sidebar: `v1.1.0` for releases, `<git describe>-local` for `scripts\install.ps1` builds and `0.0.0-dev` when running from source.

## Settings

| Key | Default | Meaning |
|---|---|---|
| `StudyFolder:RootPath` | (required) | Folder with `status-dados.js` and the block folders |
| `StudyFolder:DeliverablePrefix` | empty | File name prefix of submitted work, e.g. `guilherme_cassel` in `guilherme_cassel_DR3_TP1.pdf` |
| `StudyFolder:PollSeconds` | `30` | How often folders are read again, besides the file watcher |
| `StudyFolder:MaxUploadMegabytes` | `200` | Largest file accepted by drag and drop |

Settings are read from `appsettings.json` next to the app, then `%LOCALAPPDATA%\StudyTracker\appsettings.Local.json`, then environment variables and command line.

## Run from source

1. Create `%LOCALAPPDATA%\StudyTracker\appsettings.Local.json` from `src/StudyTracker.Desktop/appsettings.Local.example.json` (or run the installer once).
2. Run:

   ```
   dotnet run --project src/StudyTracker.Desktop
   ```

In Development the WebView developer tools are enabled (F12).

## Tests

```
dotnet test
```

## Project layout

| Project | Role |
|---|---|
| `StudyTracker.Core` | Domain model and rules (priorities, folder rules, file naming, edits), no file access |
| `StudyTracker.Infrastructure` | Reading and writing the study folder: data file, folder scan, watcher, file writer, Explorer |
| `StudyTracker.UI` | Razor class library with the screens, components, CSS and app state |
| `StudyTracker.Desktop` | WinForms window hosting the screens in a BlazorWebView (Blazor Hybrid, .NET 10) |
| `tests/*` | xUnit tests for Core, Infrastructure and UI |

## Notes

- There is no web server: the screens run inside the app process and render in WebView2, so the app can read the Google Drive folder and open Explorer windows in the user's session.
- Google Drive's virtual drive can drop file system events, so folders are also read on a timer and whenever the window is activated.
