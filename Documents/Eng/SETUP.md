# Installation & Setup Guide

This document provides detailed instructions on how to set up the environment and run the SlideGenerator project.

## Prerequisites
- **.NET 10 SDK**: The project is built on the latest .NET platform.
- **Git**: For source code management.

## Setup Steps

### 1. Configure Environment Variables

This is a critical step before running the application. The project uses Syncfusion for document processing, which requires a valid License Key.

1.  Copy the example configuration file:
    ```bash
    cp .env.example .env
    ```
2.  Open the `.env` file and fill in your license key:
    ```env
    SYNCFUSION_LICENSE_KEY=your_license_key_here
    ```

> **Note:** Without a valid license key, features related to Excel and PowerPoint may be restricted or display Syncfusion's evaluation watermark.

### 2. Restore and Build

Use the terminal in the project's root directory:

```bash
# Restore NuGet packages
dotnet restore

# Build the entire Solution
dotnet build SlideGenerator.sln
```

### 3. Running the Application

Since the project operates as an IPC Sidecar, you can run the executable directly from the `bin` folder after building:

```bash
dotnet run --project SlideGenerator.Ipc/SlideGenerator.Ipc.csproj
```

## Additional Configuration
Beyond environment variables, detailed configurations regarding Throttling limits, storage paths, and logging can be found in `appsettings.json` or YAML configuration via the `Settings` module.
