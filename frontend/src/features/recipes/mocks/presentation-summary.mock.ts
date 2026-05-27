import type { PresentationSummary } from "@/types/recipe";

export const mockPresentationSummary: PresentationSummary = {
  presentationPath: "C:\\Templates\\GiayKhenHocSinh.potx",
  slides: [
    {
      index: 1,
      shapes: [
        { name: "TenHocSinh", type: "TextPlaceholder", rect: { x: 20, y: 30, w: 60, h: 12 } },
        { name: "NamHoc", type: "TextPlaceholder", rect: { x: 35, y: 48, w: 30, h: 8 } },
        { name: "AnhHocSinh", type: "ImagePlaceholder", rect: { x: 70, y: 15, w: 20, h: 35 } },
        { name: "XepLoai", type: "TextPlaceholder", rect: { x: 25, y: 65, w: 50, h: 8 } },
      ],
    },
    {
      index: 2,
      shapes: [
        { name: "TenHocSinh", type: "TextPlaceholder", rect: { x: 15, y: 20, w: 70, h: 15 } },
        { name: "DiemTrungBinh", type: "TextPlaceholder", rect: { x: 30, y: 45, w: 40, h: 10 } },
        { name: "AnhHocSinh", type: "ImagePlaceholder", rect: { x: 68, y: 10, w: 24, h: 40 } },
      ],
    },
    {
      index: 3,
      shapes: [
        { name: "LogoTruong", type: "Picture", rect: { x: 5, y: 5, w: 15, h: 15 } },
        { name: "TenTruong", type: "TextPlaceholder", rect: { x: 25, y: 10, w: 50, h: 10 } },
        { name: "AnhToaNha", type: "ImagePlaceholder", rect: { x: 10, y: 30, w: 80, h: 50 } },
      ],
    },
    {
      index: 4,
      shapes: [
        { name: "TieuDe", type: "TextPlaceholder", rect: { x: 10, y: 5, w: 80, h: 15 } },
        { name: "NoiDung", type: "TextPlaceholder", rect: { x: 10, y: 25, w: 80, h: 60 } },
      ],
    },
    {
      index: 5,
      shapes: [
        { name: "TenHocSinh", type: "TextPlaceholder", rect: { x: 20, y: 30, w: 60, h: 12 } },
        { name: "Lop", type: "TextPlaceholder", rect: { x: 30, y: 48, w: 40, h: 8 } },
        { name: "AnhCaNhan", type: "ImagePlaceholder", rect: { x: 65, y: 20, w: 25, h: 40 } },
        { name: "ChucVu", type: "TextPlaceholder", rect: { x: 25, y: 65, w: 50, h: 8 } },
      ],
    },
    {
      index: 6,
      shapes: [
        { name: "Header", type: "TextPlaceholder", rect: { x: 5, y: 3, w: 90, h: 12 } },
        { name: "BangDiem", type: "TextPlaceholder", rect: { x: 10, y: 20, w: 80, h: 70 } },
      ],
    },
  ],
};
