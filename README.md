# IPC API Server - Backend

Backend API server for slide generation application with advanced download management and data processing capabilities.

## Features

### 🚀 Advanced Download Manager
- **Multi-threaded downloads**: Download multiple files simultaneously
- **Parallel chunk downloading**: Split large files into chunks for faster downloads
- **Auto-retry with exponential backoff**: Automatic retry on network failures
- **Pause/Resume support**: Pause and resume downloads with HTTP Range requests
- **Queue management**: Intelligent download queue with concurrency control
- **Multiple sources**: Support for Google Drive, OneDrive, Google Photos

### 📊 Data Manager (Polars-based)
- **Lazy loading**: Efficient CSV/Excel reading without memory leaks
- **Hierarchical structure**: Group → Sheet → Data
- **Pagination support**: Load data in chunks
- **1-based indexing**: Intuitive row access

### ⚙️ Flexible Configuration
- **Environment variables**: Configure via environment
- **Configuration file**: JSON-based config file support
- **Runtime API**: Update configuration via REST API
- **Sensible defaults**: Works out of the box

## Architecture

See [ARCHITECTURE.md](docs/ARCHITECTURE.md) for detailed architecture documentation.

### Directory Structure
```
backend/
├── src/
│   ├── main.py              # Flask API server
│   ├── config.py            # Configuration management
│   ├── services/
│   │   ├── download_manager.py  # Download orchestration
│   │   ├── data_manager.py      # Data processing
│   │   └── get_image.py         # URL extraction
│   └── utils/
│       ├── __init__.py
│       ├── file.py          # File utilities
│       ├── http.py          # HTTP utilities
│       └── validation.py    # Validation utilities
├── CONFIG.md               # Configuration guide
├── PAUSE_RESUME.md        # Pause/resume feature docs
├── ARCHITECTURE.md        # Architecture documentation
├── README.md
└── requirements.txt
```

## Installation

```bash
cd backend/data
pip install -response requirements.txt
```

## Running the Server

### Basic Usage
```bash
python src/main.py
```

Server runs at `http://127.0.0.1:5000` by default.

### With Custom Configuration
```bash
# Using environment variables
$env:APP_PORT = "8080"
$env:DOWNLOAD_DIR = "D:\Downloads"
$env:MAX_CONCURRENT_DOWNLOADS = "10"
python src/main.py

# Or create CONFIG.json in project root
```

See [CONFIG.md](CONFIG.md) for detailed configuration options.

## API Endpoints

### Download APIs

#### 1. Create Download Task
```http
POST /api/download/create
Content-Type: application/json

{
    "url": "https://drive.google.com/file/d/...",
    "save_dir": "downloads"  // optional, uses config default if not specified
}

Response:
{
    "success": true,
    "task_id": "uuid-string"
}
```

**Features:**
- Automatic URL detection for Google Drive, OneDrive, Google Photos
- Queues task for download based on concurrency limits
- Returns immediately with task ID for tracking

#### 2. Get Task Status
```http
GET /api/download/status/<task_id>

Response:
{
    "task_id": "uuid-string",
    "url": "https://...",
    "status": "queued|pending|detecting|downloading|paused|completed|error",
    "progress": 75.5,
    "total_size": 1024000,
    "downloaded_size": 771072,
    "file_path": "/path/to/file.jpg",
    "error_message": null,
    "is_downloaded": false,
    "supports_resume": true,
    "retry_count": 0
}
```

**Status Values:**
- `queued`: Waiting in download queue
- `pending`: Preparing to download
- `detecting`: Extracting download URL
- `downloading`: Actively downloading
- `paused`: Download paused
- `completed`: Download finished
- `error`: Download failed

#### 3. List All Tasks
```http
GET /api/download/list

Response:
{
    "tasks": [...]
}
```

#### 4. Get Queue Information
```http
GET /api/download/queue

Response:
{
    "queued_tasks": 3,
    "active_downloads": 5,
    "max_concurrent": 10,
    "queue": ["task-id-1", "task-id-2", "task-id-3"]
}
```

#### 5. Pause Task
```http
POST /api/download/pause/<task_id>

Response:
{
    "success": true
}
```

**Note:** Server must support Range requests for resume capability.

#### 6. Resume Task
```http
POST /api/download/resume/<task_id>

Response:
{
    "success": true
}
```

**Note:** Only works if task is paused and server supports resume.

#### 7. Cancel Task
```http
DELETE /api/download/cancel/<task_id>

Response:
{
    "success": true
}
```

### Configuration APIs

#### 1. Get All Configuration
```http
GET /api/config

Response:
{
    "host": "127.0.0.1",
    "port": 5000,
    "download_dir": "./downloads",
    "max_concurrent_downloads": 5,
    "max_workers_per_download": 4,
    ...
}
```

