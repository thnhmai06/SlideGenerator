using System.Drawing;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using TaoSlideTotNghiep.DTOs;
using TaoSlideTotNghiep.Exceptions;
using TaoSlideTotNghiep.Services;

namespace TaoSlideTotNghiep.Hubs;

/// <summary>
/// SignalR Hub for image processing operations.
/// </summary>
public class ImageHub(IImageService imageService, ILogger<ImageHub> logger) : Hub
{
    private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Processes an image request based on type.
    /// </summary>
    public async Task ProcessRequestAsync(JsonElement message)
    {
        BaseResponse response;

        try
        {
            var typeStr = message.GetProperty("type").GetString()?.ToLowerInvariant();

            if (string.IsNullOrEmpty(typeStr))
                throw new TypeNotIncludedException(typeof(ImageRequestType));

            response = typeStr switch
            {
                "crop" => ExecuteCrop(JsonSerializer.Deserialize<CropImageRequest>(message.GetRawText(), _serializerOptions)
                                                    ?? throw new InvalidRequestFormatException(nameof(CropImageRequest))),
                _ => throw new TypeNotIncludedException(typeof(ImageRequestType)),
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing image request");
            response = new ErrorResponse(ex);
        }

        await Clients.Caller.SendAsync("ReceiveResponse", response);
    }

    /// <summary>
    /// Executes a crop operation on an image.
    /// </summary>
    private CropImageResponse ExecuteCrop(CropImageRequest request)
    {
        var result = imageService.CropImage(request.FilePath, new Size(request.Width, request.Height), request.Mode);

        return new CropImageResponse
        {
            FilePath = request.FilePath,
            X = result.X,
            Y = result.Y,
            Width = result.Width,
            Height = result.Height
        };
    }
}
