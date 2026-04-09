using ImageMagick;
using SlideGenerator.Domain.Images.Abstractions;
using Mat = SlideGenerator.Infrastructure.Images.Adapters.Mat;

namespace SlideGenerator.Infrastructure.Images.Services;

public class FileSystem
{
    public IMat? Read(string path)
    {
        using var image = new MagickImage(path);
        var pixels = image.GetPixels();
        var bytes = pixels.ToByteArray(PixelMapping.BGR);

        if (bytes == null || bytes.Length == 0)
            return null;
        
        var mat = OpenCvSharp.Mat.FromImageData(bytes);
        return new Mat(mat);
    }
}