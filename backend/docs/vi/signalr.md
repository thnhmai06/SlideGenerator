# SignalR API

## Muc luc

1. [Endpoint](#endpoint)
2. [Mo hinh requestresponse](#mo-hinh-requestresponse)
3. [Thong diep Slide hub](#thong-diep-slide-hub)
4. [Dang ky nhan su kien](#dang-ky-nhan-su-kien)
5. [Thong bao realtime](#thong-bao-realtime)
6. [Vi du](#vi-du)

## Endpoint

Backend mo cac SignalR hub:

- `/hubs/slide`
- `/hubs/sheet`
- `/hubs/config`

## Mo hinh requestresponse

Client gui JSON vao `ProcessRequest`.

- Message co truong `type`.
- Hub tra phan hoi qua `ReceiveResponse`.

## Thong diep Slide hub

Xem code: `SlideGenerator.Presentation/Hubs/SlideHub.cs`.

Type pho bien:

- `ScanShapes`
- `GroupCreate`
- `GroupStatus`
- `GroupControl`
- `JobStatus`
- `JobControl`
- `GlobalControl`
- `GetAllGroups`

## Dang ky nhan su kien

Client subscribe de nhan realtime:

- `SubscribeGroup(groupJobId)`
- `SubscribeSheet(sheetJobId)`

## Thong bao realtime

Thong bao chi gui cho subscriber va qua `ReceiveNotification`.

Loai chinh:

- Tien do job
- Trang thai job
- Loi job
- Tien do group
- Trang thai group

Xem them:

- [Job system](../en/job-system.md)
- [Architecture](../en/architecture.md)

## Vi du

Tat ca request gui vao `ProcessRequest` va nhan response tu `ReceiveResponse`.

Scan template (shape + placeholder):

```json
{
  "type": "ScanTemplate",
  "filePath": "C:\\slides\\template.pptx"
}
```

Tao group job:

```json
{
  "type": "GroupCreate",
  "templatePath": "C:\\slides\\template.pptx",
  "spreadsheetPath": "C:\\data\\workbook.xlsx",
  "outputPath": "C:\\output",
  "textConfigs": [
    {
      "pattern": "FullName",
      "columns": ["FullName"]
    }
  ],
  "imageConfigs": [
    {
      "shapeId": 4,
      "columns": ["PhotoUrl"],
      "roiType": "Attention",
      "cropType": "Fit"
    }
  ],
  "sheetNames": ["Sheet1"]
}
```

Tam dung/choi lai group:

```json
{
  "type": "GroupControl",
  "groupId": "GROUP_ID",
  "action": "Pause"
}
```

Lay log job:

```json
{
  "type": "JobLogs",
  "jobId": "SHEET_JOB_ID"
}
```

Subscribe realtime:

```
SubscribeGroup("GROUP_ID")
SubscribeSheet("SHEET_JOB_ID")
```
