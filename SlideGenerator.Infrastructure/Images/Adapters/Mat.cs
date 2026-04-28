using System.Drawing;
using OpenCvSharp;
using SlideGenerator.Domain.Images.Entities;
using Size = System.Drawing.Size;

namespace SlideGenerator.Infrastructure.Images.Adapters;

/// <summary>
///     Adapts an <see cref="OpenCvSharp.Mat" /> to the <see cref="IImage" /> domain entity.
/// </summary>
public class Mat(OpenCvSharp.Mat? mat) : IImage
{
    internal readonly OpenCvSharp.Mat Core = mat ?? new OpenCvSharp.Mat();

    public int Width => Core.Width;
    public int Height => Core.Height;

    public bool Empty()
    {
        return Core.Empty();
    }

    public int Channels()
    {
        return Core.Channels();
    }

    public object Clone()
    {
        return new Mat(Core.Clone());
    }

    public byte[] ToByteArray()
    {
        return Cv2.ImEncode(".png", Core, out var bytes)
            ? bytes
            : throw new InvalidOperationException("Cannot encode mat to PNG bytes.");
    }

    public IImage Crop(Rectangle region)
    {
        var rect = new Rect(region.X, region.Y, region.Width, region.Height);
        return new Mat(new OpenCvSharp.Mat(Core, rect).Clone());
    }

    public IImage Resize(Size newSize)
    {
        var result = new OpenCvSharp.Mat();
        Cv2.Resize(Core, result, new OpenCvSharp.Size(newSize.Width, newSize.Height));
        return new Mat(result);
    }

    public Task SaveAsync(string filePath, CancellationToken ct = default)
    {
        Core.SaveImage(filePath);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Core.Dispose();
        GC.SuppressFinalize(this);
    }
}