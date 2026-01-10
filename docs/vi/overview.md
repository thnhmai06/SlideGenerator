# Tổng quan Frontend

English version: [English](../en/overview.md)

## Mục tiêu

Frontend là ứng dụng Electron desktop với UI React. Ứng dụng kết nối backend qua SignalR và coi backend là nguồn dữ liệu chuẩn cho trạng thái job.

## Kiến trúc

- UI: React + TypeScript.
- Desktop shell: Electron (main + preload).
- Kết nối backend: các wrapper SignalR trong `src/services`.

## Mô hình runtime

1. Electron tạo cửa sổ và (tùy chọn) khởi chạy backend.
2. UI ket noi `/hubs/job` (alias: `/hubs/task`), `/hubs/sheet`, `/hubs/config`.
3. Du lieu job va notification duoc stream qua SignalR.

## Lưu trữ

- URL backend: `localStorage.slidegen.backend.url`.
- Cache input Create Task: `sessionStorage.slidegen.ui.inputMenu.state`.
- Cache metadata/config group:
  - `sessionStorage.slidegen.group.meta`
  - `sessionStorage.slidegen.group.config`

## Logs

Log nằm ở `frontend/logs/<timestamp>/`:

- `process.log`: Electron main process.
- `renderer.log`: log renderer (DevTools).
- `backend.log`: log backend (khi Electron start).
