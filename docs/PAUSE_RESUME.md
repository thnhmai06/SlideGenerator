# Pause/Resume Download Feature

## Overview

The download manager now supports pausing and resuming downloads using HTTP Range requests. This allows users to:
- Temporarily pause downloads
- Resume from where they left off
- Recover from network interruptions
- Save bandwidth by controlling when downloads occur

## How It Works

### 1. Server Requirements

For pause/resume to work, the server must support **HTTP Range requests**. This is indicated by the `Accept-Ranges: bytes` header in the response.

### 2. Download Flow

```
┌─────────────┐
│   PENDING   │ ──create_task──> Start download thread
└─────────────┘
       │
       ▼
┌─────────────┐
│  DETECTING  │ ──get_image()──> Extract download URL
└─────────────┘
       │
       ▼
┌─────────────┐
│ DOWNLOADING │ ◄──resume_task─┐
└─────────────┘                │
       │                       │
       │ (user action)         │
       ▼                       │
┌─────────────┐                │
│   PAUSED    │ ─resume_task───┘
└─────────────┘
       │
       │ (download complete)
       ▼
┌─────────────┐
│  COMPLETED  │
└─────────────┘
```

### 3. Temporary Files

During download, files are saved with a `.part` extension:
- `image.jpg.part` - downloading/paused
- `image.jpg` - completed

This prevents incomplete files from being used accidentally.

### 4. Resume Logic

When resuming a download:

1. Check if `.part` file exists
2. Get file size (bytes already downloaded)
3. Send Range header: `Range: bytes={downloaded_size}-`
4. Server responds with status `206 Partial Content`
5. Append remaining data to `.part` file
6. Rename to final filename when complete

## API Usage

### Pause a Download

```bash
POST /api/download/pause/<task_id>
```

**Response (Success):**
```json
{
    "success": true
}
```

**Response (Error - Task not downloading):**
```json
{
    "error": "Task does not exist or cannot be paused"
}
```

### Resume a Download

```bash
POST /api/download/resume/<task_id>
```

**Response (Success):**
```json
{
    "success": true
}
```

**Response (Error - Server doesn't support resume):**
```json
{
    "error": "Task does not exist or cannot be resumed"
}
```

### Check Resume Support

Check the `supports_resume` field in the status response:

```bash
GET /api/download/status/<task_id>
```

```json
{
    "task_id": "abc-123",
    "status": "paused",
    "progress": 45.5,
    "downloaded_size": 465920,
    "total_size": 1024000,
    "supports_resume": true
}
```

## Client Example

### JavaScript/TypeScript

```javascript
class DownloadClient {
    async createDownload(url) {
        const response = await fetch('/api/download/create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ url })
        });
        const data = await response.json();
        return data.task_id;
    }
    
    async pauseDownload(taskId) {
        const response = await fetch(`/api/download/pause/${taskId}`, {
            method: 'POST'
        });
        return await response.json();
    }
    
    async resumeDownload(taskId) {
        const response = await fetch(`/api/download/resume/${taskId}`, {
            method: 'POST'
        });
        return await response.json();
    }
    
    async getStatus(taskId) {
        const response = await fetch(`/api/download/status/${taskId}`);
        return await response.json();
    }
}

// Usage
const client = new DownloadClient();

// Start download
const taskId = await client.createDownload('https://example.com/image.jpg');

// Monitor progress
const interval = setInterval(async () => {
    const status = await client.getStatus(taskId);
    console.log(`Progress: ${status.progress}%`);
    
    if (status.status === 'completed') {
        clearInterval(interval);
        console.log('Download complete!');
    }
}, 1000);

// User clicks pause
await client.pauseDownload(taskId);

// Later, user clicks resume
await client.resumeDownload(taskId);
```

### Python

```python
import requests
import time

class DownloadClient:
    def __init__(self, base_url='http://localhost:5000'):
        self.base_url = base_url
    
    def create_download(self, url):
        response = requests.post(
            f'{self.base_url}/api/download/create',
            json={'url': url}
        )
        return response.json()['task_id']
    
    def pause_download(self, task_id):
        response = requests.post(
            f'{self.base_url}/api/download/pause/{task_id}'
        )
        return response.json()
    
    def resume_download(self, task_id):
        response = requests.post(
            f'{self.base_url}/api/download/resume/{task_id}'
        )
        return response.json()
    
    def get_status(self, task_id):
        response = requests.get(
            f'{self.base_url}/api/download/status/{task_id}'
        )
        return response.json()

# Usage
client = DownloadClient()

# Start download
task_id = client.create_download('https://example.com/large-image.jpg')

# Monitor for 5 seconds
for _ in range(5):
    status = client.get_status(task_id)
    print(f"Progress: {status['progress']}%")
    time.sleep(1)

# Pause
client.pause_download(task_id)
print("Download paused")

# Wait a bit
time.sleep(3)

# Resume
client.resume_download(task_id)
print("Download resumed")

# Wait for completion
while True:
    status = client.get_status(task_id)
    if status['status'] == 'completed':
        print(f"Download complete: {status['file_path']}")
        break
    time.sleep(1)
```

## Error Handling

### Server Doesn't Support Resume

If server doesn't support Range requests:

```json
{
    "status": "error",
    "error_message": "Server does not support resume",
    "supports_resume": false
}
```

**Solution**: Download cannot be resumed. User must restart from beginning.

### Network Interruption

If connection drops during download:

```json
{
    "status": "error",
    "error_message": "Connection timeout",
    "downloaded_size": 524288,
    "supports_resume": true
}
```

**Solution**: Call `resume_task()` to continue from where it stopped.

### File Already Completed

If trying to pause/resume completed download:

```json
{
    "error": "Task does not exist or cannot be paused"
}
```

**Solution**: Download is already complete, no action needed.

## Implementation Details

### Key Components

1. **DownloadTask**
   - `paused` flag - indicates if user requested pause
   - `supports_resume` - indicates if server supports Range requests
   - `download_url` - cached download URL (avoid re-detection)

2. **Temporary File Management**
   - Files saved as `{filename}.part` during download
   - Renamed to final name on completion
   - Allows resume by checking `.part` file size

3. **Thread Safety**
   - All status checks use `threading.Lock`
   - Prevents race conditions between pause/resume

### Range Request Details

**Initial Request:**
```http
GET /image.jpg HTTP/1.1
Host: example.com
```

**Response:**
```http
HTTP/1.1 200 OK
Content-Length: 1024000
Accept-Ranges: bytes
```

**Resume Request:**
```http
GET /image.jpg HTTP/1.1
Host: example.com
Range: bytes=524288-
```

**Resume Response:**
```http
HTTP/1.1 206 Partial Content
Content-Range: bytes 524288-1023999/1024000
Content-Length: 499712
```

## Limitations

1. **Server Support Required**: Not all servers support Range requests
2. **File Modifications**: If file changes on server, resume may fail
3. **Storage Space**: `.part` files consume disk space during pause
4. **Thread Management**: Each task uses a dedicated thread

## Best Practices

1. **Check Resume Support**: Always check `supports_resume` before allowing pause
2. **Graceful Degradation**: If resume not supported, hide pause button
3. **User Feedback**: Show clear status (downloading/paused/resuming)
4. **Cleanup**: Delete `.part` files on cancel or error
5. **Timeout Handling**: Implement reasonable timeouts for network requests

## Future Enhancements

- [ ] Multi-threaded downloads (multiple Range requests)
- [ ] Automatic retry on network failure
- [ ] Download speed limiting
- [ ] Download queue management
- [ ] Chunk-based downloading for very large files
- [ ] ETag validation for file integrity
