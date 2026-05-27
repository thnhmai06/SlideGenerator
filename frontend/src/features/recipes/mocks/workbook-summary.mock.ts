import type { WorkbookSummary } from "@/types/recipe";

export const mockWorkbookSummary: WorkbookSummary = {
  filePath: "C:\\Data\\DanhSachHocSinh.xlsx",
  name: "DanhSachHocSinh.xlsx",
  worksheets: [
    {
      identifier: {
        bookPath: "C:\\Data\\DanhSachHocSinh.xlsx",
        sheetName: "Lớp 10A1",
      },
      count: 42,
    },
    {
      identifier: {
        bookPath: "C:\\Data\\DanhSachHocSinh.xlsx",
        sheetName: "Lớp 10A2",
      },
      count: 38,
    },
    {
      identifier: {
        bookPath: "C:\\Data\\DanhSachHocSinh.xlsx",
        sheetName: "Lớp 11B1",
      },
      count: 45,
    },
  ],
};

export const mockWorkbookSummary2: WorkbookSummary = {
  filePath: "C:\\Data\\SanPham.xlsx",
  name: "SanPham.xlsx",
  worksheets: [
    {
      identifier: {
        bookPath: "C:\\Data\\SanPham.xlsx",
        sheetName: "Tháng 1",
      },
      count: 120,
    },
    {
      identifier: {
        bookPath: "C:\\Data\\SanPham.xlsx",
        sheetName: "Tháng 2",
      },
      count: 98,
    },
  ],
};
