namespace TaoSlideTotNghiep.DTOs;

#region Base Responses

/// <summary>
/// Base response class.
/// </summary>
public abstract class BaseResponse
{
    public bool Success { get; init; }
    public RequestType RequestType { get; init; }
}

/// <summary>
/// Error response.
/// </summary>
public class ErrorResponse : BaseResponse
{
    public string Kind { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? StackTrace { get; init; }

    public ErrorResponse()
    {
        Success = false;
    }

    public ErrorResponse(Exception exception, RequestType requestType = RequestType.Image)
    {
        Success = false;
        RequestType = requestType;
        Kind = exception.GetType().Name;
        Message = exception.Message;
        StackTrace = exception.StackTrace;
    }
}

#endregion

#region Image Responses

/// <summary>
/// Base image response.
/// </summary>
public abstract class ImageResponseBase : BaseResponse
{
    public string FilePath { get; init; } = string.Empty;
    
    protected ImageResponseBase()
    {
        Success = true;
        RequestType = RequestType.Image;
    }
}

/// <summary>
/// Response for crop image request.
/// </summary>
public class CropImageResponse : ImageResponseBase
{
    public ImageRequestType Type { get; init; } = ImageRequestType.Crop;
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}

#endregion

#region Sheet Responses

/// <summary>
/// Base sheet response.
/// </summary>
public abstract class SheetResponseBase : BaseResponse
{
    public string SheetPath { get; init; } = string.Empty;
    
    protected SheetResponseBase()
    {
        Success = true;
        RequestType = RequestType.Sheet;
    }
}

/// <summary>
/// Response for opening a sheet file.
/// </summary>
public class OpenFileSheetResponse : SheetResponseBase
{
    public SheetRequestType Type { get; init; } = SheetRequestType.OpenFile;
}

/// <summary>
/// Response for closing a sheet file.
/// </summary>
public class CloseFileSheetResponse : SheetResponseBase
{
    public SheetRequestType Type { get; init; } = SheetRequestType.CloseFile;
}

/// <summary>
/// Response containing table information.
/// </summary>
public class GetTablesSheetResponse : SheetResponseBase
{
    public SheetRequestType Type { get; init; } = SheetRequestType.GetTables;
    public Dictionary<string, int> Tables { get; init; } = new();
}

/// <summary>
/// Response containing table headers.
/// </summary>
public class GetTableHeadersSheetResponse : SheetResponseBase
{
    public SheetRequestType Type { get; init; } = SheetRequestType.GetHeaders;
    public string TableName { get; init; } = string.Empty;
    public List<string?> Headers { get; init; } = [];
}

/// <summary>
/// Response containing a row of data.
/// </summary>
public class GetTableRowSheetResponse : SheetResponseBase
{
    public SheetRequestType Type { get; init; } = SheetRequestType.GetRow;
    public string TableName { get; init; } = string.Empty;
    public int RowNumber { get; init; }
    public Dictionary<string, object?> RowData { get; init; } = new();
}

#endregion

#region Download Responses

/// <summary>
/// Base download response.
/// </summary>
public abstract class DownloadResponseBase : BaseResponse
{
    public string Url { get; init; } = string.Empty;
    public string SavePath { get; init; } = string.Empty;
    
    protected DownloadResponseBase()
    {
        Success = true;
        RequestType = RequestType.Download;
    }
}

/// <summary>
/// Download progress update.
/// </summary>
public class DownloadProgressResponse : DownloadResponseBase
{
    public double Progress { get; init; }
    public long DownloadedBytes { get; init; }
    public long TotalBytes { get; init; }
    public string Status { get; init; } = string.Empty;
}

#endregion
