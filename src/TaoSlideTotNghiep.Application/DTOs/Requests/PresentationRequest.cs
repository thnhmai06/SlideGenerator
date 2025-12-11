using System.Text.Json.Serialization;

namespace TaoSlideTotNghiep.Application.DTOs.Requests;

#region Enums

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PresentationRequestType
{
    ScanShapes,

    // Group
    GroupCreate,
    GroupStatus,
    GroupControl,

    // Job
    JobControl,
    JobStatus
}

#endregion

#region Records

/// <summary>
/// Base presentation request.
/// </summary>
public abstract record PresentationRequest(PresentationRequestType Type) : Request(RequestType.Presentation);

#region ScanShapes

public record ScanShapesRequest(string FilePath)
    : PresentationRequest(PresentationRequestType.ScanShapes),
        IFilePathBased;

#endregion

#region GenerateSlide

#region Configs

public abstract record GenerateSlideConfig(params string[] Columns);

public record ImageConfig(uint ShapeId, params string[] Columns) : GenerateSlideConfig(Columns);

public record TextConfig(string Pattern, params string[] Columns) : GenerateSlideConfig(Columns);

#endregion

#region Group

public record GenerateSlideGroupCreateRequest(
    string TemplatePath,
    string SpreadsheetPath,
    TextConfig[] TextConfigs,
    ImageConfig[] ImageConfigs,
    string FilePath, // Save Path
    string[]? CustomSheet) : PresentationRequest(PresentationRequestType.GroupCreate),
    IFilePathBased;

public record GenerateSlideGroupStatusRequest(string FilePath)
    : PresentationRequest(PresentationRequestType.GroupStatus),
        IFilePathBased;

public record GenerateSlideGroupControlRequest(string FilePath)
    : PresentationRequest(PresentationRequestType.GroupControl),
        IFilePathBased;

#endregion

#region Job

public record GenerateSlideJobControlRequest(string JobId)
    : PresentationRequest(PresentationRequestType.JobControl),
        IJobBased;

#endregion

#endregion

#endregion