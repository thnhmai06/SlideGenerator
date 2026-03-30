using ImageMagick;
using SlideGenerator.Domain.Image.Abstractions;
using Mat = SlideGenerator.Infrastructure.Image.Adapters.Mat;

namespace SlideGenerator.Infrastructure.Image.Services;

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