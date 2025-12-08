namespace presentation.Models.Exceptions.Presentations;

public class NoPresentationPartException(string filepath) : ArgumentNullException($"The file '{filepath}' has no presentation part.")
{
    public string Filepath { get; } = filepath;
}