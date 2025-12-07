using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using TaoSlideTotNghiep.DTOs;
using TaoSlideTotNghiep.Exceptions;
using TaoSlideTotNghiep.Logic;

namespace TaoSlideTotNghiep.Hubs;

/// <summary>
/// SignalR Hub for image processing operations.
/// </summary>
public class ImageHub(ILogger<ImageHub> logger) : Hub
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
    public async Task ProcessRequest(JsonElement message)
    {
        BaseResponse response;

        try
        {
            var typeStr = message.GetProperty("type").GetString()?.ToLowerInvariant();
            
            if (string.IsNullOrEmpty(typeStr))
                throw new TypeNotIncludedException(typeof(ImageRequestType));

            if (typeStr == "crop")
            {
                var request = JsonSerializer.Deserialize<CropImageRequest>(message.GetRawText(), 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (request == null)
                    throw new ArgumentException("Invalid crop request format");

                response = ExecuteCrop(request);
            }
            else
            {
                throw new TypeNotIncludedException(typeof(ImageRequestType));
            }
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
        using var image = new Image(request.FilePath);
        
        int x, y;
        
        if (request.Mode == CropMode.Prominent)
        {
            var (topLeft, _) = image.GetProminentCrop(request.Width, request.Height);
            x = topLeft.X;
            y = topLeft.Y;
        }
        else if (request.Mode == CropMode.Center)
        {
            var topLeft = image.GetCenterCrop(request.Width, request.Height);
            x = topLeft.X;
            y = topLeft.Y;
        }
        else
        {
            throw new TypeNotIncludedException(typeof(CropMode));
        }

        image.Crop(x, y, request.Width, request.Height);
        image.Save();

        return new CropImageResponse
        {
            FilePath = request.FilePath,
            X = x,
            Y = y,
            Width = request.Width,
            Height = request.Height
        };
    }
}
