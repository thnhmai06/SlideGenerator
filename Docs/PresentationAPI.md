API Documentation
=================

# Table of Contents
1. [Overview](#overview)
2. [Protocol](#protocol)
3. [Enums](#enums)
4. [API Endpoints](#api-endpoints)
   - [1. Scan Shapes](#1-scan-shapes)
   - [2. Generate Slides](#2-generate-slides)
5. [Error Handling](#error-handling)
6. [Workflow Examples](#workflow-examples)
7. [Best Practices](#best-practices)

---

# Overview

This API provides endpoints for scanning shapes in PowerPoint templates and generating multiple slides from data sources.

## Base URL

```
ws://localhost:{port}
```

## API Endpoint Format

Connect to specific services using the following URL pattern:

```
ws://localhost:{port}/{ServiceType}
```

Where `{ServiceType}` can be:
- `ScanShapes` - For shape scanning operations
- `GenerateSlide` - For slide generation operations

**Examples:**
```
ws://localhost:5000/ScanShapes
ws://localhost:5000/GenerateSlide
```

---

# Protocol

All communication is done via WebSocket using JSON messages with the following structure:

## Request Format
```json
{
  "Type": "Create" | "Control" | "Status"
}
```

## Response Format
```json
{
  "Type": "Create" | "Control" | "Status" | "Finish" | "Error"
}
```

---

## Enums

### ControlState
Control actions for job execution:
```
Pause   - Pause execution
Resume  - Resume paused execution
Stop    - Stop and cancel execution
```

### RequestType
Types of requests that can be sent:
```
Create  - Create new operation
Control - Control existing operation
Status  - Query operation status
```

### ResponseType
Types of responses from the server:
```
Create  - Operation created acknowledgment
Control - Control action confirmed
Status  - Status update
Finish  - Operation completed
Error   - Error occurred
```

---

# API Endpoints

## 1. Scan Shapes

Extract all shapes from a PowerPoint template file.

**Endpoint:** `ws://localhost:{port}/ScanShapes`

### Request: ScanShapesCreate

**Type:** `Create`

**Payload:**
```json
{
  "Type": "Create",
  "FilePath": "string"
}
```

**Fields:**
- `FilePath` (string, required): Absolute path to the PowerPoint file (.pptx)

**Example:**
```json
{
  "Type": "Create",
  "FilePath": "C:\\Templates\\template.pptx"
}
```

### Responses

#### ScanShapesCreate (Acknowledgment)
```json
{
  "Type": "Create",
  "Path": "C:\\Templates\\template.pptx"
}
```

#### ScanShapesFinish (Success)
```json
{
  "Type": "Finish",
  "Path": "C:\\Templates\\template.pptx",
  "Success": true,
  "Shapes": [
    {
      "Id": 1,
      "Name": "Beautiful Shape"
      "Data": "base64_encoded_image_preview"
    },
    {
      "Id": 2,
      "Name": "Another Beautiful Shape",
      "Data": "base64_encoded_image_preview"
    }
  ]
}
```

**Fields:**
- `Path` (string): Original file path
- `Success` (boolean): Whether the scan was successful
- `Shapes` (array, optional): Array of shape data
  - `Id` (number): Unique shape identifier
  - `Data` (string): Base64-encoded image preview of the shape

#### ScanShapesError
```json
{
  "Type": "Error",
  "Path": "C:\\Templates\\template.pptx",
  "Kind": "FileNotFoundException",
  "Message": "The specified file was not found"
}
```

---

## 2. Generate Slides

Generate multiple PowerPoint slides from a template and data source.

**Endpoint:** `ws://localhost:{port}/GenerateSlide`

### Request: GenerateSlideCreate

**Type:** `Create`

**Payload:**
```json
{
  "Type": "Create",
  "TemplatePath": "string",
  "SpreadsheetPath": "string",
  "TextConfigs": [
    {
      "Pattern": "string",
      "Columns": ["string"]
    }
  ],
  "ImageConfigs": [
    {
      "ShapeId": 1,
      "Columns": ["string"]
    }
  ],
  "Path": "string",
  "CustomSheet": ["string"] | null
}
```

**Fields:**
- `TemplatePath` (string, required): Path to PowerPoint template file
- `SpreadsheetPath` (string, required): Path to Spreadsheet file
- `TextConfigs` (array, required): Text replacement configurations
  - `Pattern` (string): Text pattern to search for in slides
  - `Columns` (string[]): Column names from spreadsheet to replace with
- `ImageConfigs` (array, required): Image replacement configurations
  - `ShapeId` (number): Shape ID from scan shapes
  - `Columns` (string[]): Column names containing image paths
- `Path` (string, required): Output directory path for generated slides
- `CustomSheet` (string[], optional): Specific sheet names to process (null = all sheets)

**Example:**
```json
{
  "Type": "Create",
  "TemplatePath": "C:\\Templates\\template.pptx",
  "SpreadsheetPath": "C:\\Data\\students.xlsx",
  "TextConfigs": [
    {
      "Pattern": "{{NAME}}",
      "Columns": ["Full Name", "Student ID"]
    },
    {
      "Pattern": "{{CLASS}}",
      "Columns": ["Class"]
    }
  ],
  "ImageConfigs": [
    {
      "ShapeId": 1,
      "Columns": ["Photo Path"]
    }
  ],
  "Path": "C:\\Output",
  "CustomSheet": ["Sheet1", "Sheet2", "Sheet3"]
}
```

### Responses

#### GenerateSlideCreate (Acknowledgment)
```json
{
  "Type": "Create",
  "Path": "C:\\Output",
  "JobIds": {
    "Sheet1": "job_uuid_1",
    "Sheet2": "job_uuid_2",
    "Sheet3": "job_uuid_3"
  }
}
```

**Fields:**
- `Path` (string): Output directory path
- `JobIds` (object): Map of job identifiers to UUIDs
  - Key: Job identifier (sheet name)
  - Value: Unique job UUID

#### GenerateSlideGroupStatus (Progress Update)
```json
{
  "Type": "Status",
  "Path": "C:\\Output",
  "Percent": 45.5,
  "Message": "Im trying to finish this process",
}
```

**Fields:**
- `Path` (string): Output directory path
- `Percent` (number): Completion percentage (0-100)
- `Current` (number): Current item being processed
- `Total` (number): Total number of items to process
- `Message` (string, optional): Current operation description

#### GenerateSlideJobStatus (Individual Job Progress)
```json
{
  "Type": "Status",
  "JobId": "job_uuid_1",
  "Percent": 75.0,
  "Current": 15,
  "Total": 20,
  "Message": "Replacing images"
}
```

**Fields:**
- `JobId` (string): Unique job identifier
- `Percent` (number): Completion percentage (0-100)
- `Current` (number): Current item being processed
- `Total` (number): Total number of items to process
- `Message` (string): Current operation description

#### GenerateSlideGroupFinish (All Jobs Complete)
```json
{
  "Type": "Finish",
  "Path": "C:\\Output",
  "Success": true
}
```

#### GenerateSlideJobFinish (Single Job Complete)
```json
{
  "Type": "Finish",
  "JobId": "job_uuid_1",
  "Success": true
}
```

#### GenerateSlideGroupError
```json
{
  "Type": "Error",
  "Path": "C:\\Output",
  "Kind": "InvalidOperationException",
  "Message": "Template file is corrupted"
}
```

#### GenerateSlideError (Job-specific Error)
```json
{
  "Type": "Error",
  "JobId": "job_uuid_1",
  "Kind": "FileNotFoundException",
  "Message": "Image file not found: C:\\Images\\photo1.jpg"
}
```

### Control Operations

Control job execution during slide generation.

#### Group Control

Control all jobs in a generation group.

**Request:**
```json
{
  "Type": "Control",
  "Path": "C:\\Output",
  "State": "Pause" | "Resume" | "Stop"
}
```

**Fields:**
- `Path` (string, required): Output path identifying the job group
- `State` (string, optional): Control action
  - `Pause`: Pause all jobs
  - `Resume`: Resume paused jobs
  - `Stop`: Cancel all jobs

**Response:**
```json
{
  "Type": "Control",
  "Path": "C:\\Output",
  "State": "Pause"
}
```

#### Job Control

Control a specific job.

**Request:**
```json
{
  "Type": "Control",
  "JobId": "job_uuid_1",
  "State": "Pause" | "Resume" | "Stop"
}
```

**Fields:**
- `JobId` (string, required): Unique job identifier
- `State` (string, optional): Control action

**Response:**
```json
{
  "Type": "Control",
  "JobId": "job_uuid_1",
  "State": "Resume"
}
```

### Status Queries

Query current status of ongoing operations.

#### Group Status

Query status of all jobs in a group.

**Request:**
```json
{
  "Type": "Status",
  "Path": "C:\\Output"
}
```

**Response:**
```json
{
  "Type": "Status",
  "Path": "C:\\Output",
  "Percent": 67.5,
  "Current": 35,
  "Total": 50,
  "Message": "35 of 50 slides completed"
}
```

#### Job Status

Query status of a specific job.

**Request:**
```json
{
  "Type": "Status",
  "JobId": "job_uuid_1"
}
```

**Response:**
```json
{
  "Type": "Status",
  "JobId": "job_uuid_1",
  "Percent": 75.0,
  "Current": 15,
  "Total": 20,
  "Message": "Replacing images"
}
```

---

# Error Handling

All errors follow this format:

```json
{
  "Type": "Error",
  "Kind": "ExceptionTypeName",
  "Message": "Detailed error message"
}
```

### Common System Error Types

**Standard .NET Exceptions:**
- `FileNotFoundException`: File not found at specified path
- `InvalidOperationException`: Invalid operation or state
- `ArgumentException`: Invalid arguments provided
- `ArgumentNullException`: Required argument is null or missing
- `UnauthorizedAccessException`: Permission denied to access file/directory
- `IOException`: General file I/O error

### Application-Specific Error Types

**Presentation-Related Exceptions:**

#### `NoPresentationPartException`
The PowerPoint file is missing the presentation part (corrupted file structure).

**Example:**
```json
{
  "Type": "Error",
  "Kind": "NoPresentationPartException",
  "Message": "The file 'C:\\Templates\\template.pptx' has no presentation part."
}
```

**Cause:** File is corrupted or not a valid PowerPoint file.

#### `NoSlideIdListException`
The PowerPoint file has no slide ID list (empty or corrupted presentation).

**Example:**
```json
{
  "Type": "Error",
  "Kind": "NoSlideIdListException",
  "Message": "The file 'C:\\Templates\\template.pptx' has no Slide ID List."
}
```

**Cause:** Presentation structure is corrupted or contains no slides.

#### `NoRelationshipIdSlideException`
Missing relationship ID for a specific slide in the presentation.

**Example:**
```json
{
  "Type": "Error",
  "Kind": "NoRelationshipIdSlideException",
  "Message": "The file 'C:\\Templates\\template.pptx' has no relationship ID for slide 1."
}
```

**Cause:** Corrupted slide relationship in the presentation XML structure.

#### `NotOnlySlidePresentationException`
The presentation contains elements other than slides (not supported).

**Example:**
```json
{
  "Type": "Error",
  "Kind": "NotOnlySlidePresentationException",
  "Message": "The file 'C:\\Templates\\template.pptx' is not a presentation with only slides. (Has 3 slides)"
}
```

**Cause:** Presentation contains unsupported elements like master slides, notes, or custom layouts.

#### `NoImageInShapeException`
A shape does not contain an image or the image reference is missing.

**Example:**
```json
{
  "Type": "Error",
  "Kind": "NoImageInShapeException",
  "Message": "The provided shape (ID: 5) does not contain Blip (Image) element."
}
```

**Cause:** 
- Shape is not an image/picture shape
- Image reference is missing or corrupted
- Shape does not have embedded image data

**Service-Related Exceptions:**

#### `PresentationNotOpenedException`
Attempted to perform operation on a presentation that is not currently open.

**Example:**
```json
{
  "Type": "Error",
  "Kind": "PresentationNotOpenedException",
  "Message": "The presentation at the specified filepath is not open: C:\\Templates\\template.pptx"
}
```

**Cause:** Tried to access or modify a presentation file that hasn't been loaded.

#### `NotEnoughArgumentException`
Required arguments are missing from the request.

**Example:**
```json
{
  "Type": "Error",
  "Kind": "NotEnoughArgumentException",
  "Message": "Not enough arguments provided. Need provide these argument(s): TemplatePath, SpreadsheetPath"
}
```

**Cause:** Request is missing required fields.

### Error Context

Path-based operations also include the `Path` field:
```json
{
  "Type": "Error",
  "Path": "C:\\Output",
  "Kind": "InvalidOperationException",
  "Message": "Template file is corrupted"
}
```

Job-based operations also include the `JobId` field:
```json
{
  "Type": "Error",
  "JobId": "job_uuid_1",
  "Kind": "FileNotFoundException",
  "Message": "Image file not found: C:\\Images\\photo1.jpg"
}
```

### Error Handling Best Practices

1. **Check `Kind` field** to determine error type and appropriate recovery action
2. **Read `Message` field** for specific details about what went wrong
3. **Use `Path` or `JobId`** to identify which operation failed
4. **File validation**: Verify file exists and is accessible before sending request
5. **Presentation validation**: Ensure PowerPoint files are valid and not corrupted
6. **Shape validation**: When using scan shapes, verify shape IDs exist before using in image configs
7. **Path validation**: Use absolute paths and ensure directories exist

---

# Workflow Examples

## Example 1: Scan Template Shapes

**Objective:** Extract all shapes from a PowerPoint template to identify shape IDs for image replacement.

**Connect to:** `ws://localhost:5000/ScanShapes`

**Steps:**

1. **Send scan request:**
```json
{
  "Type": "Create",
  "FilePath": "C:\\Templates\\template.pptx"
}
```

2. **Receive acknowledgment:**
```json
{
  "Type": "Create",
  "Path": "C:\\Templates\\template.pptx"
}
```

3. **Receive results:**
```json
{
  "Type": "Finish",
  "Path": "C:\\Templates\\template.pptx",
  "Success": true,
  "Shapes": [
    {
      "Id": 1,
      "Name": "Picture 1",
      "Data": "base64_image_data..."
    },
    {
      "Id": 2,
      "Name": "Picture 2",
      "Data": "base64_image_data..."
    }
  ]
}
```

---

## Example 2: Generate Slides with Progress Tracking

**Objective:** Generate multiple slides from template and data, monitoring overall progress.

**Connect to:** `ws://localhost:5000/GenerateSlide`

**Steps:**

1. **Create generation job:**
```json
{
  "Type": "Create",
  "TemplatePath": "C:\\Templates\\template.pptx",
  "SpreadsheetPath": "C:\\Data\\students.xlsx",
  "TextConfigs": [
    {
      "Pattern": "{{NAME}}",
      "Columns": ["Full Name"]
    },
    {
      "Pattern": "{{CLASS}}",
      "Columns": ["Class"]
    }
  ],
  "ImageConfigs": [
    {
      "ShapeId": 1,
      "Columns": ["Photo Path"]
    }
  ],
  "Path": "C:\\Output",
  "CustomSheet": null
}
```

2. **Receive job IDs:**
```json
{
  "Type": "Create",
  "Path": "C:\\Output",
  "JobIds": {
    "Sheet1": "uuid-abc-123",
    "Sheet2": "uuid-def-456",
    "Sheet3": "uuid-ghi-789"
  }
}
```

3. **Receive progress updates (automatic):**
```json
{
  "Type": "Status",
  "Path": "C:\\Output",
  "Percent": 33.3,
  "Message": "..."
}
```

4. **Optionally pause execution:**
```json
{
  "Type": "Control",
  "Path": "C:\\Output",
  "State": "Pause"
}
```

**Receive confirmation:**
```json
{
  "Type": "Control",
  "Path": "C:\\Output",
  "State": "Pause"
}
```

5. **Resume execution:**
```json
{
  "Type": "Control",
  "Path": "C:\\Output",
  "State": "Resume"
}
```

6. **Receive completion:**
```json
{
  "Type": "Finish",
  "Path": "C:\\Output",
  "Success": true
}
```

---

## Example 3: Monitor Individual Job

**Objective:** Track progress of a specific job within a generation group.

**Connect to:** `ws://localhost:5000/GenerateSlide`

**Steps:**

1. **After receiving job IDs, query specific job status:**
```json
{
  "Type": "Status",
  "JobId": "uuid-abc-123"
}
```

2. **Receive job-specific updates:**
```json
{
  "Type": "Status",
  "JobId": "uuid-abc-123",
  "Percent": 50.0,
  "Current": 1,
  "Total": 2,
  "Message": "Replacing text patterns"
}
```

3. **Control specific job if needed:**
```json
{
  "Type": "Control",
  "JobId": "uuid-abc-123",
  "State": "Stop"
}
```

4. **Receive job completion or cancellation:**
```json
{
  "Type": "Finish",
  "JobId": "uuid-abc-123",
  "Success": false
}
```

---

# Best Practices

## Connection Management
1. **Maintain persistent WebSocket connection** for real-time updates
2. **Implement reconnection logic** with exponential backoff
3. **Handle connection errors gracefully** with user feedback

## Message Handling
1. **Validate JSON** before parsing to prevent errors
2. **Check message Type** first to route to appropriate handlers
3. **Store job IDs** from create response for tracking individual jobs
4. **Use Path or JobId** to correlate responses with requests

## File Management
1. **Use absolute paths** for all file and directory references
2. **Verify file exists** and is accessible before sending request
3. **Ensure PowerPoint files are valid** and not corrupted
4. **Check write permissions** on output directories

## Error Recovery
1. **Check `Kind` field** to determine error type and recovery action
2. **Read `Message` field** for specific error details
3. **Implement retry logic** for transient errors (I/O, network)
4. **Log errors** with context (Path/JobId) for debugging

## Performance
1. **Don't poll for status** - rely on automatic status updates
2. **Use CustomSheet** parameter to process only required sheets
3. **Consider file sizes** when processing large spreadsheets
4. **Monitor memory usage** during batch operations

## Validation
1. **Scan shapes first** to get valid shape IDs for image configs
2. **Verify shape IDs exist** before using in image replacement
3. **Validate column names** exist in spreadsheet
4. **Check pattern syntax** in text configs matches template content

## Concurrency
1. **Multiple operations** can run concurrently with different paths
2. **Use unique output paths** for each generation group
3. **Track multiple connections** if running parallel operations
4. **Coordinate job IDs** across different generation groups
