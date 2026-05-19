/* ═══════════════════════════════════════════════════════════
   SlideGenerator — Studio mock data + helpers
   Dùng cho Studio UI Kit
═══════════════════════════════════════════════════════════ */

/* ─── ICONS (Hugeicons-style stroke 1.5) ─── */
const ICON = {
  /* Brand / Tabs */
  recipe:   `<path d="M18 13C20.2091 13 22 11.2091 22 9C22 6.79086 20.2091 5 18 5C17.1767 5 16.4115 5.24874 15.7754 5.67518M6 13C3.79086 13 2 11.2091 2 9C2 6.79086 3.79086 5 6 5C6.82332 5 7.58854 5.24874 8.22461 5.67518M15.7754 5.67518C15.2287 4.11714 13.7448 3 12 3C10.2552 3 8.77132 4.11714 8.22461 5.67518M15.7754 5.67518C15.9209 6.08981 16 6.53566 16 7C16 7.3453 15.9562 7.68038 15.874 8M9.46487 7C9.15785 6.46925 8.73238 6.0156 8.22461 5.67518"/><path d="M6 17.5C7.59905 16.8776 9.69952 16.5 12 16.5C14.3005 16.5 16.401 16.8776 18 17.5"/><path d="M5 21C6.86556 20.3776 9.3161 20 12 20C14.6839 20 17.1344 20.3776 19 21"/><path d="M18 12V20M6 12V20"/>`,
  studio:   `<polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"/>`,
  configure:`<path d="M12 2v3M12 19v3M4.22 4.22l2.12 2.12M17.66 17.66l2.12 2.12M2 12h3M19 12h3M4.22 19.78l2.12-2.12M17.66 6.34l2.12-2.12"/><circle cx="12" cy="12" r="4"/>`,
  running:  `<polygon points="6 4 18 12 6 20"/>`,
  result:   `<path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/>`,

  /* Hierarchy */
  workflow: `<rect x="3" y="3" width="18" height="18" rx="3"/><path d="M9 3v18M3 9h18"/>`,
  workbook: `<rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18M9 3v18"/><path d="M14 13l2 2 4-4"/>`,
  worksheet:`<rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18M3 15h18M9 3v18M15 3v18"/>`,
  row:      `<rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18M3 15h18"/>`,

  /* Actions */
  play:     `<polygon points="6 4 18 12 6 20"/>`,
  pause:    `<rect x="6" y="4" width="4" height="16" rx="1"/><rect x="14" y="4" width="4" height="16" rx="1"/>`,
  stop:     `<rect x="6" y="6" width="12" height="12" rx="2"/>`,
  create:   `<path d="M12 2 9 9 2 12l7 3 3 7 3-7 7-3-7-3-3-7z"/>`,
  folder:   `<path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>`,
  folderOpen:`<path d="M6 14l2-6h13l-2 6"/><path d="M2 5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2v3"/>`,
  external: `<path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/><polyline points="15 3 21 3 21 9"/><line x1="10" y1="14" x2="21" y2="3"/>`,
  edit:     `<path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="m18.5 2.5 3 3L12 15l-4 1 1-4 9.5-9.5z"/>`,
  chevron:  `<polyline points="9 18 15 12 9 6"/>`,
  chevronDown:`<polyline points="6 9 12 15 18 9"/>`,
  copy:     `<rect x="9" y="9" width="13" height="13" rx="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>`,
  download: `<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/>`,
  filePpt:  `<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><path d="M9 13h3a1.5 1.5 0 0 1 0 3H9v2"/>`,
  cog:      `<circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"/>`,
  info:     `<circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>`,
  warning:  `<path d="m10.29 3.86-8.5 14.69A1 1 0 0 0 2.66 20h17.68a1 1 0 0 0 .87-1.5l-8.5-14.69a1 1 0 0 0-1.72 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/>`,
  sun:      `<circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41"/>`,
  moon:     `<path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>`,
  monitor:  `<rect x="2" y="3" width="20" height="14" rx="2"/><line x1="8" y1="21" x2="16" y2="21"/><line x1="12" y1="17" x2="12" y2="21"/>`,
  refresh:  `<polyline points="23 4 23 10 17 10"/><polyline points="1 20 1 14 7 14"/><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"/>`,
};

