namespace TaoSlideTotNghiep.Infrastructure.Exceptions.Slide;

/// <summary>
/// Exception thrown when a presentation has no presentation part.
/// </summary>
public class NoPresentationPartException(string filepath)
    : ArgumentNullException($"The file '{filepath}' has no presentation part.")
{
    public string Filepath { get; } = filepath;
}