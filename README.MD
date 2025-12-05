# PulseAPK

PulseAPK is a Windows Presentation Foundation (WPF) desktop client that wraps the `apktool` command-line utility with a modern MVVM-based interface. It guides users through selecting APK files, configuring apktool flags, and running decompile operations while streaming console output in-app.

## Features
- **Guided decompilation:** Choose the APK, toggle resource/source decoding, and preserve the original manifest when needed.
- **Safe output handling:** Warns about risky output folders and prompts before overwriting existing directories.
- **Live console log:** Streams apktool output and errors directly into the UI.
- **Configurable settings:** Persist the apktool JAR path in a user-level settings file under the application data folder.

## Requirements
- Windows with .NET 8.0 SDK (for building and running the project).
- Java runtime available on `PATH` (apktool executes via `java -jar`).
- `apktool.jar` downloaded locally.

## Getting started
1. **Configure dependencies**
   - Download the latest `apktool.jar` and note its location.
   - Ensure `java` is accessible from the command line.
2. **Build the app**
   ```bash
   dotnet build
   ```
3. **Run the app**
   ```bash
   dotnet run
   ```
4. **Set the apktool path**
   - Open the **Settings** view and browse to the `apktool.jar` location. The path is persisted to `%AppData%/APKToolUI/settings.json`.

## How it works
- The UI is defined in `MainWindow.xaml` with views for **Decompile** and **Settings** hosted via view models.
- `DecompileViewModel` constructs apktool command arguments based on user options, validates paths, and streams output using `ApktoolRunner`.
- `ApktoolRunner` shells out to `java -jar <apktool.jar>` and relays stdout/stderr through an event consumed by the view model.
- `SettingsService` loads and saves the apktool path, while `FilePickerService` wraps OS dialogs used by the UI.

## Project structure
- `App.xaml`, `MainWindow.xaml` — Application bootstrap and primary window layout.
- `Views/` — XAML views for decompiling APKs and editing settings.
- `ViewModels/` — MVVM presentation logic, commands, and validation for each view.
- `Services/` — Helpers for running apktool, picking files/folders, and persisting settings.
- `Utils/` — Utilities for path defaults and APK validation.
- `Resources/` — Theme resources and icons used throughout the UI.
- `APKToolUI.csproj` — Project file targeting `.NET 8.0-windows` with WPF enabled.

## File tree
```
PulseAPK/
├─ App.xaml
├─ MainWindow.xaml
├─ Views/
├─ ViewModels/
├─ Services/
├─ Utils/
├─ Resources/
├─ APKToolUI.csproj
└─ APKToolUI.sln
```
