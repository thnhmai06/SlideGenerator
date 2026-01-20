# Configuration

[🇻🇳 Vietnamese Version](../vi/configuration.md)

The backend is configured via a YAML file named `backend.config.yaml` located in the working directory.

## Configuration File

On the first run, if the file is missing, the application will generate a default `backend.config.yaml`. You can also use `backend.config.sample.yaml` as a reference.

### Structure & Key Settings

```yaml
server:
  host: "localhost"
  port: 5000
  debug: false   # Enable detailed debug logging

job:
  # Maximum number of sheet jobs running in parallel across all groups.
  maxConcurrentJobs: 4 

image:
  # Face detection confidence threshold (0.0 - 1.0)
  faceConfidence: 0.7
  # Max dimension for image resizing (0 = unlimited)
  maxDimension: 1280
  # Padding added to detected regions of interest
  saliencyPadding: 0.1

download:
  # Network settings for downloading remote images
  maxBandwidth: 0 # 0 = unlimited
  retryCount: 3
```

## Runtime Behavior

### Persistence
- **Job State:** Stored in a SQLite database (`jobs.db` by default). This allows the application to resume tasks after a restart.
- **Worker Pool:** The number of background processing threads is automatically adjusted based on `job.maxConcurrentJobs`.

### Safety Mechanisms

To ensure data integrity, the system enforces the following rules regarding configuration changes:

1.  **Blocked Updates:** You cannot change configuration settings while any job group is in `Pending` or `Running` state.
2.  **Allowed Updates:** Configuration can be safely updated when all active jobs are `Paused` or when there are no active jobs.

Next: [Job System](job-system.md)
