# Build & Packaging

[🇻🇳 Vietnamese Version](../vi/build-and-packaging.md)

This guide covers how to build the SlideGenerator application for production distribution.

## Build Process Overview

The build process consists of two main stages:
1.  **Backend Build:** Compiling the .NET application into a self-contained executable.
2.  **Frontend Build:** Bundling the React app and packaging it with Electron, including the backend binary.

## 1. Building the Backend

The backend must be built first so it can be copied into the frontend's resource folder.

**Command:**
```bash
# Run from repository root
./build.ps1 -Runtime win-x64
```
*or on Linux:*
```bash
./build.sh linux-x64
```

**Output:**
The compiled binaries will be placed in `backend/bin/Release/net10.0/<runtime>/publish`.

## 2. Building the Frontend

Once the backend is ready, you can build the Electron app.

**Command:**
```bash
# Run from frontend/ directory
npm run build:full
```

This script performs the following actions:
1.  `build:backend`: Copies the published backend files to `frontend/backend`.
2.  `build`: Runs Vite to bundle the React application.
3.  `electron-builder`: Packages everything into an installer (NSIS for Windows, AppImage for Linux).

## Distribution

### Output Artifacts
The final installers are located in `frontend/release/`.

- **Windows:** `SlideGenerator Setup <version>.exe`
- **Linux:** `SlideGenerator-<version>.AppImage`

### Signing (Optional)
To sign the application (required for auto-updates and to avoid SmartScreen warnings):
1.  Set `CSC_LINK` and `CSC_KEY_PASSWORD` environment variables.
2.  Refer to [electron-builder documentation](https://www.electron.build/code-signing) for details.

## Troubleshooting

- **Missing Backend:** If the app launches but does nothing, ensure the backend binary was correctly copied to `resources/backend` inside the installed app.
- **Runtime Error:** Verify that the target machine meets the OS requirements (though the .NET runtime is self-contained, some OS dependencies might be needed on Linux).
