/**
 * Backend API Service (SignalR)
 */

import { SignalRHubClient, getBackendBaseUrl } from "./signalrClient";

interface ResponseBase {
  Type?: string;
  type?: string;
  Message?: string;
  message?: string;
  Kind?: string;
  FilePath?: string;
  filePath?: string;
}

export type ControlAction = "Pause" | "Resume" | "Cancel" | "Stop";

export interface ShapeDto {
  Id: number;
  Name: string;
  Data: string;
  Kind?: string;
  IsImage?: boolean;
}

export interface SlideScanShapesSuccess {
  Type: "scanshapes";
  FilePath: string;
  Shapes: ShapeDto[];
}

export interface SlideScanPlaceholdersSuccess {
  Type: "scanplaceholders";
  FilePath: string;
  Placeholders: string[];
}

export interface SlideScanTemplateSuccess {
  Type: "scantemplate";
  FilePath: string;
  Shapes: ShapeDto[];
  Placeholders: string[];
}

export interface SlideGroupCreateSuccess {
  Type: "groupcreate";
  GroupId: string;
  OutputFolder: string;
  JobIds: Record<string, string>;
}

export interface JobStatusInfo {
  JobId: string;
  SheetName: string;
  Status: string;
  CurrentRow: number;
  TotalRows: number;
  Progress: number;
  OutputPath?: string;
  ErrorMessage?: string | null;
  ErrorCount?: number;
}

export interface SlideGroupStatusSuccess {
  Type: "groupstatus";
  GroupId: string;
  Status: string;
  Progress: number;
  Jobs: Record<string, JobStatusInfo>;
  ErrorCount?: number;
}

export interface SlideGroupRemoveSuccess {
  Type: "groupremove";
  GroupId: string;
  Removed: boolean;
}

export interface SlideJobStatusSuccess {
  Type: "jobstatus";
  JobId: string;
  SheetName: string;
  Status: string;
  CurrentRow: number;
  TotalRows: number;
  Progress: number;
  OutputPath?: string;
  ErrorMessage?: string | null;
  ErrorCount?: number;
}

export interface SlideJobRemoveSuccess {
  Type: "jobremove";
  JobId: string;
  Removed: boolean;
}

export interface JobLogEntry {
  Level: string;
  Message: string;
  Timestamp: string;
  Data?: Record<string, unknown>;
}

export interface SlideJobLogsSuccess {
  Type: "joblogs";
  JobId: string;
  Logs: JobLogEntry[];
}

export interface GroupSummary {
  GroupId: string;
  WorkbookPath: string;
  OutputFolder?: string;
  Status: string;
  Progress: number;
  SheetCount: number;
  CompletedSheets: number;
  ErrorCount?: number;
}

export interface SlideGlobalGetGroupsSuccess {
  Type: "getallgroups";
  Groups: GroupSummary[];
}

export interface ConfigGetSuccess {
  Type: "get";
  Server: {
    Host: string;
    Port: number;
    Debug: boolean;
  };
  Download: {
    MaxChunks: number;
    LimitBytesPerSecond: number;
    SaveFolder: string;
    Retry: {
      Timeout: number;
      MaxRetries: number;
    };
  };
  Job: {
    MaxConcurrentJobs: number;
  };
  Image: {
    Face: {
      Confidence: number;
      PaddingTop: number;
      PaddingBottom: number;
      PaddingLeft: number;
      PaddingRight: number;
      UnionAll: boolean;
    };
    Saliency: {
      PaddingTop: number;
      PaddingBottom: number;
      PaddingLeft: number;
      PaddingRight: number;
    };
  };
}

export interface ConfigUpdateSuccess {
  Type: "update";
  Success: boolean;
  Message: string;
}

export interface ConfigReloadSuccess {
  Type: "reload";
  Success: boolean;
  Message: string;
}

export interface ConfigResetSuccess {
  Type: "reset";
  Success: boolean;
  Message: string;
}

