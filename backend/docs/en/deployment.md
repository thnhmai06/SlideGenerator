# Deployment

Vietnamese version: [Vietnamese](../vi/deployment.md)

## Summary

The backend is an ASP.NET Core app hosted by `SlideGenerator.Presentation`.

## Steps

1. Prepare `backend.config.yaml` (host, port, maxConcurrentJobs).
2. Ensure write access for:
   - the config file location,
   - the Hangfire SQLite path,
   - output folders.
3. Run the server (local or published build).

## Notes

- Default health check: `/health`.
- Hangfire dashboard: `/hangfire` (read-only).
- The server is designed for local/offline usage.
