namespace SlideGenerator.Application.Settings.Abstractions;

public interface ITextFile : IDisposable
{
    string FilePath { get; }

    string Read();

    void Write(string? content);
}