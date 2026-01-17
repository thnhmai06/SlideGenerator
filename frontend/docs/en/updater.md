# Updater

[Tiếng Việt](../vi/updater.md)

The application uses `electron-updater` to provide automated updates for non-portable Windows builds.

## Features

- **Automatic Check**: Checks for updates on application startup.
- **Manual Check**: Users can trigger a check from the About screen.
- **Differential Downloads**: Only downloads what has changed (standard `electron-updater` behavior).
- **Safety Guard**: Prevents installation if there are active slide generation jobs.
- **Portable Detection**: Automatically disables updates when running from a portable executable.

## Architecture

### Main Process (`electron/main/updater.ts`)

- Manages the `autoUpdater` instance.
- Handles IPC calls for checking, downloading, and installing updates.
- Broadcasts status updates to all renderer windows via `updater:status`.
- Persists "downloaded" state to handle app restarts.

### Preload (`electron/preload/api.ts`)

- Exposes typed methods to the renderer via `window.electronAPI`:
  - `checkForUpdates()`
  - `downloadUpdate()`
  - `installUpdate()`
  - `onUpdateStatus(callback)`
  - `isPortable()`

### React Context (`src/shared/contexts/UpdaterContext.tsx`)

- Provides `useUpdater()` hook.
- Synchronizes local state with IPC events.
- Tracks `hasActiveJobs` to gate the installation process.

## Update Flow

1. **Check**: Application calls `checkForUpdates`. Status transitions to `checking`.
2. **Available**: If a newer version is found, status becomes `available`.
3. **Download**: User clicks download. Status becomes `downloading` with progress percentage.
4. **Ready**: Once downloaded, status becomes `downloaded`.
5. **Install**: User clicks install. The app calls `quitAndInstall()`.
   - _Note_: The UI disables the Install button if `hasActiveJobs` is true.

## Configuration

The updater configuration is read from `package.json` under `build.publish`.

```json
"build": {
  "publish": {
    "provider": "github",
    "owner": "your-username",
    "repo": "your-repo"
  }
}
```

## Testing in Development

The updater is configured to allow testing in development mode:

- `autoUpdater.forceDevUpdateConfig = true` is set when `app.isPackaged` is false.
- A `dev-app-update.yml` may be required in the root for local testing.
