namespace TaoSlideTotNghiep.Exceptions.Presentations;

public class NoSlideIdListException(string filepath) : ArgumentNullException($"The file '{filepath}' has no Slide ID List.")
{
    public string Filepath { get; } = filepath;
}