export interface SlideTextConfig {
  Pattern: string;
  Columns: string[];
}

export interface SlideImageConfig {
  ShapeId: number;
  Columns: string[];
  RoiType?: string | null;
  CropType?: string | null;
}

interface OpenBookSheetSuccess {
  Type: "openfile";
  FilePath: string;
}

interface SheetWorkbookCloseSuccess {
  Type: "closefile";
  FilePath: string;
}

interface SheetWorkbookGetSheetInfoSuccess {
  Type: "gettables";
  FilePath: string;
  Sheets: Record<string, number>;
}

interface SheetWorksheetGetHeadersSuccess {
  Type: "getheaders";
  FilePath: string;
  SheetName: string;
  Headers: Array<string | null>;
}

interface SheetWorksheetGetRowSuccess {
  Type: "getrow";
  FilePath: string;
  TableName: string;
  RowNumber: number;
  Row: Record<string, string | null>;
}

export interface SheetWorkbookGetInfoSuccess {
  Type: "getworkbookinfo";
  FilePath: string;
  WorkbookName?: string;
  Sheets: Array<{
    Name: string;
    Headers: Array<string | null>;
    RowCount: number;
  }>;
}

export interface LoadedFile {
  group_id: string;
  file_path: string;
  file_type: string;
  num_sheets: number;
}

export interface SheetInfo {
  sheet_id: string;
  sheet_name: string;
  num_rows: number;
  num_cols: number;
}

export interface LoadFileResponse {
  success: boolean;
  group_id: string;
  file_type: string;
  num_sheets: number;
  sheets: string[];
}

export interface FileListResponse {
  files: LoadedFile[];
}

export interface SheetListResponse {
  sheets: SheetInfo[];
}

export interface ColumnListResponse {
  columns: string[];
}

export interface SheetDetailInfo {
  sheet_id: string;
  sheet_name: string;
  num_rows: number;
  num_cols: number;
  columns: string[];
  start_row: number;
  start_col: number;
}

export interface SheetDataResponse {
  columns: string[];
  data: Record<string, string | null>[];
  num_rows: number;
  offset: number;
  total_rows: number;
}

const sheetHub = new SignalRHubClient("/hubs/sheet");
const slideHub = new SignalRHubClient("/hubs/slide");
const configHub = new SignalRHubClient("/hubs/config");

function getResponseType(response: ResponseBase): string {
  return (response.Type ?? response.type ?? "").toLowerCase();
}

function getCaseInsensitive<
  T extends Record<string, unknown>,
  TValue = unknown,
>(obj: T | null | undefined, key: string): TValue | undefined {
  if (!obj) return undefined;
  if (key in obj) return obj[key] as TValue;
  const lowered = key.toLowerCase();
  for (const [entryKey, value] of Object.entries(obj)) {
    if (entryKey.toLowerCase() === lowered) {
      return value as TValue;
    }
  }
  return undefined;
}

function getResponseErrorMessage(response: ResponseBase): string {
  const message = response.Message ?? response.message ?? "";
  const kind = response.Kind ?? "";
  const filePath = response.FilePath ?? response.filePath ?? "";
  const prefix = filePath ? `[${filePath}] ` : "";
  if (message && kind && !message.includes(kind)) {
    return `${prefix}${kind}: ${message}`;
  }
  return `${prefix}${message || kind || "Backend error"}`;
}

function assertSuccess<T>(response: ResponseBase): T {
  if (getResponseType(response) === "error") {
    throw new Error(getResponseErrorMessage(response));
  }
  return response as T;
}

