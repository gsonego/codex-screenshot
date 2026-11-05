# Codex Screenshot

A floating screenshot button that captures a screen region and pastes it into your **ChatGPT or Codex message on Windows**.

<img src="assets/camera.png" alt="Codex Screenshot camera icon" width="96">

Click **Print**, select an area, and the image is automatically pasted into the message field that had focus. Review your attachment and send the message when you are ready.

## Features

- Appears only while the ChatGPT or Codex desktop app has focus.
- Captures rectangular regions across one or multiple monitors.
- Pastes the image automatically without sending the message.
- Can be repositioned using the three dots on the right side of the button.
- Supports **Ctrl+Alt+S** when the shortcut is available.
- Cancels selection with **Esc** or a right-click.
- Includes a system tray menu to hide, re-enable, or quit the utility.

## Build and run

The floating button requires Windows with an active desktop session, Windows PowerShell 5.1, and the .NET Framework C# compiler at `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`.

Run these commands from the project directory:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build-Button.ps1
.\CodexScreenshot.exe
```

The build script generates the camera icon, compiles the application, and checks selection coordinates and application identification. Executables are generated locally and are not tracked in this repository.

If the utility is already running, quit it from its tray menu and run the build again to update `CodexScreenshot.exe`. The newly built executable is also available at `build\CodexScreenshot.exe`.

## Usage

1. Click the message field in ChatGPT or Codex so the text cursor is active.
2. Click the floating **Print** button.
3. Drag to select the area you want to capture, then release the mouse button.
4. Review the attached image and send your message.

Drag the three dots on the right side to move the button next to your chat. Switching to another application hides the button within approximately 150 ms. Returning to ChatGPT or Codex restores the button at the same position. The system tray icon remains available.

This is a separate local utility; it does not modify the application's **+** menu. Focus detection recognizes the `ChatGPT` and `Codex` desktop processes. It does not detect ChatGPT tabs in a web browser.

## Behavior and limitations

- Capturing a region replaces the clipboard contents.
- The floating button does not save screenshots to disk, start with Windows, or send messages.
- Automatic pasting uses the field that had focus before selection. Click the message field before capturing.
- If focus changes or Windows prevents the utility from restoring the destination window, the image remains on the clipboard for manual pasting with **Ctrl+V**.
- Applications running as administrator may block automatic pasting.
- Protected content, a locked screen, or a session without an interactive desktop may result in a black image or a capture error.
- The button keeps its position when switching applications, but its position resets when the utility restarts.
- The utility currently displays some messages and menu items in Portuguese.

## Optional MCP plugin

The project also includes an MCP tool for asking an assistant to capture the primary monitor or the entire desktop. This tool returns an image as its result and provides a different workflow from the floating region selector.

Additional requirements: Node.js 18 or later on `PATH` and the Codex CLI to install the plugin. No npm dependencies, API key, or remote server are required.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1
```

The installer registers the local marketplace `codex-screenshot-local` and installs `codex-screenshot@codex-screenshot-local`. It updates the current user's plugin configuration. Open a new local conversation afterward; restarting the desktop app may be necessary.

To uninstall the plugin:

```powershell
codex plugin remove codex-screenshot@codex-screenshot-local
codex plugin marketplace remove codex-screenshot-local
```

The `Copy screenshot.cmd` helper provides a primary-monitor capture after a three-second delay and copies it to the clipboard for manual pasting.

## Project structure

| File | Purpose |
| --- | --- |
| `CodexScreenshot.cs` | Floating button, region selection, focus detection, and automatic pasting |
| `Build-Button.ps1` | Icon generation, compilation, and checks |
| `assets/` | Camera icon in PNG and ICO formats |
| `plugins/codex-screenshot/` | Plugin manifest, MCP server, and PowerShell capture script |
| `tests/server.test.mjs` | MCP protocol tests using synthetic images |
| `Install.ps1` | Optional MCP plugin installation |

## Checks

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build-Button.ps1
node --test tests/server.test.mjs
```

Automated tests do not capture the real desktop. Region selection and pasting have been manually verified in the desktop application. Changes to this integration should also be checked in the client interface.

This is an independent project and is not an official OpenAI product.
