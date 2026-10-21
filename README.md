# Study Tracker

Local app to track college coursework: status of every assignment by term and course, deadlines, grades feedback, and submission files organized right in the course folders.

It runs on your own machine and works directly on the folder where the coursework lives (a Google Drive for desktop folder, in my case). There is no database: the folder and its `status-dados.js` file are the source of truth, so the app, the old static panel and an AI assistant can all keep working on the same data.

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

Requires the .NET 10 SDK.

```powershell
.\scripts\install.ps1
```

This publishes the app to `%LOCALAPPDATA%\StudyTracker\app` and creates a **Study Tracker** shortcut on the desktop. The shortcut starts the server without a console window and opens the browser; if the app is already running it only opens the browser. Run the script again to update: settings are kept.

`.\scripts\install.ps1 -StartWithWindows` also starts the app when you sign in.

On the first install, settings are copied to `%LOCALAPPDATA%\StudyTracker\appsettings.Local.json`. Check the folder path there.

## Settings

| Key | Default | Meaning |
|---|---|---|
| `StudyFolder:RootPath` | (required) | Folder with `status-dados.js` and the block folders |
| `StudyFolder:DeliverablePrefix` | empty | File name prefix of submitted work, e.g. `guilherme_cassel` in `guilherme_cassel_DR3_TP1.pdf` |
| `StudyFolder:PollSeconds` | `30` | How often folders are read again, besides the file watcher |
| `StudyFolder:MaxUploadMegabytes` | `200` | Largest file accepted by drag and drop |
| `Urls` | `http://localhost:5280` | Address the app listens on (loopback only) |

Settings are read from `appsettings.json`, then `appsettings.Local.json` next to the app, then `%LOCALAPPDATA%\StudyTracker\appsettings.Local.json`, then environment variables and command line.

## Run from source

1. Copy `src/StudyTracker.Web/appsettings.Local.example.json` to `appsettings.Local.json` in the same folder and set the paths.
2. Run:

   ```
   dotnet run --project src/StudyTracker.Web
   ```

3. Open http://localhost:5280.

The app only listens on the loopback interface. It is meant for a single user on their own machine.

## Tests

```
dotnet test
```

## Project layout

| Project | Role |
|---|---|
| `StudyTracker.Core` | Domain model and rules (priorities, folder rules, file naming, edits), no file access |
| `StudyTracker.Infrastructure` | Reading and writing the study folder: data file, folder scan, watcher, file writer, Explorer |
| `StudyTracker.Web` | Blazor Server UI (ASP.NET Core, .NET 10) |
| `StudyTracker.Launcher` | Tiny Windows app used by the shortcut to start the server without a console |
| `tests/*` | xUnit tests for Core, Infrastructure and Web |

## Notes

- Google Drive's virtual drive can drop file system events, so folders are also read on a timer and whenever the browser tab gets focus.
- `StudyTracker.Web` must stay a console app (`OutputType` `Exe`): the package that ships `blazor.web.js` only adds it to console apps. The launcher exists so the shortcut can still start it without a window, and the install script checks the script was published.
