namespace TaoSlideTotNghiep.Infrastructure.Exceptions.Slide;

/// <summary>
/// Exception thrown when a slide has no relationship ID.
/// </summary>
public class NoRelationshipIdSlideException(string filepath, int pos)
    : ArgumentNullException($"The file '{filepath}' has no relationship ID for slide {pos}.")
{
    public string Filepath { get; } = filepath;
    public int Pos { get; } = pos;
}