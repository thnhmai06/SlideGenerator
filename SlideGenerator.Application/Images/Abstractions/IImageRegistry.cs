using SlideGenerator.Domain.Images.Abstractions;

namespace SlideGenerator.Application.Images.Abstractions;

public interface IImageRegistry
{
    IMat? OpenImage(string path);
}