function normalizeJobStatusInfo(input: Record<string, unknown>): JobStatusInfo {
  return {
    JobId: (getCaseInsensitive<string>(input, "JobId") ?? "") as string,
    SheetName: (getCaseInsensitive<string>(input, "SheetName") ?? "") as string,
    Status: (getCaseInsensitive<string>(input, "Status") ?? "") as string,
    CurrentRow: (getCaseInsensitive<number>(input, "CurrentRow") ??
      0) as number,
    TotalRows: (getCaseInsensitive<number>(input, "TotalRows") ?? 0) as number,
    Progress: (getCaseInsensitive<number>(input, "Progress") ?? 0) as number,
    OutputPath: getCaseInsensitive<string>(input, "OutputPath") ?? undefined,
    ErrorMessage:
      getCaseInsensitive<string | null>(input, "ErrorMessage") ?? undefined,
    ErrorCount: getCaseInsensitive<number>(input, "ErrorCount") ?? undefined,
  };
}

function normalizeShapeDto(input: Record<string, unknown>): ShapeDto {
  return {
    Id: (getCaseInsensitive<number>(input, "Id") ?? 0) as number,
    Name: (getCaseInsensitive<string>(input, "Name") ?? "") as string,
    Data: (getCaseInsensitive<string>(input, "Data") ?? "") as string,
    Kind: getCaseInsensitive<string>(input, "Kind") ?? undefined,
    IsImage: getCaseInsensitive<boolean>(input, "IsImage") ?? undefined,
  };
}

function normalizeGroupSummary(input: Record<string, unknown>): GroupSummary {
  return {
    GroupId: (getCaseInsensitive<string>(input, "GroupId") ?? "") as string,
    WorkbookPath: (getCaseInsensitive<string>(input, "WorkbookPath") ??
      "") as string,
    OutputFolder:
      getCaseInsensitive<string>(input, "OutputFolder") ?? undefined,
    Status: (getCaseInsensitive<string>(input, "Status") ?? "") as string,
    Progress: (getCaseInsensitive<number>(input, "Progress") ?? 0) as number,
    SheetCount: (getCaseInsensitive<number>(input, "SheetCount") ??
      0) as number,
    CompletedSheets: (getCaseInsensitive<number>(input, "CompletedSheets") ??
      0) as number,
    ErrorCount: getCaseInsensitive<number>(input, "ErrorCount") ?? undefined,
  };
}

function normalizeJobLogEntry(input: Record<string, unknown>): JobLogEntry {
  return {
    Level: (getCaseInsensitive<string>(input, "Level") ?? "") as string,
    Message: (getCaseInsensitive<string>(input, "Message") ?? "") as string,
    Timestamp: (getCaseInsensitive<string>(input, "Timestamp") ?? "") as string,
    Data: (getCaseInsensitive<Record<string, unknown>>(input, "Data") ??
      undefined) as Record<string, unknown> | undefined,
  };
}

export async function loadFile(filePath: string): Promise<LoadFileResponse> {
  const open = await sheetHub.sendRequest<ResponseBase>({
    type: "openfile",
    filePath,
  });
  assertSuccess<OpenBookSheetSuccess>(open);

  const info = await sheetHub.sendRequest<ResponseBase>({
    type: "gettables",
    filePath,
  });
  const tables = assertSuccess<SheetWorkbookGetSheetInfoSuccess>(info);
  const sheets = (getCaseInsensitive<Record<string, number>>(
    tables,
    "Sheets",
  ) ?? {}) as Record<string, number>;
  const sheetNames = Object.keys(sheets);

  return {
    success: true,
    group_id: filePath,
    file_type: "sheet",
    num_sheets: sheetNames.length,
    sheets: sheetNames,
  };
}

export async function unloadFile(
  filePath: string,
): Promise<{ success: boolean }> {
  const response = await sheetHub.sendRequest<ResponseBase>({
    type: "closefile",
    filePath,
  });
  assertSuccess<SheetWorkbookCloseSuccess>(response);
  return { success: true };
}

export async function getLoadedFiles(): Promise<FileListResponse> {
  return { files: [] };
}

