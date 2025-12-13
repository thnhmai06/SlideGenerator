namespace TaoSlideTotNghiep.Infrastructure.Exceptions.Slide;

/// <summary>
/// Exception thrown when a presentation has no slide ID list.
/// </summary>
public class NoSlideIdListException(string filepath)
    : ArgumentNullException($"The file '{filepath}' has no Slide ID List.")
{
    public string Filepath { get; } = filepath;
}