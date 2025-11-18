# Configuration Guide

This document describes the configuration system for the IPC API Server, including all available settings, how to configure them, and best practices.

## Overview

The configuration system supports multiple configuration sources with the following priority (highest to lowest):

1. **Environment Variables** - Runtime configuration via environment
2. **Configuration File** - JSON file (default: `config.json`)
3. **Default Values** - Built-in defaults

## Configuration Options

### Server Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `host` | string | `"127.0.0.1"` | Server host address |
| `port` | int | `5000` | Server port number |
| `debug` | bool | `false` | Enable debug mode |

**Environment Variables:**
- `APP_HOST` - Override host
- `APP_PORT` - Override port
- `APP_DEBUG` - Override debug mode

### Download Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `download_dir` | string | `"./downloads"` | Directory for downloaded files |
| `max_concurrent_downloads` | int | `5` | Maximum number of simultaneous downloads |
| `max_workers_per_download` | int | `4` | Maximum threads for parallel chunk download |
| `chunk_size` | int | `1048576` | Download chunk size in bytes (1MB) |

**Environment Variables:**
- `DOWNLOAD_DIR` - Override download directory
- `MAX_CONCURRENT_DOWNLOADS` - Override max concurrent downloads
- `MAX_WORKERS_PER_DOWNLOAD` - Override max workers per download
- `CHUNK_SIZE` - Override chunk size

### Retry Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `max_retries` | int | `3` | Maximum retry attempts on failure |
| `initial_retry_delay` | float | `1.0` | Initial delay in seconds before first retry |
| `max_retry_delay` | float | `60.0` | Maximum delay in seconds between retries |
| `retry_backoff_multiplier` | float | `2.0` | Exponential backoff multiplier |
| `retry_on_status_codes` | list | `[408, 429, 500, 502, 503, 504]` | HTTP status codes that trigger retry |

**Environment Variables:**
- `MAX_RETRIES` - Override max retries
- `INITIAL_RETRY_DELAY` - Override initial retry delay
- `MAX_RETRY_DELAY` - Override max retry delay
- `RETRY_BACKOFF_MULTIPLIER` - Override backoff multiplier

**Exponential Backoff Formula:**
```
delay = initial_retry_delay × (retry_backoff_multiplier ^ retry_count)
actual_delay = min(delay, max_retry_delay)
```

**Example:** With defaults (initial=1.0, multiplier=2.0, max=60.0):
- Retry 1: 1.0s
- Retry 2: 2.0s
- Retry 3: 4.0s
- Retry 4: 8.0s
- ...
- Retry 6+: 60.0s (capped)

### Network Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `request_timeout` | int | `30` | Request timeout in seconds |
| `connect_timeout` | int | `10` | Connection timeout in seconds |

**Environment Variables:**
- `REQUEST_TIMEOUT` - Override request timeout
- `CONNECT_TIMEOUT` - Override connection timeout

### Parallel Chunk Download Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `enable_parallel_chunks` | bool | `true` | Enable parallel chunk downloading |
| `min_file_size_for_parallel` | int | `10485760` | Minimum file size (10MB) to use parallel chunks |

**Environment Variables:**
- `ENABLE_PARALLEL_CHUNKS` - Override parallel chunks enable
- `MIN_FILE_SIZE_FOR_PARALLEL` - Override minimum file size

## Configuration Methods

### 1. Configuration File (config.json)

Create a `config.json` file in the project root:

```json
{
  "host": "0.0.0.0",
  "port": 8080,
  "debug": false,
  "download_dir": "./my_downloads",
  "max_concurrent_downloads": 10,
  "max_workers_per_download": 6,
  "chunk_size": 2097152,
  "max_retries": 5,
  "initial_retry_delay": 2.0,
  "max_retry_delay": 120.0,
  "retry_backoff_multiplier": 2.5,
  "request_timeout": 60,
  "connect_timeout": 15,
  "enable_parallel_chunks": true,
  "min_file_size_for_parallel": 20971520
}
```

The server will automatically load this file on startup if it exists.

### 2. Environment Variables

Set environment variables before starting the server:

**Windows (PowerShell):**
```powershell
$env:APP_PORT = "8080"
$env:DOWNLOAD_DIR = "D:\Downloads"
$env:MAX_CONCURRENT_DOWNLOADS = "10"
python src/main.py
```

**Linux/Mac (Bash):**
```bash
export APP_PORT=8080
export DOWNLOAD_DIR="/home/user/downloads"
export MAX_CONCURRENT_DOWNLOADS=10
python src/main.py
```

### 3. API Endpoints (Runtime Configuration)

#### Get All Configuration
```http
GET /api/config
```

**Response:**
```json
{
  "host": "127.0.0.1",
  "port": 5000,
  "debug": false,
  "download_dir": "./downloads",
  ...
}
```

#### Get Specific Configuration Value
```http
GET /api/config/<key>
```

**Example:**
```http
GET /api/config/max_concurrent_downloads
```

**Response:**
```json
{
  "max_concurrent_downloads": 5
}
```

#### Update Multiple Configuration Values
```http
PUT /api/config
Content-Type: application/json

{
  "max_concurrent_downloads": 10,
  "chunk_size": 2097152,
  "max_retries": 5
}
```

**Response:**
```json
{
  "success": true,
  "config": {
    "host": "127.0.0.1",
    "port": 5000,
    ...
    "max_concurrent_downloads": 10,
    "chunk_size": 2097152,
    "max_retries": 5
  }
}
```

