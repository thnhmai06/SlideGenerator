using SlideGenerator.Application.Settings.Abstractions;

namespace SlideGenerator.Infrastructure.Settings.Adapters;

public class FileRepository : IRepository
{
    public string Read(string path)
    {
        return File.ReadAllText(path);
    }

    public void Write(string path, string? content)
    {
        Directory.CreateDirectory(Path.GetPathRoot(path)!);
        File.WriteAllText(path, content);
    }
}