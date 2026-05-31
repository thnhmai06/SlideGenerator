# SlideGenerator Documentation

Welcome to the SlideGenerator backend documentation. This project is a **Modular Monolith** designed for high-performance PowerPoint automation.

## 🏗️ Architecture
- [System Overview](Architecture/System-Overview.md): Understanding the Modular Monolith and IPC Sidecar.
- [Workflow Engine](Architecture/Workflow-Engine.md): How we use WorkflowCore for resilient processing.
- [Concurrency Management](Architecture/Concurrency-Management.md): The GateLocker system.

## 🛠️ Development Guides
- [Setup Guide](Guides/Setup-Guide.md): How to get the project running.
- [Development Standards](Guides/Development-Standards.md): Coding rules, DI, and conventions.
- [Adding Workflow Steps](Guides/Adding-Workflow-Steps.md): Extending the generation pipeline.

## 📖 Reference
- [IPC API Reference](Reference/IPC-API-Reference.md): JSON-RPC 2.0 endpoints and protocols.
- **[Module Reference](Reference/Modules/)**: Detailed documentation for each internal project.
  - [Utilities](Reference/Modules/Utilities.md)
  - [Settings](Reference/Modules/Settings.md)
  - [Cloud](Reference/Modules/Cloud.md)
  - [Cryptography](Reference/Modules/Cryptography.md)
  - [Coordinator](Reference/Modules/Coordinator.md)
  - [Document](Reference/Modules/Document.md)
  - [Logging](Reference/Modules/Logging.md)
  - [Image](Reference/Modules/Image.md)
  - [Summarization](Reference/Modules/Summarization.md)
  - [Recipe](Reference/Modules/Recipe.md)
  - [Generator](Reference/Modules/Generator.md)
  - [Stdio](Reference/Modules/Stdio.md)