#### Update Single Configuration Value
```http
PUT /api/config/<key>
Content-Type: application/json

{
  "value": 10
}
```

**Example:**
```http
PUT /api/config/max_concurrent_downloads
Content-Type: application/json

{
  "value": 10
}
```

**Response:**
```json
{
  "success": true,
  "max_concurrent_downloads": 10
}
```

#### Reset Configuration to Defaults
```http
POST /api/config/reset
```

**Response:**
```json
{
  "success": true,
  "config": {
    // ... default configuration values
  }
}
```

#### Save Configuration to File
```http
POST /api/config/save
```

**Response:**
```json
{
  "success": true,
  "message": "Configuration saved"
}
```

## Usage Examples

### Example 1: High-Performance Configuration

For systems with good network and powerful hardware:

```json
{
  "max_concurrent_downloads": 10,
  "max_workers_per_download": 8,
  "chunk_size": 4194304,
  "enable_parallel_chunks": true,
  "min_file_size_for_parallel": 5242880
}
```

### Example 2: Conservative Configuration

For systems with limited resources or unstable network:

```json
{
  "max_concurrent_downloads": 2,
  "max_workers_per_download": 2,
  "chunk_size": 524288,
  "max_retries": 5,
  "initial_retry_delay": 3.0,
  "max_retry_delay": 180.0,
  "enable_parallel_chunks": false
}
```

### Example 3: Custom Download Directory

Using environment variable:
```powershell
$env:DOWNLOAD_DIR = "D:\MyProject\Images"
```

Or in config.json:
```json
{
  "download_dir": "D:\\MyProject\\Images"
}
```

### Example 4: Production Server Configuration

```json
{
  "host": "0.0.0.0",
  "port": 80,
  "debug": false,
  "download_dir": "/var/www/downloads",
  "max_concurrent_downloads": 20,
  "max_workers_per_download": 6,
  "request_timeout": 120,
  "connect_timeout": 30,
  "max_retries": 5,
  "retry_backoff_multiplier": 2.0
}
```

## Best Practices

### 1. Choosing Concurrent Downloads

- **Low bandwidth**: `max_concurrent_downloads: 2-3`
- **Medium bandwidth**: `max_concurrent_downloads: 5-10`
- **High bandwidth**: `max_concurrent_downloads: 10-20`

### 2. Choosing Workers Per Download

- **Small files (<10MB)**: `max_workers_per_download: 1-2`
- **Medium files (10-100MB)**: `max_workers_per_download: 4-6`
- **Large files (>100MB)**: `max_workers_per_download: 6-10`

### 3. Chunk Size Selection

- **Slow connection**: `524288` (512KB)
- **Normal connection**: `1048576` (1MB) - **Default**
- **Fast connection**: `2097152` (2MB) or `4194304` (4MB)

### 4. Retry Configuration

For **unstable networks**:
```json
{
  "max_retries": 5,
  "initial_retry_delay": 3.0,
  "max_retry_delay": 180.0,
  "retry_backoff_multiplier": 2.5
}
```

For **stable networks**:
```json
{
  "max_retries": 3,
  "initial_retry_delay": 1.0,
  "max_retry_delay": 60.0,
  "retry_backoff_multiplier": 2.0
}
```

### 5. Parallel Chunks

Enable parallel chunks for:
- ✅ Large files (>10MB)
- ✅ Servers with Range request support
- ✅ Fast, stable network connections

Disable parallel chunks for:
- ❌ Small files (<10MB)
- ❌ Servers without Range request support
- ❌ Slow or unstable networks

## Configuration Priority Example

Given:
- config.json: `{"port": 5000}`
- Environment: `APP_PORT=8080`
- Default: `port: 5000`

Result: `port = 8080` (environment variable wins)

## Monitoring Configuration

Get queue and download information:

```http
GET /api/download/queue
```

**Response:**
```json
{
  "queued_tasks": 3,
  "active_downloads": 5,
  "max_concurrent": 10,
  "queue": ["task-id-1", "task-id-2", "task-id-3"]
}
```

## Troubleshooting

### Downloads are slow
- Increase `max_workers_per_download`
- Increase `chunk_size`
- Enable `enable_parallel_chunks`
- Check network bandwidth

### Frequent download failures
- Increase `max_retries`
- Increase retry delays
- Check `retry_on_status_codes`
- Verify network stability

### High memory usage
- Decrease `max_concurrent_downloads`
- Decrease `max_workers_per_download`
- Decrease `chunk_size`

### Server not responding
- Check `request_timeout` and `connect_timeout`
- Verify server is running on correct `host` and `port`
- Check firewall settings

## Default Configuration

```json
{
  "host": "127.0.0.1",
  "port": 5000,
  "debug": false,
  "download_dir": "./downloads",
  "max_concurrent_downloads": 5,
  "max_workers_per_download": 4,
  "chunk_size": 1048576,
  "max_retries": 3,
  "initial_retry_delay": 1.0,
  "max_retry_delay": 60.0,
  "retry_backoff_multiplier": 2.0,
  "retry_on_status_codes": [408, 429, 500, 502, 503, 504],
  "request_timeout": 30,
  "connect_timeout": 10,
  "enable_parallel_chunks": true,
  "min_file_size_for_parallel": 10485760
}
```
