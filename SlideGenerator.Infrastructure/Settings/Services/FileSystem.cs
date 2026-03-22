using SlideGenerator.Application.Common;

namespace SlideGenerator.Infrastructure.Settings.Services;

public class FileSystem : IRegistry<string>
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