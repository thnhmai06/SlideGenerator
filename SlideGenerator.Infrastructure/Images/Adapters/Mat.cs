using SlideGenerator.Domain.Images.Abstractions;

namespace SlideGenerator.Infrastructure.Images.Adapters;

public class Mat(OpenCvSharp.Mat? mat) : IMat
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

    public IMat Clone()
    {
        return new Mat(Core.Clone());
    }

    public void Dispose()
    {
        Core.Dispose();
    }
}