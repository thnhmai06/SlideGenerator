# Concurrency Management: The GateLocker System

This document explains how SlideGenerator manages system resources and prevents exhaustion during high-volume generation tasks.

## The Problem
Generating slides often involves hundreds of parallel tasks:
- **Network**: Hundreds of image downloads.
- **CPU/RAM**: Heavy image processing via Magick.NET.
- **I/O**: Frequent reads/writes to large Excel and PowerPoint files.

Without throttling, the system would quickly run out of memory or be blocked by cloud providers for excessive requests.

---

## The Solution: GateLocker

The `GateLocker` is a centralized concurrency controller that enforces limits on specific types of operations, known as **Gates**.

### Gate Types
- `DownloadImage`: Prevents IP blocking and network congestion.
- `EditImage`: Limits concurrent CPU-heavy image manipulations.
- `EditPresentation`: Protects against file-lock contention and excessive RAM usage during PPTX assembly.
- `ReadWorkbook` / `ReadPresentation`: Throttles initial parsing of large documents.

### How it Works
Workflow steps must "acquire" a gate before performing a protected operation and "release" it afterward:

```csharp
await gateLocker.AcquireAsync(GateType.EditImage, cancellationToken);
try {
    // Perform CPU-heavy image processing
} finally {
    gateLocker.Release(GateType.EditImage);
}
```

---

## Dynamic Throttling

Unlike static semaphores, the `GateLocker` integrates with the **Settings Module**.

- **Live Updates**: If a user changes the "Max Parallel Downloads" in the application settings, the `GateLocker` immediately begins enforcing the new limit for all subsequent requests.
- **Fairness**: Waiters are queued in a FIFO (First-In-First-Out) order using a `LinkedList`, ensuring that earlier workflow steps get priority as slots become available.

## Implementation Details
- **AcquireAsync**: A non-blocking asynchronous wait. If the gate is full, the task is suspended until a slot opens.
- **TryAcquire**: An immediate check used in scenarios where the system should skip a task or fall back if resources are busy.
- **Thread Safety**: Uses `lock` and `ConcurrentDictionary` to manage state across parallel workflow branches.
