namespace generator.Models.Exceptions.Presentations;

public class NotOnlySlidePresentationException(string filepath, int amount) : ArgumentException($"The file '{filepath}' is not a presentation with only slides. (Has {amount} slides)")
{
    public string Filepath { get; } = filepath;
    public int Amount { get; } = amount;
}