#### 2. Update Configuration
```http
PUT /api/config
Content-Type: application/json

{
    "max_concurrent_downloads": 10,
    "chunk_size": 2097152
}

Response:
{
    "success": true,
    "config": {...}
}
```

#### 3. Get Specific Config Value
```http
GET /api/config/<key>

Response:
{
    "<key>": <value>
}
```

#### 4. Set Specific Config Value
```http
PUT /api/config/<key>
Content-Type: application/json

{
    "value": <new_value>
}

Response:
{
    "success": true,
    "<key>": <new_value>
}
```

#### 5. Reset Configuration
```http
POST /api/config/reset

Response:
{
    "success": true,
    "config": {...}
}
```

#### 6. Save Configuration
```http
POST /api/config/save

Response:
{
    "success": true,
    "message": "Configuration saved"
}
```

### Data Manager APIs

#### 1. Load CSV/Excel File
```
POST /api/data/load
Content-Type: application/json

{
    "file_path": "/path/to/file.xlsx",
    "group_id": "my_data"  // optional, formerly file_id
}

Response:
{
    "success": true,
    "group_id": "my_data",
    "file_type": ".xlsx",
    "num_sheets": 3,
    "sheets": ["sheet_0", "sheet_1", "sheet_2"]
}
```

#### 2. Hủy load file
```
DELETE /api/data/unload/<group_id>

Response:
{
    "success": true
}
```

#### 3. Liệt kê các file đã load
```
GET /api/data/files

Response:
{
    "files": [
        {
            "group_id": "my_data",
            "file_path": "/path/to/file.xlsx",
            "file_type": ".xlsx",
            "num_sheets": 3
        }
    ]
}
```

#### 4. Lấy danh sách sheet
```
GET /api/data/<group_id>/sheets

Response:
{
    "sheets": [
        {
            "sheet_id": "sheet_0",
            "sheet_name": "Data",
            "num_rows": 100,
            "num_cols": 5
        }
    ]
}
```

#### 5. Lấy danh sách cột
```
GET /api/data/<group_id>/sheets/<sheet_id>/columns

Response:
{
    "columns": ["Name", "Age", "Email", ...]
}
```

#### 6. Lấy thông tin sheet
```
GET /api/data/<group_id>/sheets/<sheet_id>/info

Response:
{
    "sheet_id": "sheet_0",
    "sheet_name": "Data",
    "num_rows": 100,
    "num_cols": 5,
    "columns": ["Name", "Age", "Email", ...],
    "start_row": 0,
    "start_col": 0
}
```

#### 7. Lấy dữ liệu sheet (với pagination)
```
GET /api/data/<group_id>/sheets/<sheet_id>/data?offset=0&limit=10

Query params:
    - offset: Starting row index (default: 0)
    - limit: Maximum rows to return (optional, default: all remaining)

Response:
{
    "columns": ["Name", "Age", "Email"],
    "data": [
        {"Name": "John", "Age": 25, "Email": "john@example.com"},
        ...
    ],
    "num_rows": 10,
    "offset": 0,
    "total_rows": 100
}
```

#### 8. Lấy một bản ghi cụ thể (NEW)
```
GET /api/data/<group_id>/sheets/<sheet_id>/rows/<row_index>

Path params:
    - row_index: Row index (1-based, 1 = first data row after header)

Response:
{
    "row_index": 5,
    "data": {
        "Name": "Alice",
        "Age": 30,
        "Email": "alice@example.com"
    }
}
```

### Health Check
```http
GET /api/health

Response:
{
    "status": "ok",
    "message": "IPC API Server is running"
}
```

## Key Features Explained

### 🚀 Multi-threaded Downloads

The download manager can handle multiple file downloads simultaneously. Configure via:
- `max_concurrent_downloads`: Maximum files downloading at once (default: 5)
- `download_queue`: Automatic queuing when limit reached

**Example:** Download 10 files, only 5 active at once, others queued.

### ⚡ Parallel Chunk Downloads

Large files (>10MB by default) are split into chunks and downloaded in parallel:
- Configurable via `max_workers_per_download` (default: 4 threads)
- Configurable chunk size via `chunk_size` (default: 1MB)
- Automatic merge after all chunks complete
- Much faster for large files on fast connections

**Conditions for parallel chunks:**
- `enable_parallel_chunks` must be `true`
- File size >= `min_file_size_for_parallel` (default: 10MB)
- Server must support HTTP Range requests

### 🔄 Auto-Retry with Exponential Backoff

Automatic retry on network failures with smart backoff:
- Configurable `max_retries` (default: 3)
- Exponential backoff: delay = initial_delay × (multiplier ^ retry_count)
- Configurable delays and multiplier
- Retries on specific HTTP status codes (408, 429, 500, 502, 503, 504)

**Example retry delays** (with defaults):
1. First retry: 1.0s
2. Second retry: 2.0s
3. Third retry: 4.0s