export async function getSheets(filePath: string): Promise<SheetListResponse> {
  const response = await sheetHub.sendRequest<ResponseBase>({
    type: "gettables",
    filePath,
  });
  const data = assertSuccess<SheetWorkbookGetSheetInfoSuccess>(response);

  const tables = (getCaseInsensitive<Record<string, number>>(data, "Sheets") ??
    {}) as Record<string, number>;
  const sheets = Object.entries(tables).map(([name, rows]) => ({
    sheet_id: name,
    sheet_name: name,
    num_rows: rows,
    num_cols: 0,
  }));

  return { sheets };
}

export async function getColumns(
  filePath: string,
  sheetName: string,
): Promise<ColumnListResponse> {
  const response = await sheetHub.sendRequest<ResponseBase>({
    type: "getheaders",
    filePath,
    sheetName,
  });
  const data = assertSuccess<SheetWorksheetGetHeadersSuccess>(response);
  const headers = (getCaseInsensitive<Array<string | null>>(data, "Headers") ??
    []) as Array<string | null>;
  const columns = headers.filter((header): header is string => Boolean(header));
  return { columns };
}

export async function getSheetInfo(
  filePath: string,
  sheetName: string,
): Promise<SheetDetailInfo> {
  const tableResponse = await sheetHub.sendRequest<ResponseBase>({
    type: "gettables",
    filePath,
  });
  const tables = assertSuccess<SheetWorkbookGetSheetInfoSuccess>(tableResponse);
  const tableMap = (getCaseInsensitive<Record<string, number>>(
    tables,
    "Sheets",
  ) ?? {}) as Record<string, number>;

  const headerResponse = await sheetHub.sendRequest<ResponseBase>({
    type: "getheaders",
    filePath,
    sheetName,
  });
  const headers =
    assertSuccess<SheetWorksheetGetHeadersSuccess>(headerResponse);
  const headerList = (getCaseInsensitive<Array<string | null>>(
    headers,
    "Headers",
  ) ?? []) as Array<string | null>;
  const columns = headerList.filter((header): header is string =>
    Boolean(header),
  );

  return {
    sheet_id: sheetName,
    sheet_name: sheetName,
    num_rows: tableMap[sheetName] ?? 0,
    num_cols: columns.length,
    columns,
    start_row: 1,
    start_col: 1,
  };
}

export async function getSheetData(
  filePath: string,
  sheetName: string,
  offset = 0,
  limit?: number,
): Promise<SheetDataResponse> {
  const info = await getSheetInfo(filePath, sheetName);
  const totalRows = info.num_rows;
  const startRow = offset + 1;
  const endRow = limit ? Math.min(totalRows, offset + limit) : totalRows;

  const data: Record<string, string | null>[] = [];
  for (let rowIndex = startRow; rowIndex <= endRow; rowIndex += 1) {
    const response = await sheetHub.sendRequest<ResponseBase>({
      type: "getrow",
      filePath,
      tableName: sheetName,
      rowNumber: rowIndex,
    });
    const row = assertSuccess<SheetWorksheetGetRowSuccess>(response);
    const rowData = (getCaseInsensitive<Record<string, string | null>>(
      row,
      "Row",
    ) ?? {}) as Record<string, string | null>;
    data.push(rowData);
  }

  return {
    columns: info.columns,
    data,
    num_rows: data.length,
    offset,
    total_rows: totalRows,
  };
}

export async function getSheetRow(
  filePath: string,
  sheetName: string,
  rowIndex: number,
): Promise<{ row_index: number; data: Record<string, string | null> }> {
  const response = await sheetHub.sendRequest<ResponseBase>({
    type: "getrow",
    filePath,
    tableName: sheetName,
    rowNumber: rowIndex,
  });
  const row = assertSuccess<SheetWorksheetGetRowSuccess>(response);
  const rowData = (getCaseInsensitive<Record<string, string | null>>(
    row,
    "Row",
  ) ?? {}) as Record<string, string | null>;
  const rowNumber = (getCaseInsensitive<number>(row, "RowNumber") ??
    rowIndex) as number;
  return { row_index: rowNumber, data: rowData };
}

