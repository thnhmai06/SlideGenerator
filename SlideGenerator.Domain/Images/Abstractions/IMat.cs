using System.Drawing;

namespace SlideGenerator.Domain.Images.Abstractions;

public interface IMat : IDisposable
{
    /// <summary>
    ///     Gets the width of the mat in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    ///     Gets the height of the mat in pixels.
    /// </summary>
    int Height { get; }

    Size Size => new(Width, Height);
    bool Empty();
    int Channels();
    IMat Clone();
}