### ⏸️ Pause/Resume Support

Pause and resume downloads using HTTP Range requests:
- Works with servers that support `Accept-Ranges: bytes`
- Saves progress in `.part` files
- Resume from exact byte position
- Survives application restarts (if `.part` file preserved)

See [PAUSE_RESUME.md](PAUSE_RESUME.md) for detailed documentation.

### 📊 Data Manager Features

**Lazy Loading:**
- Uses Polars `scan_csv()` for CSV files
- Only reads metadata initially (row/column counts)
- Data loaded on-demand via `collect()` or `fetch()`
- No memory leaks from repeated file opens

**Pagination:**
- Load data in chunks: `?offset=0&limit=100`
- Efficient for large datasets
- Combines with lazy loading

**1-based Indexing:**
- `get_row(1)` returns first row (not 0-indexed)
- More intuitive for end users

## Configuration

The application supports flexible configuration through multiple methods:

1. **Environment Variables** (highest priority)
2. **config.json file**
3. **Default values** (lowest priority)

### Quick Configuration Examples

**Change download directory:**
```bash
$env:DOWNLOAD_DIR = "D:\MyDownloads"
```

**Increase concurrent downloads:**
```bash
$env:MAX_CONCURRENT_DOWNLOADS = "10"
```

**Change server port:**
```bash
$env:APP_PORT = "8080"
```

See [CONFIG.md](CONFIG.md) for complete configuration guide.

## Usage Examples

### Example 1: Download with Custom Settings

```python
# Update configuration for high-performance downloading
PUT /api/config
{
    "max_concurrent_downloads": 10,
    "max_workers_per_download": 8,
    "enable_parallel_chunks": true
}

# Create multiple download tasks
POST /api/download/create
{"url": "https://drive.google.com/..."}

POST /api/download/create
{"url": "https://onedrive.live.com/..."}

# Monitor queue
GET /api/download/queue
```

### Example 2: Load and Process CSV File

```python
# Load file
POST /api/data/load
{
    "file_path": "data/students.xlsx",
    "group_id": "students_2024"
}

# Get sheet list
GET /api/data/students_2024/sheets

# Get sheet info
GET /api/data/students_2024/sheets/sheet_0/info

# Get first 10 records
GET /api/data/students_2024/sheets/sheet_0/data?offset=0&limit=10

# Get next 10 records (pagination)
GET /api/data/students_2024/sheets/sheet_0/data?offset=10&limit=10

# Get specific row (1-based)
GET /api/data/students_2024/sheets/sheet_0/rows/5
```

### Example 3: Pause and Resume Download

```python
# Create download task
POST /api/download/create
{"url": "https://large-file-url"}
# Response: {"task_id": "abc-123"}

# Check status
GET /api/download/status/abc-123
# Response: {"status": "downloading", "progress": 45.5}

# Pause download
POST /api/download/pause/abc-123

# Check status
GET /api/download/status/abc-123
# Response: {"status": "paused", "progress": 45.5}

# Resume download
POST /api/download/resume/abc-123

# Check status
GET /api/download/status/abc-123
# Response: {"status": "downloading", "progress": 45.5, ...}
```

## Performance Tips

### For Fast Downloads
- Increase `max_concurrent_downloads` (10-20)
- Increase `max_workers_per_download` (6-8)
- Increase `chunk_size` (2-4MB)
- Enable `enable_parallel_chunks`

### For Stability
- Decrease `max_concurrent_downloads` (2-3)
- Increase retry settings
- Decrease `max_workers_per_download`

### For Limited Resources
- Decrease concurrent downloads
- Disable parallel chunks
- Smaller chunk size

See [CONFIG.md](CONFIG.md) for detailed tuning guide.

## Documentation

- **[CONFIG.md](CONFIG.md)** - Complete configuration guide
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture
- **[PAUSE_RESUME.md](PAUSE_RESUME.md)** - Pause/resume feature details

## Dependencies

- **Flask 3.0.0** - Web framework
- **Flask-CORS 4.0.0** - CORS support
- **Polars 0.20.0** - Fast DataFrame library with lazy loading
- **Openpyxl 3.1.2** - Excel file reading
- **Requests 2.31.0** - HTTP library

## Troubleshooting

### Downloads fail frequently
- Check network connection
- Increase `max_retries` and retry delays
- Check if URL is valid
- Verify server supports required features

### High memory usage
- Decrease `max_concurrent_downloads`
- Decrease `max_workers_per_download`
- Use pagination for data access
- Check if lazy loading is working

### Slow downloads
- Increase `max_workers_per_download`
- Increase `chunk_size`
- Enable parallel chunks
- Check network bandwidth

### Cannot pause/resume
- Verify server supports HTTP Range requests
- Check `supports_resume` field in task status
- Ensure `.part` file exists for resume

## License

This project is part of the slide generation application.
