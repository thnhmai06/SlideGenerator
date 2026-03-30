namespace SlideGenerator.Application.Common;

public interface IRegistry<T>
{
    T GetOrOpen(string filePath, bool isEditable = true);

    bool TryGet(string filePath, out T? resource);

    bool Close(string filePath);
}