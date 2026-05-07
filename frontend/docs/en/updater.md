# Updater

[Tiếng Việt](../vi/updater.md)

The application updater now uses the Tauri v2 updater plugin.

## Features

- **Automatic Check**: Checks for updates on application startup.
- **Manual Check**: Users can trigger a check from the About screen.
- **Differential Downloads**: Managed by Tauri updater artifacts/signatures configuration.
- **Safety Guard**: Prevents installation if there are active slide generation jobs.
- **Portable Detection**: Automatically disables updates when running from a portable executable.

## Architecture

### Desktop Host (`src-tauri/src/main.rs` and updater plugin config)

- Uses Tauri updater plugin lifecycle and release metadata.
- Exposes update flow to renderer through Tauri APIs and app bridge adapters.

### Frontend Bridge (`desktopApi` adapter)

- Exposes typed updater methods to the renderer via a Tauri-based desktop adapter:
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
5. **Install**: User clicks install. The app applies update and relaunches.
   - _Note_: The UI disables the Install button if `hasActiveJobs` is true.

## Configuration

The updater configuration is read from `src-tauri/tauri.conf.json` under `plugins.updater`.

```json
"plugins": {
  "updater": {
    "active": true,
    "pubkey": "PUBLIC_KEY_CONTENT",
    "endpoints": ["https://example.com/latest.json"]
  }
}
```

## Testing in Development

The updater is configured to allow testing in development mode:

- Development updater behavior follows Tauri plugin configuration and environment setup.
