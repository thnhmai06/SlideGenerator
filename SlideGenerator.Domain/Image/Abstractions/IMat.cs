namespace SlideGenerator.Domain.Image.Abstractions;

public interface IMat : IDisposable
{
    /// <summary>
    ///     Gets the width of the mat in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    ///     Gets the height of the mat in pixels.
    /// </summary>
    public int Height { get; }

    public bool Empty();
    public int Channels();
    public IMat Clone();
}