export async function checkHealth(): Promise<{
  status: string;
  message: string;
}> {
  const baseUrl = getBackendBaseUrl();
  const response = await fetch(`${baseUrl}/health`);

  if (!response.ok) {
    throw new Error("Backend server is not responding");
  }

  const data = (await response.json()) as { IsRunning?: boolean };
  return {
    status: data.IsRunning ? "ok" : "unknown",
    message: data.IsRunning ? "Backend is running" : "Backend status unknown",
  };
}

export async function getAllColumns(filePaths: string[]): Promise<string[]> {
  const allColumns = new Set<string>();

  for (const filePath of filePaths) {
    try {
      const infoResponse = await sheetHub.sendRequest<ResponseBase>({
        type: "getworkbookinfo",
        filePath,
      });
      const info = assertSuccess<SheetWorkbookGetInfoSuccess>(infoResponse);
      const sheets = (getCaseInsensitive<Array<Record<string, unknown>>>(
        info,
        "Sheets",
      ) ?? []) as Array<Record<string, unknown>>;
      sheets.forEach((sheet) => {
        const headers = (getCaseInsensitive<Array<string | null>>(
          sheet,
          "Headers",
        ) ?? []) as Array<string | null>;
        headers
          .filter((header): header is string => Boolean(header))
          .forEach((header) => allColumns.add(header));
      });
    } catch (error) {
      console.error(`Error getting columns for file ${filePath}:`, error);
    }
  }

  return Array.from(allColumns).sort();
}

export async function getWorkbookInfo(
  filePath: string,
): Promise<SheetWorkbookGetInfoSuccess> {
  const response = await sheetHub.sendRequest<ResponseBase>({
    type: "getworkbookinfo",
    filePath,
  });
  const data = assertSuccess<SheetWorkbookGetInfoSuccess>(response);
  const sheets = (getCaseInsensitive<Array<Record<string, unknown>>>(
    data,
    "Sheets",
  ) ?? []) as Array<Record<string, unknown>>;

  return {
    Type: getResponseType(data),
    FilePath: getCaseInsensitive<string>(data, "FilePath") ?? filePath,
    WorkbookName: getCaseInsensitive<string>(data, "WorkbookName") ?? undefined,
    Sheets: sheets.map((sheet) => ({
      Name: (getCaseInsensitive<string>(sheet, "Name") ?? "") as string,
      Headers: (getCaseInsensitive<Array<string | null>>(sheet, "Headers") ??
        []) as Array<string | null>,
      RowCount: (getCaseInsensitive<number>(sheet, "RowCount") ?? 0) as number,
    })),
  };
}

export async function scanShapes(filePath: string): Promise<unknown> {
  const response = await slideHub.sendRequest<ResponseBase>({
    type: "scanshapes",
    filePath,
  });
  const data = assertSuccess<SlideScanShapesSuccess>(response);
  return {
    Type: getResponseType(data),
    FilePath: getCaseInsensitive<string>(data, "FilePath") ?? filePath,
    Shapes: (
      (getCaseInsensitive<Array<Record<string, unknown>>>(data, "Shapes") ??
        []) as Array<Record<string, unknown>>
    ).map((shape) => normalizeShapeDto(shape)),
  } satisfies SlideScanShapesSuccess;
}

export async function scanPlaceholders(filePath: string): Promise<unknown> {
  const response = await slideHub.sendRequest<ResponseBase>({
    type: "scanplaceholders",
    filePath,
  });
  const data = assertSuccess<SlideScanPlaceholdersSuccess>(response);
  return {
    Type: getResponseType(data),
    FilePath: getCaseInsensitive<string>(data, "FilePath") ?? filePath,
    Placeholders: (getCaseInsensitive<string[]>(data, "Placeholders") ??
      []) as string[],
  } satisfies SlideScanPlaceholdersSuccess;
}

