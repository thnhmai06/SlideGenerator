# Module: Coordinator (Concurrency)

## The Hook (Q&A)

**Q: Why do we need a custom Locker?**  
Microsoft Office files (Excel, PowerPoint) and network downloads are sensitive to concurrent access. Opening the same file twice or hitting a server with too many simultaneous requests can lead to `IOException` or IP bans. The `GateLocker` centralizes access control to ensure system stability.

**Q: What is a "Gate"?**  
A Gate is a logical semaphore with a defined capacity. For example, the `Excel` gate might have a capacity of 1 (strictly sequential access), while the `Network` gate might allow 4 concurrent downloads.

---

## 1. Gate Types

- **`Excel`**: Protects `.xlsx` file handles. Prevents "File in use" errors.
- **`PowerPoint`**: Protects `.pptx` file handles during cloning and assembly.
- **`Network`**: Limits concurrent downloads to avoid throttling.
- **`CPU`**: Limits heavy image processing tasks to keep the system responsive.

---

## 2. Usage Pattern

Services wrap their logic inside a `GateLocker.LockAsync` call. This ensures the semaphore is always released (via `IDisposable`) even if an exception occurs.

```csharp
using (await gateLocker.LockAsync(GateType.Excel, ct))
{
    // Perform file operations safely here
}
```

---

## 3. Benefits

- **Predictable Performance**: No CPU spikes from 100+ threads trying to crop images at once.
- **Zero Orphaned Handles**: Centralized management ensures files are closed properly.
- **Graceful Queuing**: Steps wait their turn automatically instead of failing immediately.