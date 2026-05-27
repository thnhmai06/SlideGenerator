import type { RecipeEntry } from "@/types/recipe";

// 5 mock recipes with varying states
export const mockRecipes: RecipeEntry[] = [
  {
    id: 1,
    displayName: "Giấy khen học sinh",
    recipe: JSON.stringify({
      nodes: [
        {
          id: "wb-1",
          type: "workbook",
          position: { x: 50, y: 100 },
          data: {
            identifier: { bookPath: "C:\\Data\\DanhSachHocSinh.xlsx" },
            loaded: true,
            selectedSheets: ["Lớp 10A1"],
          },
        },
        {
          id: "ws-1",
          type: "worksheet",
          position: { x: 300, y: 100 },
          data: {
            identifier: {
              bookPath: "C:\\Data\\DanhSachHocSinh.xlsx",
              sheetName: "Lớp 10A1",
            },
            rowCount: 42,
          },
        },
        {
          id: "prs-1",
          type: "presentation",
          position: { x: 50, y: 350 },
          data: {
            identifier: { presentationPath: "C:\\Templates\\GiayKhenHocSinh.potx" },
            loaded: true,
          },
        },
        {
          id: "sl-1",
          type: "slide",
          position: { x: 300, y: 350 },
          data: {
            identifier: {
              presentationPath: "C:\\Templates\\GiayKhenHocSinh.potx",
              slideIndex: 1,
            },
          },
        },
        {
          id: "map-1",
          type: "map",
          position: { x: 550, y: 200 },
          data: {
            mapping: {
              sheets: [
                {
                  bookPath: "C:\\Data\\DanhSachHocSinh.xlsx",
                  sheetName: "Lớp 10A1",
                },
              ],
              slide: {
                presentationPath: "C:\\Templates\\GiayKhenHocSinh.potx",
                slideIndex: 1,
              },
              textInstructions: [
                {
                  placeholders: ["TenHocSinh"],
                  columns: [
                    {
                      bookPath: "C:\\Data\\DanhSachHocSinh.xlsx",
                      sheetName: "Lớp 10A1",
                      columnName: "Họ và tên",
                    },
                  ],
                },
              ],
              imageInstructions: [
                {
                  shapes: [
                    {
                      presentationPath: "C:\\Templates\\GiayKhenHocSinh.potx",
                      slideIndex: 1,
                      shapeName: "AnhHocSinh",
                    },
                  ],
                  columns: [
                    {
                      bookPath: "C:\\Data\\DanhSachHocSinh.xlsx",
                      sheetName: "Lớp 10A1",
                      columnName: "Ảnh",
                    },
                  ],
                  editOptions: {
                    roiOption: {
                      type: "Center",
                      pivot: { x: 0.5, y: 0.5 },
                      useFaceAlignment: true,
                    },
                  },
                },
              ],
            },
          },
        },
        {
          id: "cmt-1",
          type: "comment",
          position: { x: 800, y: 50 },
          data: {
            markdown:
              "## Recipe Giấy khen\nSử dụng template **GiayKhenHocSinh.potx** để in giấy khen cho học sinh.",
            width: 280,
            height: 120,
            theme: "note",
          },
        },
      ],
      edges: [
        { id: "e-wb-ws", source: "wb-1", target: "ws-1", type: "smoothstep" },
        { id: "e-prs-sl", source: "prs-1", target: "sl-1", type: "smoothstep" },
        { id: "e-ws-map", source: "ws-1", target: "map-1", type: "smoothstep" },
        { id: "e-sl-map", source: "sl-1", target: "map-1", type: "smoothstep" },
      ],
    }),
    createdTimestamp: "2026-04-10T08:00:00.000Z",
    updatedTimestamp: "2026-05-15T14:22:00.000Z",
  },
  {
    id: 2,
    displayName: "Thẻ học sinh",
    recipe: JSON.stringify({ nodes: [], edges: [] }),
    createdTimestamp: "2026-04-20T09:30:00.000Z",
    updatedTimestamp: "2026-05-10T11:00:00.000Z",
  },
  {
    id: 3,
    displayName: "Bảng điểm lớp",
    recipe: JSON.stringify({ nodes: [], edges: [] }),
    createdTimestamp: "2026-05-01T07:00:00.000Z",
    updatedTimestamp: "2026-05-18T16:45:00.000Z",
  },
  {
    id: 4,
    displayName: "Giới thiệu sản phẩm",
    recipe: undefined,
    createdTimestamp: "2026-05-12T13:00:00.000Z",
    updatedTimestamp: "2026-05-12T13:00:00.000Z",
  },
  {
    id: 5,
    displayName: "Báo cáo tháng",
    recipe: JSON.stringify({ nodes: [], edges: [] }),
    createdTimestamp: "2026-05-16T10:00:00.000Z",
    updatedTimestamp: "2026-05-19T09:15:00.000Z",
  },
];
