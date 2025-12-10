using System.Drawing;
using System.Text.Json;
using Application.Contracts;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Exceptions;
using Presentation.Exceptions;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Hubs;

/// <summary>
/// SignalR Hub for image processing operations.
/// </summary>
public class ImageHub(IImageService imageService, ILogger<ImageHub> logger) : Hub
{
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
        Response response;
        var filePath = string.Empty;
        try
        {
            var typeStr = message.GetProperty("type").GetString()?.ToLowerInvariant();

            if (string.IsNullOrEmpty(typeStr))
                throw new TypeNotIncludedException(typeof(ImageRequestType));

            filePath = message.GetProperty("filePath").GetString() ?? string.Empty;

            response = typeStr switch
            {
                "crop" => ExecuteCrop(
                    JsonSerializer.Deserialize<CropImageRequest>(message.GetRawText(), SerializerOptions)
                    ?? throw new InvalidRequestFormatException(nameof(CropImageRequest))),
                _ => throw new TypeNotIncludedException(typeof(ImageRequestType))
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing image request");
            response = new ImageError(filePath, ex);
        }

        await Clients.Caller.SendAsync("ReceiveResponse", response);
    }

    /// <summary>
    /// Executes a crop operation on an image.
    /// </summary>
    private CropImageSuccess ExecuteCrop(CropImageRequest request)
    {
        var result = imageService.CropImage(request.FilePath, new Size(request.Width, request.Height), request.Mode);

        return new CropImageSuccess
        (
            request.FilePath,
            result.X,
            result.Y,
            result.Width,
            result.Height
        );
    }
}