# Study Tracker

Local app to track college coursework: status of every assignment by term and course, deadlines, grades feedback, and submission files organized right in the course folders.

It runs on your own machine and works directly on the folder where the coursework lives (a Google Drive for desktop folder, in my case). There is no database: the folder and its `status-dados.js` file are the source of truth, so the app, the old static panel and an AI assistant can all keep working on the same data.

> Work in progress. The v1 plan is in [docs/plano-v1.md](docs/plano-v1.md) (Portuguese).

## Stack

- .NET 10, ASP.NET Core, Blazor (Interactive Server)
- xUnit

## Running

1. Install the .NET 10 SDK.
2. Copy `src/StudyTracker.Web/appsettings.Local.example.json` to `appsettings.Local.json` in the same folder and set `StudyFolder:RootPath` to your study folder.
3. Run:

   ```
   dotnet run --project src/StudyTracker.Web
   ```

4. Open http://localhost:5280.

The app only listens on the loopback interface (`localhost`). It is meant for a single user on their own machine.

## Tests

```
dotnet test
```

## Project layout

| Project | Role |
|---|---|
| `StudyTracker.Core` | Domain model and rules, no file access |
| `StudyTracker.Infrastructure` | Reading and writing the study folder |
| `StudyTracker.Web` | Blazor UI |
| `tests/*` | xUnit tests for Core and Infrastructure |
