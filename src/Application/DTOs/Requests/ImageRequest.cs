using System.Text.Json.Serialization;

namespace Application.DTOs.Requests;

#region Enums

/// <summary>
/// Types of image requests.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImageRequestType
{
    Crop
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

#region Records

/// <summary>
/// Base image request.
/// </summary>
public abstract record ImageRequest(ImageRequestType Type, string FilePath) : Request(RequestType.Image),
    IFilePathBased;

/// <summary>
/// Request to crop an image.
/// </summary>
public record CropImageRequest(string FilePath, int Width, int Height, CropMode Mode = CropMode.Prominent)
    : ImageRequest(ImageRequestType.Crop, FilePath);

#endregion