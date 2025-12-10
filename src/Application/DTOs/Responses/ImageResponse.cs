using Application.DTOs.Requests;

namespace Application.DTOs.Responses;

#region Success

/// <summary>
/// Base image response.
/// </summary>
public abstract record ImageSuccess(string FilePath, ImageRequestType Type) : SuccessResponse(RequestType.Image),
    IImageDto;

/// <summary>
/// Response for success crop image request.
/// </summary>
public record CropImageSuccess(string FilePath, int X, int Y, int Width, int Height)
    : ImageSuccess(FilePath, ImageRequestType.Crop);

#endregion

#region Error

public record ImageError : ErrorResponse,
    IImageDto
{
    public string FilePath { get; init; }

    public ImageError(string filePath, Exception e) : base(RequestType.Image, e)
    {
        FilePath = filePath;
    }
}

#endregion