export async function scanTemplate(filePath: string): Promise<unknown> {
  const response = await slideHub.sendRequest<ResponseBase>({
    type: "scantemplate",
    filePath,
  });
  const data = assertSuccess<SlideScanTemplateSuccess>(response);
  return {
    Type: getResponseType(data),
    FilePath: getCaseInsensitive<string>(data, "FilePath") ?? filePath,
    Shapes: (
      (getCaseInsensitive<Array<Record<string, unknown>>>(data, "Shapes") ??
        []) as Array<Record<string, unknown>>
    ).map((shape) => normalizeShapeDto(shape)),
    Placeholders: (getCaseInsensitive<string[]>(data, "Placeholders") ??
      []) as string[],
  } satisfies SlideScanTemplateSuccess;
}

export async function createGroup(
  request: Record<string, unknown>,
): Promise<unknown> {
  const response = await slideHub.sendRequest<ResponseBase>({
    type: "groupcreate",
    ...request,
  });
  const data = assertSuccess<SlideGroupCreateSuccess>(response);
  return {
    Type: getResponseType(data),
    GroupId: getCaseInsensitive<string>(data, "GroupId") ?? "",
    OutputFolder: getCaseInsensitive<string>(data, "OutputFolder") ?? "",
    JobIds: (getCaseInsensitive<Record<string, string>>(data, "JobIds") ??
      {}) as Record<string, string>,
  } satisfies SlideGroupCreateSuccess;
}

export async function groupStatus(
  request: Record<string, unknown>,
): Promise<unknown> {
  const response = await slideHub.sendRequest<ResponseBase>({
    type: "groupstatus",
    ...request,
  });
  const data = assertSuccess<SlideGroupStatusSuccess>(response);
  const rawJobs = (getCaseInsensitive<Record<string, Record<string, unknown>>>(
    data,
    "Jobs",
  ) ?? {}) as Record<string, Record<string, unknown>>;
  const normalizedJobs: Record<string, JobStatusInfo> = {};
  Object.entries(rawJobs).forEach(([key, value]) => {
    normalizedJobs[key] = normalizeJobStatusInfo(value);
  });
  return {
    Type: getResponseType(data),
    GroupId: getCaseInsensitive<string>(data, "GroupId") ?? "",
    Status: getCaseInsensitive<string>(data, "Status") ?? "Unknown",
    Progress: getCaseInsensitive<number>(data, "Progress") ?? 0,
    Jobs: normalizedJobs,
    ErrorCount: getCaseInsensitive<number>(data, "ErrorCount") ?? 0,
  } satisfies SlideGroupStatusSuccess;
}

export async function groupControl(
  request: Record<string, unknown>,
): Promise<unknown> {
  const response = await slideHub.sendRequest<ResponseBase>({
    type: "groupcontrol",
    ...request,
  });
  return assertSuccess(response);
}

export async function removeGroup(
  request: Record<string, unknown>,
): Promise<unknown> {
  const response = await slideHub.sendRequest<ResponseBase>({
    type: "groupremove",
    ...request,
  });
  const data = assertSuccess<SlideGroupRemoveSuccess>(response);
  return {
    Type: getResponseType(data),
    GroupId: getCaseInsensitive<string>(data, "GroupId") ?? "",
    Removed: getCaseInsensitive<boolean>(data, "Removed") ?? false,
  } satisfies SlideGroupRemoveSuccess;
}

export async function jobStatus(
  request: Record<string, unknown>,
): Promise<unknown> {
  const response = await slideHub.sendRequest<ResponseBase>({
    type: "jobstatus",
    ...request,
  });
  const data = assertSuccess<SlideJobStatusSuccess>(response);
  return {
    Type: getResponseType(data),
    JobId: getCaseInsensitive<string>(data, "JobId") ?? "",
    SheetName: getCaseInsensitive<string>(data, "SheetName") ?? "",
    Status: getCaseInsensitive<string>(data, "Status") ?? "Unknown",
    CurrentRow: getCaseInsensitive<number>(data, "CurrentRow") ?? 0,
    TotalRows: getCaseInsensitive<number>(data, "TotalRows") ?? 0,
    Progress: getCaseInsensitive<number>(data, "Progress") ?? 0,
    OutputPath: getCaseInsensitive<string>(data, "OutputPath") ?? undefined,
    ErrorMessage: getCaseInsensitive<string | null>(data, "ErrorMessage"),
    ErrorCount: getCaseInsensitive<number>(data, "ErrorCount"),
  } satisfies SlideJobStatusSuccess;
}

