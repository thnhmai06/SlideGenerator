namespace SlideGenerator.Application.Settings.Abstractions;

public interface IRepository
{
    string Read(string filePath);
    
    void Write(string filePath, string? content);
}