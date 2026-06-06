# Graph Editor Design Notes

## Node Types

| NodeType       | Role                                                       |
|----------------|------------------------------------------------------------|
| `Workbook`     | File reference (path, password, separator)                 |
| `Worksheet`    | Child of Workbook. Column selection + row filter + preview |
| `Presentation` | File reference (path, password)                            |
| `Slide`        | Child of Presentation. Preview                             |
| `Map`          | Mapping node. TextInstructions + ImageInstructions         |
| `Comment`      | Free-floating label. Color, opacity, markdown text         |

## Topology

```
WorkbookNode (container)
  └── WorksheetNode  ──edge──> MapNode ──edge──> SlideNode
  └── WorksheetNode  ──edge──> MapNode

PresentationNode (container)
  └── SlideNode
  └── SlideNode
```

## Parent-Child

- Sheet/Slide nằm **bên trong** container của Book/Presentation (Option A — no scroll)
- Parent-child qua `ParentId` field trên child node
- **Không** dùng Edge cho containment — Edge chỉ dùng cho data-flow
- WorkbookNode không chứa list con — query bằng `nodes.Where(n => n.ParentId == id)`

## Edge Semantics

Edge list = **data-flow only**:

- `WorksheetNode.Id → MapNode.Id`
- `MapNode.Id → SlideNode.Id`

Containment (Book→Sheet, Presentation→Slide) **không có** trong Edge list.

## Container Scroll Decision

**No scroll. Container expands.**

Scroll bên trong container + external edges = anti-pattern:

- Edge bị clip hoặc nhảy khi cuộn
- Mất thông tin "edge từ sheet nào"

Container cao là acceptable trade-off. Nếu quá nhiều sheet → user reconsider.

## Sheet/Slide Selection

Graph chỉ chứa những node user **chủ động thêm** — không enumerate toàn bộ file.

```
WorkbookNode (file.xlsx — 50 sheets)
  └── WorksheetNode "Sheet3"   ← dùng
  └── WorksheetNode "Sheet7"   ← dùng
  // Sheet1,2,4,...49 không xuất hiện
```

## MapNode (graph node)

Chỉ chứa: `TextInstructions + ImageInstructions`

Không chứa `Sheets` hoặc `Slide` — thông tin đó nằm trong edges (WorksheetNode→MapNode, MapNode→SlideNode).

## WorkbookNode / WorksheetNode Fields

**WorkbookNode**: `BookPath, BookPassword, Separator`

**WorksheetNode**: `ParentId, SheetName, AllowedColumns, RowFilter`

**PresentationNode**: `PresentationPath, PresentationPassword`

**SlideNode**: `ParentId, SlideIndex`

## Naming Note

Tránh type tên `Graph` trong namespace `...Models.Graph` → `Models.Graph.Graph` gây resolution friction.
Dùng `RecipeGraph`.