export async function jobControl(
  request: Record<string, unknown>,
): Promise<unknown> {
  const response = await slideHub.sendRequest<ResponseBase>({
    type: "jobcontrol",
    ...request,
  });
  return assertSuccess(response);
}

export async function removeJob(
  request: Record<string, unknown>,
): Promise<unknown> {
  const response = await slideHub.sendRequest<ResponseBase>({
    type: "jobremove",
    ...request,
  });
  const data = assertSuccess<SlideJobRemoveSuccess>(response);
  return {
    Type: getResponseType(data),
    JobId: getCaseInsensitive<string>(data, "JobId") ?? "",
    Removed: getCaseInsensitive<boolean>(data, "Removed") ?? false,
  } satisfies SlideJobRemoveSuccess;
}

export async function getJobLogs(
  request: Record<string, unknown>,
): Promise<unknown> {
  const response = await slideHub.sendRequest<ResponseBase>({
    type: "joblogs",
    ...request,
  });
  const data = assertSuccess<SlideJobLogsSuccess>(response);
  const logs = (getCaseInsensitive<Array<Record<string, unknown>>>(
    data,
    "Logs",
  ) ?? []) as Array<Record<string, unknown>>;
  return {
    Type: getResponseType(data),
    JobId: getCaseInsensitive<string>(data, "JobId") ?? "",
    Logs: logs.map((entry) => normalizeJobLogEntry(entry)),
  } satisfies SlideJobLogsSuccess;
}

export async function globalControl(
  request: Record<string, unknown>,
): Promise<unknown> {
  const response = await slideHub.sendRequest<ResponseBase>({
    type: "globalcontrol",
    ...request,
  });
  return assertSuccess(response);
}

export async function getAllGroups(): Promise<unknown> {
  const response = await slideHub.sendRequest<ResponseBase>({
    type: "getallgroups",
  });
  const data = assertSuccess<SlideGlobalGetGroupsSuccess>(response);
  const rawGroups = (getCaseInsensitive<Array<Record<string, unknown>>>(
    data,
    "Groups",
  ) ?? []) as Array<Record<string, unknown>>;
  return {
    Type: getResponseType(data),
    Groups: rawGroups.map((group) => normalizeGroupSummary(group)),
  } satisfies SlideGlobalGetGroupsSuccess;
}

export async function subscribeGroup(groupId: string): Promise<void> {
  await slideHub.invoke("SubscribeGroup", groupId);
}

export async function subscribeSheet(sheetId: string): Promise<void> {
  await slideHub.invoke("SubscribeSheet", sheetId);
}

export function onSlideNotification(
  handler: (payload: unknown) => void,
): () => void {
  return slideHub.onNotification(handler);
}

export async function getConfig(): Promise<unknown> {
  const response = await configHub.sendRequest<ResponseBase>({
    type: "get",
  });
  return assertSuccess<ConfigGetSuccess>(response);
}

export async function updateConfig(
  request: Record<string, unknown>,
): Promise<unknown> {
  const response = await configHub.sendRequest<ResponseBase>({
    type: "update",
    ...request,
  });
  return assertSuccess<ConfigUpdateSuccess>(response);
}

export async function reloadConfig(): Promise<unknown> {
  const response = await configHub.sendRequest<ResponseBase>({
    type: "reload",
  });
  return assertSuccess<ConfigReloadSuccess>(response);
}

export async function resetConfig(): Promise<unknown> {
  const response = await configHub.sendRequest<ResponseBase>({
    type: "reset",
  });
  return assertSuccess<ConfigResetSuccess>(response);
}