function svg(name, size = 16) {
  const path = ICON[name];
  if (!path) return '';
  const strokeWidth = name === 'recipe' ? '1.5' : '1.7';
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 24 24" color="currentColor" fill="none" stroke="currentColor" stroke-width="${strokeWidth}" stroke-linecap="round" stroke-linejoin="round">${path}</svg>`;
}

/* ─── MOCK DATA ─── */
const RECIPES = [
  { id: 'rec_revenue',  name: 'Báo cáo doanh thu tháng',  desc: '12 layouts · Cập nhật 14/05' },
  { id: 'rec_vip',      name: 'Slide khách hàng VIP',     desc: '8 layouts · Cập nhật 02/05' },
  { id: 'rec_weekly',   name: 'Họp tuần — Tổng kết',      desc: '6 layouts · Cập nhật 18/05' },
  { id: 'rec_catalog',  name: 'Catalog sản phẩm 2026',    desc: '24 layouts · Cập nhật 10/05' },
  { id: 'rec_pitch',    name: 'Pitch Deck Investor',      desc: '14 layouts · Cập nhật 28/04' },
];

const EXTENSIONS = [
  { id: 'pptx', name: '.pptx', desc: 'Standard PowerPoint Presentation' },
  { id: 'potx', name: '.potx', desc: 'PowerPoint Template' },
  { id: 'ppsx', name: '.ppsx', desc: 'PowerPoint Slideshow' },
];

