# Contributing Guidelines

## The Hook (Q&A)

**Q: How do I build and test this locally?**  
You need the .NET 8 SDK. Since this is an IPC application, testing it requires either running a mock frontend that pipes `stdin` or using unit/integration test wrappers. Standard `dotnet build` works out of the box.

**Q: What are the coding standards for this project?**  
Pragmatic, direct, and explicit. Avoid generic abstractions unless absolutely necessary. Keep steps in workflows small and focused. Always use dependency injection and respect the `GateLocker` for resource constraints.

---

## 1. Development Principles

1. **No "Just-in-case" Code:** Do not build abstract factories or interfaces unless there are at least two concrete implementations.
2. **Fail Fast & Log Everything:** Use Serilog. If a critical state is violated, throw immediately with context.
3. **Idempotency:** Any pipeline step must be safe to rerun. Never assume a clean slate. Check before creating/downloading.
4. **Clean Code:** Adhere to standard C# conventions (PascalCase for properties, camelCase for local variables). Use `sealed` on classes by default.

## 2. Commit Standards

Use descriptive commit messages focusing on the *Why* rather than the *What*. E.g., "Fix GateLocker race condition to prevent orphaned Excel handles."