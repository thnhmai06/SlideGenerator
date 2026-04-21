using SlideGenerator.Domain.Images.Entities;

namespace SlideGenerator.Infrastructure.Images.Adapters;

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
        return OpenCvSharp.Cv2.ImEncode(".png", Core, out var bytes) 
            ? bytes 
            : throw new InvalidOperationException("Cannot encode mat to PNG bytes.");
    }
    
    public void Dispose()
    {
        Core.Dispose();
    }
}