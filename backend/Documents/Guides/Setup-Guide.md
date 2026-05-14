# Setup Guide

Follow this guide to set up your environment and build the SlideGenerator backend.

## Prerequisites

1. **.NET 10.0 SDK**: The project is pinned to .NET 10.
2. **IDE**: JetBrains Rider (recommended) or Visual Studio 2022 with C# 12/14 support.
3. **Syncfusion License**: A valid license key is required for PowerPoint and Excel processing.

---

## Initial Configuration

### 1. Environment Variables
Copy the `.env.example` file to `.env` in the project root:

```bash
cp .env.example .env
```

Open `.env` and fill in your Syncfusion license key:
```env
SYNCFUSION_LICENSE_KEY=YOUR_KEY_HERE
```

### 2. App Settings
Review `appsettings.json`. By default, the application stores data in `%LOCALAPPDATA%/SlideGenerator`. You can modify paths here if necessary for development.

---

## Building the Project

We use the new `.slnx` (Solution Explorer) format.

### Command Line
```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build SlideGenerator.slnx
```

### IDE
Open `SlideGenerator.slnx` in your IDE. It will automatically detect the projects and modular structure.

---

## Running the IPC Sidecar

The backend is an executable intended to be launched by a frontend. However, you can run it manually for testing:

```bash
cd SlideGenerator.Ipc
dotnet run
```

The application will print a welcome message and wait for JSON-RPC commands on `stdin`. You can pipe commands into it or use a tool like `StreamJsonRpc` tester.

## Troubleshooting

- **License Errors**: Ensure the `.env` file is in the root and the key is correct. The license is registered at startup in `SlideGenerator.Document/Injection/Registration.cs`.
- **Dependency Issues**: If you add a new module, ensure it is added to the `SlideGenerator.slnx` and referenced by `SlideGenerator.Ipc`.
