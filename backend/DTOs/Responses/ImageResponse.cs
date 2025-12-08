using TaoSlideTotNghiep.DTOs.Requests;

namespace TaoSlideTotNghiep.DTOs.Responses;

/// <summary>
/// Base image response.
/// </summary>
public abstract record ImageResponse(string FilePath, ImageRequestType Type) : Response(RequestType.Image, true), IFilePathBased;

/// <summary>
/// Response for crop image request.
/// </summary>
public record CropImageResponse(string FilePath, int X, int Y, int Width, int Height)
    : ImageResponse(FilePath, ImageRequestType.Crop);
