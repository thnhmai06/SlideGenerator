# Configuration

Vietnamese version: [Vietnamese](../vi/configuration.md)

## Where config lives

- The backend loads `backend.config.yaml` from the working directory.
- If the file is missing, defaults are saved on first run.
- A sample file is provided as `backend.config.sample.yaml`.

## Key settings

- `server`: host, port, debug.
- `job`: `maxConcurrentJobs` controls how many sheet jobs run in parallel.
- `image`: face detection confidence, max dimension (default 1280, 0 = unlimited) and saliency padding.
- `download`: bandwidth limits and retry policy (used when downloading images).

## Runtime behavior

- Hangfire persists job state in a SQLite database (`jobs.db` by default).
- Worker count is derived from `job.maxConcurrentJobs`.

## Safety rules

- Config updates are blocked while any group is Pending or Running.
- Paused tasks do not block configuration changes.

Next: [Job system](job-system.md)
