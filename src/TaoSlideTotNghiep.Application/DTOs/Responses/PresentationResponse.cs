using TaoSlideTotNghiep.Application.DTOs.Requests;

namespace TaoSlideTotNghiep.Application.DTOs.Responses;

public record ShapeData(uint Id, string Name, string Image); // Image: Base64

#region Success

public abstract record PresentationSuccess(PresentationRequestType Type)
    : SuccessResponse(RequestType.Presentation),
        IPresentationDto;

#region ScanShapes

public record ScanShapesSuccess(string FilePath, ShapeData[]? Shapes = null)
    : PresentationSuccess(PresentationRequestType.ScanShapes),
        IFilePathBased;

#endregion

#region GenerateSlide

#region Group

public record GenerateSlideGroupCreateSuccess(string FilePath, Dictionary<string, string> JobIds)
    : PresentationSuccess(PresentationRequestType.GroupCreate),
        IFilePathBased;

public record GenerateSlideGroupStatusSuccess(string FilePath, float Percent, string? Message = null)
    : PresentationSuccess(PresentationRequestType.GroupStatus),
        IFilePathBased;

public record GenerateSlideGroupControlSuccess(string FilePath)
    : PresentationSuccess(PresentationRequestType.GroupControl),
        IFilePathBased;

#endregion

#region Job

public record GenerateSlideJobStatusSuccess(
    string JobId,
    float Percent,
    uint Current,
    uint Total,
    string? Message = null)
    : PresentationSuccess(PresentationRequestType.JobStatus),
        IJobBased;

public record GenerateSlideJobControlSuccess(string JobId)
    : PresentationSuccess(PresentationRequestType.JobControl),
        IJobBased;

#endregion

#endregion

#endregion

#region Error

public abstract record PresentationError : ErrorResponse,
    IPresentationDto
{
    protected PresentationError(Exception e) : base(RequestType.Presentation, e)
    {
    }
}

// TODO: Define specific error responses if needed.

#endregion