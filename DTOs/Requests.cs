using System.Text.Json.Serialization;

namespace TaoSlideTotNghiep.DTOs;

#region Enums

/// <summary>
/// Types of requests.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestType
{
    Download,
    Image,
    Sheet
}

/// <summary>
/// Types of image requests.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImageRequestType
{
    Crop
}

/// <summary>
/// Types of sheet requests.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SheetRequestType
{
    OpenFile,
    CloseFile,
    GetTables,
    GetHeaders,
    GetRow
}

/// <summary>
/// Crop modes for image processing.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CropMode
{
    Prominent,
    Center
}

#endregion

#region Base Requests

/// <summary>
/// Base request class.
/// </summary>
public abstract class BaseRequest
{
    public RequestType RequestType { get; init; }
}

/// <summary>
/// Base image request.
/// </summary>
public abstract class ImageRequestBase : BaseRequest
{
    public string FilePath { get; init; } = string.Empty;
    
    protected ImageRequestBase()
    {
        RequestType = RequestType.Image;
    }
}

/// <summary>
/// Base sheet request.
/// </summary>
public abstract class SheetRequestBase : BaseRequest
{
    public string SheetPath { get; init; } = string.Empty;
    
    protected SheetRequestBase()
    {
        RequestType = RequestType.Sheet;
    }
}

/// <summary>
/// Base download request.
/// </summary>
public abstract class DownloadRequestBase : BaseRequest
{
    public string Url { get; init; } = string.Empty;
    public string SavePath { get; init; } = string.Empty;
    
    protected DownloadRequestBase()
    {
        RequestType = RequestType.Download;
    }
}

#endregion

#region Image Requests

/// <summary>
/// Request to crop an image.
/// </summary>
public class CropImageRequest : ImageRequestBase
{
    public ImageRequestType Type { get; init; } = ImageRequestType.Crop;
    public int Width { get; init; }
    public int Height { get; init; }
    public CropMode Mode { get; init; } = CropMode.Prominent;
}

#endregion

#region Sheet Requests

/// <summary>
/// Request to open a sheet file.
/// </summary>
public class OpenFileSheetRequest : SheetRequestBase
{
    public SheetRequestType Type { get; init; } = SheetRequestType.OpenFile;
}

/// <summary>
/// Request to close a sheet file.
/// </summary>
public class CloseFileSheetRequest : SheetRequestBase
{
    public SheetRequestType Type { get; init; } = SheetRequestType.CloseFile;
}

/// <summary>
/// Request to get all tables in a sheet.
/// </summary>
public class GetTablesSheetRequest : SheetRequestBase
{
    public SheetRequestType Type { get; init; } = SheetRequestType.GetTables;
}

/// <summary>
/// Request to get table headers.
/// </summary>
public class GetTableHeadersSheetRequest : SheetRequestBase
{
    public SheetRequestType Type { get; init; } = SheetRequestType.GetHeaders;
    public string TableName { get; init; } = string.Empty;
}

/// <summary>
/// Request to get a specific row from a table.
/// </summary>
public class GetTableRowSheetRequest : SheetRequestBase
{
    public SheetRequestType Type { get; init; } = SheetRequestType.GetRow;
    public string TableName { get; init; } = string.Empty;
    public int RowNumber { get; init; }
}

#endregion
