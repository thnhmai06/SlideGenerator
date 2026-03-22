using SlideGenerator.Domain.Image.Abstractions;

namespace SlideGenerator.Application.Image.Abstractions;

public interface IImageRegistry
{
    IMat? OpenImage(string path);
}