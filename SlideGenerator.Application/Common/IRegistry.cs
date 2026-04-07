namespace SlideGenerator.Application.Common;

public interface IRegistry<T>
{
    T GetOrOpen(string filePath, bool isEditable);

    bool TryGet(string filePath, out T? resource);

    bool Close(string filePath);
}