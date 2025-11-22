namespace generator.Models.Exceptions.Presentations;

public class NoRelationshipIdSlideException(string filepath, int pos) : ArgumentNullException($"The file '{filepath}' has no relationship ID for slide {pos}.")
{
    public string Filepath { get; } = filepath;
    public int Pos { get; } = pos;
}