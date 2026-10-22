# Study Tracker

Windows desktop app to track college coursework: one screen per block, course and assignment, with status, deadlines, grades feedback, and the brief, rubrics and submission files organized right in the course folders.

It works directly on the folder where the coursework lives (a Google Drive for desktop folder, in my case). There is no database: the folder and its `status-dados.js` file are the source of truth, so the app, the old static panel and an AI assistant can all keep working on the same data.

The interface is in Portuguese; the code is in English.

## Features

Screens go from the whole course down to one assignment:

| Screen | Route | What it shows |
|---|---|---|
| Blocks | `/blocos` | Every block (term pair) with progress and its courses |
| Block | `/` and `/blocos/{code}` | Counters by status, next steps, deadlines, missing context, folder suggestions, courses by group. `/` opens the block marked `atual` (or the first one with course data); a picker switches blocks |
| Block assignments | `/blocos/{code}/trabalhos` | The block's assignments, filterable by status |
| Course | `/blocos/{code}/{sigla}` | The course's assignments as rows, with stage dots and folder warnings; drag files onto the header to save them in the course folder (`.vtt` transcripts go to its `Transcrição das Aulas` subfolder) |
| Assignment | `/blocos/{code}/{sigla}/{id}` | Folder contents and suggestions, brief, rubrics, editable state, that assignment's history, and a button that copies the prompt for Claude Code |
| History | `/historico` | The last entries recorded in `status-dados.js` |

Finished blocks (the summarized `concluido` form) open read-only.

- **Folders**: reads each assignment folder of every block and suggests updates when the files disagree with the data (brief and rubrics present, folder created, deliverable named with the wrong course code). Changes on disk show up on their own. The app never creates folders: when an assignment folder is missing it says so and offers the prompt that asks Claude Code to create the structure.
- **Brief and rubrics**: paste the text on the assignment screen and it is saved at once as `Enunciado TPn.md` or `Rubricas TPn.txt`; when the file exists its text is shown.
- **Editing**: status, deadline, submission date, stages, next step, feedback and notes, inline on the assignment screen, with a history line generated from what changed. No dialogs.
- **Files**: drag files onto an assignment (or pick them) and they are saved in its folder with the expected names (`Enunciado TP2.docx`, `Rubricas TP2.txt`, or the original name).
- **Prompt for Claude**: copies a message with block, course, assignment, folder path, status, stages, what is in the folder and what is missing ("Novo trabalho", "Retomar trabalho" or "Correção recebida").
- **Explorer**: opens block, course and assignment folders in Windows Explorer.
- **Theme**: system, light or dark, chosen in the sidebar and remembered.

## How it works with the data file

`status-dados.js` is a small script (`window.DADOS = { ... };`) that the original static panel loads from disk. The app reads and writes the same file and keeps it byte-for-byte compatible: same key order, same JSON layout, unknown fields preserved. Editing it by hand or with an assistant keeps working.

Every save goes through a version check. An edit is applied on top of the latest file on disk and is only refused when that same assignment changed in the edited fields; then the form keeps what was typed and shows what is in the file now. Writes go to a temp file first and then replace the original, so a sync never sees a half-written file.

Only folders inside the configured study folder can be read, written or opened, whatever folder names the data file contains.

## Install (Windows)

Requires the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (the SDK includes it) and the WebView2 runtime (included in Windows 11).

1. Download `StudyTracker-<version>-win-x64.zip` from the latest [release](../../releases) and extract it anywhere.
2. Run `StudyTracker.exe`. On the first run it asks for the study folder (the one with `status-dados.js`) and the deliverable prefix, and saves them to `%LOCALAPPDATA%\StudyTracker\appsettings.Local.json`. It asks again whenever that folder no longer has the data file.
3. Opening the app again while it is open brings the existing window to the front.

To update, close the app and replace the extracted files with the new release; the settings stay in `%LOCALAPPDATA%`. The build is not code-signed, so Windows SmartScreen asks for confirmation the first time.

## Releases

- **Release** (`.github/workflows/release.yml`): every push to `main` runs the tests and, if they pass, publishes a framework-dependent win-x64 build as version `1.0.<run number>`, tagged `v1.0.<run number>`, with generated notes.
- **CI** (`.github/workflows/ci.yml`): builds and runs the tests on pull requests.

The version shows at the bottom of the sidebar: `v1.0.<n>` for releases and `0.0.0-dev` when running from source.

## Settings

| Key | Default | Meaning |
|---|---|---|
| `StudyFolder:RootPath` | (required) | Folder with `status-dados.js` and the block folders |
| `StudyFolder:DeliverablePrefix` | empty | File name prefix of submitted work, e.g. `guilherme_cassel` in `guilherme_cassel_DR3_TP1.pdf` |
| `StudyFolder:PollSeconds` | `30` | How often folders are read again, besides the file watcher |
| `StudyFolder:MaxUploadMegabytes` | `200` | Largest file accepted by drag and drop |

Settings are read from `appsettings.json` next to the app, then `%LOCALAPPDATA%\StudyTracker\appsettings.Local.json`, then environment variables and command line.

## Run from source

1. Create `%LOCALAPPDATA%\StudyTracker\appsettings.Local.json` from `src/StudyTracker.Desktop/appsettings.Local.example.json`.
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
| `StudyTracker.Infrastructure` | Reading and writing the study folder: data file, folder scan, watcher, file writer (never creates folders), Explorer |
| `StudyTracker.UI` | Razor class library with the screens, components, CSS and app state |
| `StudyTracker.Desktop` | WinForms window hosting the screens in a BlazorWebView (Blazor Hybrid, .NET 10) |
| `tests/*` | xUnit tests for Core, Infrastructure and UI |

## Notes

- There is no web server: the screens run inside the app process and render in WebView2, so the app can read the Google Drive folder and open Explorer windows in the user's session.
- Google Drive's virtual drive can drop file system events, so folders are also read on a timer and whenever the window is activated.
