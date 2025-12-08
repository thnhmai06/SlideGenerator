# Presentation API Types

TypeScript interfaces and types for WebSocket API communication.

## Structure

```
api/
├── enums.ts              # Enums (ControlState, RequestType, ResponseType)
├── requests/             # Request interfaces
│   ├── ScanShapes.ts
│   ├── GenerateSlide.ts
│   └── index.ts
├── responses/            # Response interfaces
│   ├── ScanShapes.ts
│   ├── GenerateSlide.ts
│   └── index.ts
├── guards.ts             # Type guard functions
├── helpers.ts            # Helper functions for creating requests
└── index.ts              # Main export
```

## Usage

### Import Types

```typescript
import {
  // Enums
  RequestType,
  ResponseType,
  ControlState,
  
  // Request Types
  ScanShapesCreateRequest,
  GenerateSlideCreateRequest,
  GenerateSlideGroupControlRequest,
  
  // Response Types
  ScanShapesFinishResponse,
  GenerateSlideCreateResponse,
  GenerateSlideGroupStatusResponse,
  
  // Type Guards
  isScanShapesFinishResponse,
  isGenerateSlideCreateResponse,
  
  // Helpers
  createScanShapesRequest,
  createGenerateSlideRequest
} from '@/types/api'
```

### Create Requests

```typescript
// Scan shapes
const scanRequest = createScanShapesRequest('C:\\template.pptx')

// Generate slides
const generateRequest = createGenerateSlideRequest(
  'C:\\template.pptx',
  'C:\\data.xlsx',
  'C:\\output',
  [{ Pattern: '{{NAME}}', Columns: ['Name'] }],
  [{ ShapeId: 1, Columns: ['Photo'] }]
)

// Control
const pauseRequest = createGroupControlRequest('C:\\output', ControlState.Pause)
```

### Handle Responses

```typescript
function handleResponse(response: AnyAPIResponse) {
  if (isScanShapesFinishResponse(response)) {
    console.log('Shapes:', response.Shapes)
  } else if (isGenerateSlideCreateResponse(response)) {
    console.log('Job IDs:', response.JobIds)
  } else if (isErrorResponse(response)) {
    console.error(`Error: ${response.Kind} - ${response.Message}`)
  }
}
```

## Type Safety

All types are strictly typed according to the API documentation. Use type guards to safely narrow response types at runtime.