/* Workflow tree — used by Running & Result panes */
function makeTree() {
  const ts = (base, off) => {
    const d = new Date(Date.now() - off * 1000);
    return d.toTimeString().slice(0, 8);
  };

  /* Helper: generate N rows for a worksheet */
  const rows = (n, prefix, donePct) => {
    return Array.from({ length: n }, (_, i) => {
      const idx = i + 1;
      const isDone   = i < Math.floor(n * donePct);
      const isActive = i === Math.floor(n * donePct);
      const isErr    = (i === 2 && prefix === 'KH-Q1');
      const isWarn   = (i === 4 && prefix === 'SP-A');
      let status = 'pending';
      if (isErr)        status = 'error';
      else if (isWarn)  status = 'done';
      else if (isDone)  status = 'done';
      else if (isActive) status = 'running';
      return {
        idx,
        desc: `${prefix} — Hàng ${String(idx).padStart(3, '0')}`,
        status,
        logs: status === 'pending' ? [] : [
          { t: ts(0, idx * 4), l: 'DEBUG',   m: `Đọc dữ liệu hàng ${idx} từ sheet…` },
          { t: ts(0, idx * 4 - 1), l: 'INFO', m: `Tìm thấy 3 trường giá trị, 1 trường hình ảnh.` },
          ...(isErr ? [
            { t: ts(0, idx * 4 - 2), l: 'WARNING', m: 'Liên kết hình ảnh trả về 404.' },
            { t: ts(0, idx * 4 - 3), l: 'ERROR',   m: 'Không thể tải ảnh từ https://cdn.example.com/img-404.png — đã bỏ qua.' },
          ] : isWarn ? [
            { t: ts(0, idx * 4 - 2), l: 'WARNING', m: 'Trường "Mô tả" rỗng — dùng giá trị mặc định.' },
            { t: ts(0, idx * 4 - 3), l: 'INFO',    m: 'Áp dụng layout L02. Render hoàn tất.' },
          ] : [
            { t: ts(0, idx * 4 - 2), l: 'INFO', m: 'Áp dụng layout. Đang render slide…' },
            { t: ts(0, idx * 4 - 3), l: 'INFO', m: 'Hoàn tất hàng — đã ghi slide.' },
          ]),
        ],
      };
    });
  };

  return {
    workflow: {
      id: 'wf_001',
      name: 'Run #042',
      ts: 'Bắt đầu 09:14, 19/05/2026',
      status: 'running',
      progress: 64,
      stats: { workbook: 3, done: 1 },
      logs: [
        { t: '09:14:02', l: 'INFO',  m: 'Khởi động workflow #042.' },
        { t: '09:14:03', l: 'INFO',  m: 'Đã nạp recipe "Báo cáo doanh thu tháng".' },
        { t: '09:14:05', l: 'INFO',  m: 'Đầu vào: 3 workbook (12 worksheet, ~340 hàng).' },
        { t: '09:14:06', l: 'DEBUG', m: 'Khởi tạo 5 luồng tải ảnh, 5 luồng chỉnh sửa slide.' },
        { t: '09:14:42', l: 'INFO',  m: 'Workbook #1 đã hoàn tất.' },
        { t: '09:16:18', l: 'WARNING', m: 'Phát hiện 1 liên kết ảnh không phản hồi tại Workbook #2.' },
      ],
      workbooks: [
        {
          id: 'wb_1', name: 'Khach-hang-2026-Q1.xlsx', path: 'D:/Data/Reports/Khach-hang-2026-Q1.xlsx',
          status: 'done', progress: 100,
          logs: [
            { t: '09:14:06', l: 'INFO',  m: 'Mở Khach-hang-2026-Q1.xlsx (3 sheet, 145 hàng).' },
            { t: '09:14:38', l: 'INFO',  m: 'Đã xử lý xong 3/3 sheet.' },
            { t: '09:14:42', l: 'INFO',  m: 'Lưu file đầu ra Khach-hang-2026-Q1.pptx (12.4 MB).' },
          ],
          worksheets: [
            { id: 'ws_1_1', name: 'Q1-Tổng hợp', status: 'done', progress: 100, rows: rows(48, 'KH-Q1', 1.0) },
            { id: 'ws_1_2', name: 'Q1-Khách VIP',  status: 'done', progress: 100, rows: rows(22, 'VIP-Q1', 1.0) },
            { id: 'ws_1_3', name: 'Q1-Phân tích',  status: 'done', progress: 100, rows: rows(75, 'PT-Q1', 1.0) },
          ],
        },
        {
          id: 'wb_2', name: 'San-pham-A-2026.xlsx', path: 'D:/Data/Catalog/San-pham-A-2026.xlsx',
          status: 'running', progress: 52,
          logs: [
            { t: '09:14:44', l: 'INFO',  m: 'Mở San-pham-A-2026.xlsx (5 sheet, 130 hàng).' },
            { t: '09:15:30', l: 'INFO',  m: 'Hoàn tất sheet "Tổng quan".' },
            { t: '09:16:18', l: 'WARNING', m: 'Liên kết ảnh tại hàng 12 sheet "Chi tiết" không phản hồi.' },
          ],
          worksheets: [
            { id: 'ws_2_1', name: 'Tổng quan',   status: 'done',    progress: 100, rows: rows(20, 'SP-T', 1.0) },
            { id: 'ws_2_2', name: 'Chi tiết',     status: 'running', progress: 60,  rows: rows(60, 'SP-A', 0.6) },
            { id: 'ws_2_3', name: 'So sánh',      status: 'pending', progress: 0,   rows: rows(30, 'SP-S', 0) },
            { id: 'ws_2_4', name: 'Khuyến nghị',   status: 'pending', progress: 0,   rows: rows(15, 'SP-K', 0) },
            { id: 'ws_2_5', name: 'Phụ lục',      status: 'pending', progress: 0,   rows: rows(5,  'SP-P', 0) },
          ],
        },
        {
          id: 'wb_3', name: 'Bao-cao-tong-hop.xlsx', path: 'D:/Data/Reports/Bao-cao-tong-hop.xlsx',
          status: 'pending', progress: 0,
          logs: [],
          worksheets: [
            { id: 'ws_3_1', name: 'Mục lục',  status: 'pending', progress: 0, rows: rows(8, 'BC-M', 0) },
            { id: 'ws_3_2', name: 'Nội dung', status: 'pending', progress: 0, rows: rows(45, 'BC-N', 0) },
            { id: 'ws_3_3', name: 'Phụ lục',   status: 'pending', progress: 0, rows: rows(22, 'BC-P', 0) },
            { id: 'ws_3_4', name: 'Tham chiếu',status: 'pending', progress: 0, rows: rows(10, 'BC-T', 0) },
          ],
        },
      ],
    },
  };
}

/* Captured config (đại diện cho config tại Configure pane sau khi user bấm Tạo) */
const CAPTURED_CFG = {
  recipe:    'rec_revenue',
  recipeName:'Báo cáo doanh thu tháng',
  recipePath:'C:/Users/admin/Documents/SlideGenerator/Recipes/bao-cao-doanh-thu.recipe',
  extension: 'pptx',
  useLocal:  true,
  localPath: 'D:/Data/Reports/',
  saveDl:    true,
  saveEdit:  false,
  imgPath:   'D:/Slides/Images/',
};

/* Export to window so the Studio UI Kit can use */
Object.assign(window, { ICON, svg, RECIPES, EXTENSIONS, makeTree, CAPTURED_CFG });
