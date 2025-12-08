using TaoSlideTotNghiep.DTOs.Requests;
using TaoSlideTotNghiep.Models.Enum;

namespace TaoSlideTotNghiep.DTOs.Responses;


public record ShapeData(uint Id, string Name, string Image); // Image: Base64

/// <summary>
/// Base presentation response.
/// </summary>
public abstract record PresentationResponse(PresentationRequestType Type, bool Success)
    : Response(RequestType.Presentation, Success);

#region ScanShapes

public record ScanShapesResponse(string FilePath, ShapeData[]? Shapes = null)
    : PresentationResponse(PresentationRequestType.ScanShapes, true), IFilePathBased;

#endregion

#region GenerateSlide

#region Group

public record GenerateSlideGroupCreateResponse(string FilePath, Dictionary<string, string> JobIds)
    : PresentationResponse(PresentationRequestType.GroupCreate, true), IFilePathBased;

public record GenerateSlideGroupStatusResponse(string FilePath, float Percent, ControlState State, string? Message = null)
    : PresentationResponse(PresentationRequestType.GroupStatus, true), IFilePathBased;

public record GenerateSlideGroupControlResponse(string FilePath, ControlState State)
    : PresentationResponse(PresentationRequestType.GroupControl, true), IFilePathBased;

#endregion

#region Job

public record GenerateSlideJobStatusResponse(string JobId, float Percent, uint Current, uint Total, ControlState State, string? Message = null)
    : PresentationResponse(PresentationRequestType.JobStatus, true), IJobBased;

public record GenerateSlideJobControlResponse(string JobId, ControlState State)
    : PresentationResponse(PresentationRequestType.JobControl, true), IJobBased;

#endregion

#endregion
