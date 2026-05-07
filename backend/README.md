# SlideGenerator Backend

Welcome to the technical documentation for **SlideGenerator**, a powerful PowerPoint automation system driven by Excel data.

## Detailed Documentation

Please refer to the following files for a deep dive into the system:

1.  **[System Architecture](./Documents/Eng/ARCHITECTURE.md)**: Overview of the Modular Monolith, IPC Sidecar, and data flow.
2.  **[API & Workflow Documentation](./Documents/Eng/API_WORKFLOW.md)**: Full list of JSON-RPC Endpoints and WorkflowCore orchestration logic.
3.  **[Tech Stack & Standards](./Documents/Eng/STANDARDS.md)**: Detailed tech stack and mandatory coding standards.
4.  **[Installation & Setup (Setup Guide)](./Documents/Eng/SETUP.md)**: Detailed steps to configure environment variables and build the project.

---

## Quick Start

> **⚠️ Important:** You must configure the `SYNCFUSION_LICENSE_KEY` in your `.env` file before running the application. See the [Setup Guide](./Documents/Eng/SETUP.md) for details.

```bash
# 1. Configure license
cp .env.example .env
# Edit .env and fill in SYNCFUSION_LICENSE_KEY

# 2. Build project
dotnet restore
dotnet build SlideGenerator.sln
```

### Key Capabilities
- Blazingly fast IPC via StreamJsonRpc.
- Resilient workflow orchestration with WorkflowCore.
- Smart image processing (ROI & Face Detection).
- Multi-cloud support (Google Drive, OneDrive, SharePoint).
