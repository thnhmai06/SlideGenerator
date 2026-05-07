# Module: Settings & Configuration

## The Hook (Q&A)

**Q: Where are settings stored?**  
Settings are persisted as **YAML** files. This choice provides a balance between human readability and machine parsability, making it easy for users to tweak configurations manually if needed.

**Q: How is configuration managed during runtime?**  
The `SettingManager` acts as a central hub. It loads settings at startup, provides thread-safe access to configuration objects, and handles atomic saves to disk.

---

## 1. Configuration Types

- **JobConfig**: Defines output paths, file naming patterns, and global generation rules.
- **DownloadConfig**: Manages concurrent download limits and timeout settings.
- **ImageConfig**: Controls default ROI types and processing quality.

---

## 2. Serialization

We use **YamlDotNet** for serialization. This allows us to support advanced features like comments and clean formatting in the configuration files.

---

## 3. IPC Integration

Settings can be queried and updated in real-time via the `settings.*` JSON-RPC methods, allowing the frontend to provide a rich UI for configuration.