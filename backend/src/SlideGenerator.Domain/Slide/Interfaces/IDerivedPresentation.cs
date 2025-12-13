namespace SlideGenerator.Domain.Slide.Interfaces;

public interface IDerivedPresentation : IDisposable
{
    string FilePath { get; }
    void AddSlideFromTemplate(Dictionary<string, string?> rowData);
    void Save();
}