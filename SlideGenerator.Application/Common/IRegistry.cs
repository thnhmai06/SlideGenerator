namespace SlideGenerator.Application.Common;

public interface IRegistry<T>
{
    T? Read(string filePath);
    
    void Write(string filePath, T? content)
    {
        throw new NotImplementedException();
    }
}