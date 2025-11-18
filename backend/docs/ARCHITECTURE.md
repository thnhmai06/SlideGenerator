# Architecture - Data Management System

## Overview

The system follows a hierarchical 3-tier architecture for managing CSV/Excel files:

```
Group (File)
  ├── Sheet 1
  │     ├── Metadata (columns, row count, etc.)
  │     └── Data (lazy loaded)
  ├── Sheet 2
  │     ├── Metadata
  │     └── Data
  └── Sheet N
```

## Components

### 1. Group
- **Purpose**: Represents a single CSV or Excel file
- **Responsibilities**:
  - File path management
  - Sheet discovery and initialization
  - Provide access to sheets
- **Key Features**:
  - Lightweight initialization (only discovers sheets, doesn't load data)
  - Supports both CSV (single sheet) and Excel (multiple sheets)

### 2. Sheet
- **Purpose**: Represents a single sheet within a file
- **Responsibilities**:
  - Metadata management (columns, row count, table position)
  - Lazy data loading
  - Data access with pagination
- **Key Features**:
  - **Lazy Initialization**: Only reads file header to extract metadata
  - **Table Detection**: Automatically finds data start position (handles offset tables)
  - **Memory Efficient**: Doesn't keep full data in memory
  - **On-Demand Loading**: Loads data only when requested

### 3. DataManager
- **Purpose**: Central manager for all groups
- **Responsibilities**:
  - Load/unload groups
  - Provide unified API access
  - Manage group lifecycle

## Lazy Loading Strategy

### Why Lazy Loading?

1. **Memory Efficiency**: Large Excel/CSV files can be hundreds of MB to GBs
2. **Fast Initialization**: User doesn't wait for full file load
3. **No Memory Leaks**: Files are read on-demand, not kept in memory
4. **Scalability**: Can handle multiple large files simultaneously

### How It Works

#### Phase 1: Initialization (Fast, Lightweight)
```python
group = Group("students_2024", "data/students.xlsx")
# Only reads:
# - Sheet names from Excel workbook
# - First 20 rows to detect table structure
# - Column names
# - Row count (using lazy scan for CSV, minimal read for Excel)
```

#### Phase 2: Data Access (On-Demand)
```python
# Option A: Pagination (recommended for large datasets)
sheet.get_rows(offset=0, limit=100)
# Reads only 100 rows from disk

# Option B: Single row access
sheet.get_row(42)
# Reads only the 42nd data row (1-based indexing)

# Option C: Full data (use with caution)
sheet.get_rows(offset=0, limit=None)
# Reads entire sheet
```

## Polars Integration

### CSV Files
- Uses `pl.scan_csv()` for lazy loading
- Row count via lazy aggregation
- Data read on-demand with `skip_rows` and `n_rows`

### Excel Files
- Uses `openpyxl` for sheet discovery (read_only mode)
- Uses `pl.read_excel()` with slicing
- Caches full sheet read (first access), then slices in memory

## Memory Management

### Best Practices

✅ **DO:**
- Use pagination (offset/limit) for large datasets
- Access specific rows by index when possible
- Unload groups when no longer needed
- Use reasonable limits (e.g., 100-1000 rows per request)

❌ **DON'T:**
- Request all data at once for large files
- Keep groups loaded indefinitely
- Make repeated full-data requests

### Example: Processing Large File

```python
# BAD: Load all data at once
response = requests.get(f'/api/data/{group_id}/sheets/{sheet_id}/data')
# Could load 1GB+ into memory!

# GOOD: Use pagination
offset = 0
limit = 100
while True:
    response = requests.get(
        f'/api/data/{group_id}/sheets/{sheet_id}/data',
        params={'offset': offset, 'limit': limit}
    )
    data = response.json()
    
    # Process chunk
    process_chunk(data['data'])
    
    if data['num_rows'] < limit:
        break  # Last chunk
    offset += limit
```

## Table Detection Algorithm

Many real-world Excel/CSV files have:
- Title rows at the top
- Empty rows before data
- Merged cells in header
- Extra columns/rows

### Detection Strategy

1. **Sample first 10 rows** (lightweight read)
2. **Find row with most non-null values** (likely the header)
3. **Use that row as column names**
4. **Start data from next row**
5. **Remove completely empty columns**

### Example

```
Row 0: "Company Report - Q4 2024"        (title)
Row 1: ""                                 (empty)
Row 2: "Name", "Age", "Email", ""        (header - most non-null)
Row 3: "John", 25, "john@example.com", ""
Row 4: "Alice", 30, "alice@example.com", ""

Detection:
- start_row = 2 (header)
- columns = ["Name", "Age", "Email"]  (empty column removed)
- data starts at row 3
- num_rows = 2
- get_row(1) returns John (first data row)
- get_row(2) returns Alice (second data row)
```

## API Design Principles

1. **Consistent Naming**: `group_id` -> `sheet_id` -> `row_index`
2. **RESTful**: Standard HTTP methods and status codes
3. **Pagination First**: All data endpoints support offset/limit
4. **Metadata Separate**: Info endpoints don't return data
5. **Error Handling**: Clear error messages with appropriate status codes

## Performance Characteristics

| Operation | Time Complexity | Memory Usage |
|-----------|----------------|--------------|
| Load Group | O(sheets × 20 rows) | Minimal |
| Get Metadata | O(1) | Cached |
| Get Row by Index | O(1) seek + O(1) read | Single row |
| Get Rows (paginated) | O(limit) | limit rows |
| Get All Rows | O(n) | Full sheet |

## Future Improvements

1. **Caching**: Cache frequently accessed rows/chunks
2. **Parallel Reading**: Read multiple chunks in parallel
3. **Compression**: Compress cached data
4. **Smart Prefetching**: Predict and prefetch next chunks
5. **Index Building**: Build indexes for faster random access
